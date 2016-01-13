using System;
using System.Collections.Generic;
using System.Threading;
using VersionOne.TeamSync.Interfaces;
using VersionOne.TeamSync.VersionOne.Domain;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using VersionOne.TeamSync.TfsConnector.Config;
using VersionOne.TeamSync.TfsConnector.Interfaces;
using VersionOne.TeamSync.TfsWorker.Domain;

namespace VersionOne.TeamSync.TfsWorker
{
    public class VersionOneToTfsWorker : IV1StartupWorker
    {
        private readonly List<IAsyncWorker> _asyncWorkers;
		private readonly IV1Log _v1Log;
        private readonly IV1 _v1;
        private readonly IList<ITfs> _tfsInstances;

        [ImportingConstructor]
        public VersionOneToTfsWorker([Import]IV1LogFactory v1LogFactory, [Import]IV1 v1, [Import]ITfsFactory tfsFactory)
        {
            _v1Log = v1LogFactory.Create<VersionOneToTfsWorker>();
            _v1 = v1;
            
            var tfsSettings = TfsSettings.GetInstance();
            _tfsInstances = tfsFactory.Create(tfsSettings);
        }

        public void DoFirstRun()
        {
            var syncTime = DateTime.Now;
            _v1Log.Info("<DUMMY> Beginning first run...");
            Thread.Sleep(2500);
            _v1Log.Info("<DUMMY> Ending first run...");
            _v1Log.DebugFormat("<DUMMY> Total time: {0}", DateTime.Now - syncTime);
        }

        public void DoWork()
        {
            var syncTime = DateTime.Now;
            _v1Log.Info("<DUMMY> Beginning sync...");
            Thread.Sleep(2500);
            _v1Log.Info("<DUMMY> Ending sync...");
            _v1Log.DebugFormat("<DUMMY> Total sync time: {0}", DateTime.Now - syncTime);
        }

        public bool IsActualWorkEnabled
        {
            get { throw new System.NotImplementedException(); }
        }

        public void ValidateConnections()
        {
            if (_tfsInstances.Any())
            {
                _v1Log.Info("Verifying VersionOne connection...");
                _v1Log.DebugFormat("URL: {0}", _v1.InstanceUrl);
                if (_v1.ValidateConnection())
                {
                    _v1Log.Info("VersionOne connection successful!");
                }
                else
                {
                    _v1Log.Error("VersionOne connection failed.");
                    throw new Exception(string.Format("Unable to validate connection to {0}.", _v1.InstanceUrl));
                }

                foreach (var tfsInstance in _tfsInstances)
                {
                    _v1Log.InfoFormat("Verifying Tfs connection...");
                    _v1Log.DebugFormat("URL: {0}", tfsInstance.InstanceUrl);
                    _v1Log.Info(tfsInstance.ValidateConnection()
                        ? "Tfs connection successful!"
                        : "Tfs connection failed!");
                }
            }
        }

        public void ValidateProjectMappings()
        {
            foreach (var tfsInstance in _tfsInstances)
            {
                var invalidMappings = new List<TfsProjectMapping>();
                foreach (var projectMapping in tfsInstance.ProjectMappings)
                {
                    var isMappingValid = true;
                    _v1Log.InfoFormat("Verifying V1ProjectID={0} to TfsProjectID={1} project mapping...",
                        projectMapping.V1Project, projectMapping.TfsProject);

                    if (!tfsInstance.ValidateProjectExists(projectMapping.TfsProject))
                    {
                        _v1Log.ErrorFormat("Tfs project '{0}' does not exist. Current project mapping will be ignored",
                            projectMapping.TfsProject);
                        isMappingValid = false;
                    }
                    if (isMappingValid && !_v1.ValidateProjectExists(projectMapping.V1Project))
                    {
                        _v1Log.ErrorFormat(
                            "VersionOne project '{0}' does not exist or does not have a role assigned for user {1}. Current project mapping will be ignored",
                            projectMapping.V1Project, _v1.MemberId);
                        isMappingValid = false;
                    }
                    if (isMappingValid && !_v1.ValidateEpicCategoryExists(projectMapping.EpicCategory))
                    {
                        _v1Log.ErrorFormat(
                            "VersionOne Epic Category '{0}' does not exist. Current project mapping will be ignored",
                            projectMapping.EpicCategory);
                        isMappingValid = false;
                    }

                    if (isMappingValid)
                    {
                        _v1Log.Info("Mapping successful! Projects will be synchronized.");
                    }
                    else
                    {
                        _v1Log.Error("Mapping failed. Projects will not be synchronized.");
                        invalidMappings.Add(projectMapping);
                    }
                }
                tfsInstance.ProjectMappings.RemoveAll(pm => invalidMappings.Contains(pm));

                if (!tfsInstance.ProjectMappings.Any())
                    throw new Exception(
                        "No valid projects to synchronize. You need at least one valid project mapping for the service to run.");
            }
        }

        public void ValidateMemberAccountPermissions()
        {
            _v1Log.Info("<DUMMY> Verifying VersionOne member account permissions...");
            _v1Log.DebugFormat("<DUMMY> Member: {0}", "Member:20");
            Thread.Sleep(2500);
            _v1Log.Info("<DUMMY> VersionOne member account has valid permissions.");

            _v1Log.Info("<DUMMY> Verifying TFS member account permissions...");
            _v1Log.DebugFormat("<DUMMY> Member: {0}", "TFS_MEMBER");
            Thread.Sleep(2500);
            _v1Log.Info("<DUMMY> FS member account has valid permissions.");
        }

        public void ValidatePriorityMappings()
        {
            _v1Log.Info("<DUMMY> Verifying priority mappings...");
            Thread.Sleep(2500);
            _v1Log.Info("<DUMMY> Priority mappings verified...");
        }

        public void ValidateStatusMappings()
        {
            _v1Log.Info("<DUMMY> Verifying status mappings...");
            Thread.Sleep(2500);
            _v1Log.Info("<DUMMY> Status mappings verified...");
        }
    }
}