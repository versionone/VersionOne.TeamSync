using System;
using System.Collections.Generic;
using System.Linq;
using VersionOne.TeamSync.Interfaces;
using VersionOne.TeamSync.TfsConnector.Config;
using VersionOne.TeamSync.TfsConnector.Interfaces;

namespace VersionOne.TeamSync.TfsWorker.Domain
{
    public class Tfs : ITfs
    {
        private const int ConnectionAttempts = 3;

        private readonly IV1Log _v1Log;
        private readonly ITfsConnector _connector;
        private readonly List<TfsProjectMapping> _projectMappings = new List<TfsProjectMapping>();

        public List<TfsProjectMapping>  ProjectMappings { get { return _projectMappings; } }

        public Tfs(TfsServer serverSettings, DateTime runFromThisDateOn, IV1LogFactory v1LogFactory, ITfsConnectorFactory tfsConnectorFactory)
        {
            _v1Log = v1LogFactory.Create<Tfs>();
            _connector = tfsConnectorFactory.Create(serverSettings);

            var projectMappings = serverSettings.ProjectMappings.Cast<ProjectMapping>();
            if (!projectMappings.Any())
                _v1Log.ErrorFormat(
                    "Tfs server '{0}' requires that project mappings are set in the configuration file.",
                    serverSettings.Name);

            foreach (var projectMapping in projectMappings)
            {
                _projectMappings.Add(new TfsProjectMapping(projectMapping.TfsProject, projectMapping.V1Project, projectMapping.EpicSyncType, runFromThisDateOn));
            }
        }

        public string InstanceUrl
        {
            get { return _connector.BaseUrl; }
        }

        public bool ValidateConnection()
        {
            for (var i = 0; i < ConnectionAttempts; i++)
            {
                _v1Log.DebugFormat("Connection attempt {0}.", i + 1);

                if (_connector.IsConnectionValid())
                    return true;

                System.Threading.Thread.Sleep(5000);
            }
            return false;
        }

        public bool ValidateProjectExists(string projectName)
        {
            return _connector.ProjectExists(projectName);
        }

        public bool ValidateMemberPermissions()
        {
            throw new System.NotImplementedException();
        }
    }
}