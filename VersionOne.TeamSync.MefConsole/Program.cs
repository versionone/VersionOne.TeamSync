using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using VersionOne.TeamSync.V1Connector.Interfaces;

namespace VersionOne.TeamSync.MefConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var dirCatalog = new DirectoryCatalog(@".\");
            var typeCatalog = new TypeCatalog(typeof(IV1ConnectorFactory));
            var catalog = new AggregateCatalog(dirCatalog, typeCatalog);
            var container = new CompositionContainer(dirCatalog);

            //var workers = container.GetExportedValue<IntegrationWorker>();
            var factoryImporter = new Factories();
            container.ComposeParts(factoryImporter);

            factoryImporter.DoAllTheThings();
            Console.ReadKey();
        }
    }

    public class Factories : IPartImportsSatisfiedNotification
    {
        [ImportMany(AllowRecomposition = true)]
        private IEnumerable<IV1ConnectorFactory> _factories;


        public void OnImportsSatisfied()
        {
            Console.WriteLine("Ready to find connectors!");

            if (_factories.Count() == 0)
                throw new InvalidOperationException("I need at least one factory");

        }

        public async void DoAllTheThings()
        {
            foreach (var factory in _factories)
            {
                var connector = factory.WithInstanceUrl("https://www14.v1host.com/v1sdktesting")
                    .WithUserAgentHeader("JogoCode", "0.0.0")
                    .WithUsernameAndPassword(***REMOVED***)
                    .Build();
                string adminName = string.Empty;
                await connector.QueryOne("Member", "20", new[] { "Name" }, xElement =>
                {
                    var attributes = xElement.Elements("Attribute")
                        .ToDictionary(item => item.Attribute("name").Value, item => item.Value);
                    adminName = attributes.ContainsKey("Name") ? attributes["Name"] : string.Empty;
                });
                System.Console.WriteLine(string.Format("The Administrator name: {0}", adminName));
            }
        }
    }
}
