using System;
using System.Collections.Generic;
using System.Threading;
using log4net;
using VersionOne.TeamSync.Interfaces;
using VersionOne.TeamSync.VersionOne.Domain;

namespace VersionOne.TeamSync.TfsWorker
{
    public class VersionOneToTfsWorkerFactory : IV1StartupWorkerFactory
    {
        public IV1StartupWorker Create()
        {
            return new VersionOneToTfsWorker();
        }
    }

    public class VersionOneToTfsWorker : IV1StartupWorker
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (VersionOneToTfsWorker));
        private readonly IV1 _v1;
        private readonly List<IAsyncWorker> _asyncWorkers;

        public void DoFirstRun()
        {
            var syncTime = DateTime.Now;
            Log.Info("<DUMMY> Beginning first run...");
            Thread.Sleep(2500);
            Log.Info("<DUMMY> Ending first run...");
            Log.DebugFormat("<DUMMY> Total time: {0}", DateTime.Now - syncTime);
        }

        public void DoWork()
        {
            var syncTime = DateTime.Now;
            Log.Info("<DUMMY> Beginning sync...");
            Thread.Sleep(2500);
            Log.Info("<DUMMY> Ending sync...");
            Log.DebugFormat("<DUMMY> Total sync time: {0}", DateTime.Now - syncTime);
        }

        public bool IsActualWorkEnabled
        {
            get { throw new System.NotImplementedException(); }
        }

        public void ValidateConnections()
        {
            Log.Info("<DUMMY> Verifying VersionOne connection...");
            Log.DebugFormat("<DUMMY> URL: {0}", "http://localhost/VersionOne");
            Thread.Sleep(2500);
            Log.Info("<DUMMY> VersionOne connection successful!");
            
            Log.InfoFormat("<DUMMY> Verifying TFS connection...");
            Log.DebugFormat("<DUMMY> URL: {0}", "http://dummy.tfs.url");
            Thread.Sleep(2500);
            Log.Info("<DUMMY> TFS connection successful!");
        }

        public void ValidateProjectMappings()
        {
            Log.InfoFormat("<DUMMY> Verifying V1ProjectID={1} to TfsProjectID={0} project mapping...", "DUMMY_TFS", "DUMMY_V1");
            Thread.Sleep(2500);
            Log.Info("<DUMMY> Mapping successful! Projects will be synchronized.");
        }

        public void ValidateMemberAccountPermissions()
        {
            Log.Info("<DUMMY> Verifying VersionOne member account permissions...");
            Log.DebugFormat("<DUMMY> Member: {0}", "Member:20");
            Thread.Sleep(2500);
            Log.Info("<DUMMY> VersionOne member account has valid permissions.");

            Log.Info("<DUMMY> Verifying TFS member account permissions...");
            Log.DebugFormat("<DUMMY> Member: {0}", "TFS_MEMBER");
            Thread.Sleep(2500);
            Log.Info("<DUMMY> FS member account has valid permissions.");
        }

        public void ValidatePriorityMappings()
        {
            Log.Info("<DUMMY> Verifying priority mappings...");
            Thread.Sleep(2500);
            Log.Info("<DUMMY> Priority mappings verified...");
        }

        public void ValidateStatusMappings()
        {
            Log.Info("<DUMMY> Verifying status mappings...");
            Thread.Sleep(2500);
            Log.Info("<DUMMY> Status mappings verified...");
        }
    }
}