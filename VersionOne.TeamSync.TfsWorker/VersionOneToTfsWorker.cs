using System;
using System.Collections.Generic;
using System.Threading;
using VersionOne.TeamSync.Interfaces;
using VersionOne.TeamSync.VersionOne.Domain;
using System.ComponentModel.Composition;

namespace VersionOne.TeamSync.TfsWorker
{
    public class VersionOneToTfsWorker : IV1StartupWorker
    {
        private readonly IV1 _v1;
        private readonly List<IAsyncWorker> _asyncWorkers;
		private readonly IV1Log _v1Log;

        [ImportingConstructor]
        public VersionOneToTfsWorker([Import]IV1LogFactory v1LogFactory)
        {
            _v1Log = v1LogFactory.Create<VersionOneToTfsWorker>();
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
            _v1Log.Info("<DUMMY> Verifying VersionOne connection...");
            _v1Log.DebugFormat("<DUMMY> URL: {0}", "http://localhost/VersionOne");
            Thread.Sleep(2500);
            _v1Log.Info("<DUMMY> VersionOne connection successful!");
            
            _v1Log.InfoFormat("<DUMMY> Verifying TFS connection...");
            _v1Log.DebugFormat("<DUMMY> URL: {0}", "http://dummy.tfs.url");
            Thread.Sleep(2500);
            _v1Log.Info("<DUMMY> TFS connection successful!");
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