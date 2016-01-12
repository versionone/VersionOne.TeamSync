using System;
using System.Collections.Generic;
using System.Threading;
using VersionOne.TeamSync.Interfaces;
using VersionOne.TeamSync.VersionOne.Domain;
using System.ComponentModel.Composition;
using System.Linq;
using VersionOne.TeamSync.TfsConnector.Config;
using VersionOne.TeamSync.TfsConnector.Interfaces;

namespace VersionOne.TeamSync.TfsWorker
{
    public class VersionOneToTfsWorker : IV1StartupWorker
    {
        private readonly IV1 _v1;
        private readonly List<IAsyncWorker> _asyncWorkers;
		private readonly IV1Log _v1Log;
        private List<ITfsConnector> _connectors = new List<ITfsConnector>();

        [ImportingConstructor]
        public VersionOneToTfsWorker([Import]IV1LogFactory v1LogFactory, [Import]IV1 v1, [Import] ITfsConnectorFactory tfsConnectorFactory)
        {
            _v1Log = v1LogFactory.Create<VersionOneToTfsWorker>();
            _v1 = v1;
            var settings = TfsSettings.GetInstance();

            foreach (var server in settings.Servers.Cast<TfsServer>())
            {
                _connectors.Add(tfsConnectorFactory.Create(server));
            }
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
            foreach (var tfsConnector in _connectors)
            {
                tfsConnector.IsConnectionValid();
            }
        }

        public void ValidateProjectMappings()
        {
            _v1Log.InfoFormat("<DUMMY> Verifying V1ProjectID={1} to TfsProjectID={0} project mapping...", "DUMMY_TFS", "DUMMY_V1");
            Thread.Sleep(2500);
            _v1Log.Info("<DUMMY> Mapping successful! Projects will be synchronized.");
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