using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Globalization;
using System.Linq;
using VersionOne.TeamSync.Interfaces;
using VersionOne.TeamSync.TfsConnector.Config;
using VersionOne.TeamSync.TfsConnector.Interfaces;

namespace VersionOne.TeamSync.TfsWorker.Domain
{
    public class TfsFactory : ITfsFactory
    {
        private readonly IV1LogFactory _v1LogFactory;
        private readonly ITfsConnectorFactory _tfsConnectorFactory;
        private readonly IV1Log _v1Log;

        [ImportingConstructor]
		public TfsFactory([Import] IV1LogFactory v1LogFactory, [Import] ITfsConnectorFactory tfsConnectorFactory)
        {
            _v1Log = v1LogFactory.Create<TfsFactory>();
			_v1LogFactory = v1LogFactory;
            _tfsConnectorFactory = tfsConnectorFactory;
		}

        public IList<ITfs> Create(ITfsSettings tfsSettings)
        {
            var instances = new List<ITfs>();
            foreach (var serverSettings in tfsSettings.Servers.Cast<TfsServer>().Where(s => s.Enabled))
            {
                instances.Add(new Tfs(serverSettings, ParseRunFromThisDateOn(tfsSettings.RunFromThisDateOn),
                    _v1LogFactory, _tfsConnectorFactory));
            }

            return instances;
        }

        private DateTime ParseRunFromThisDateOn(string runFromThisDateOn)
        {
            DateTime parsedDate;

            if (string.IsNullOrEmpty(runFromThisDateOn))
            {
                parsedDate = new DateTime(1980, 1, 1);
                _v1Log.Info("No date found, defaulting to " + parsedDate.ToString("yyyy-MM-dd"));
            }
            else if (!DateTime.TryParseExact(runFromThisDateOn, "M/d/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
            {
                _v1Log.Error("Invalid date : " + runFromThisDateOn);
                throw new ConfigurationErrorsException("RunFromThisDateOn contains an invalid entry");
            }

            return parsedDate;
        }
    }
}