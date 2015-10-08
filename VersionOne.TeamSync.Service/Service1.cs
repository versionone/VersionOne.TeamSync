using System;
using System.ServiceProcess;
using System.Threading;
using log4net;
using VersionOne.TeamSync.Core.Config;
using VersionOne.TeamSync.Worker;

namespace VersionOne.TeamSync.Service
{
    public partial class Service1 : ServiceBase
    {
        private Timer _timer;
        private static TimeSpan _serviceDuration;
        private static VersionOneToJiraWorker _worker;
        private static readonly ILog Log = LogManager.GetLogger(typeof(Service1));

        public Service1()
        {
            InitializeComponent();
        }

        public void OnDebugStart()
        {
            OnStart(null);
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                _serviceDuration = new TimeSpan(0, 0, ServiceSettings.Settings.SyncIntervalInSeconds);

                StartMessage();
                _worker = new VersionOneToJiraWorker();
                _worker.ValidateConnections();
                _worker.ValidateProjectMappings();
                _worker.ValidateMemberAccountPermissions();
                //_worker.ValidateVersionOneSchedules(); D-09877
                _worker.ValidatePriorityMappings();

                _timer = new Timer(OnTimedEvent, null, 0, (int)_serviceDuration.TotalMilliseconds);
            }
            catch (Exception e)
            {
                Log.Error(e);
                Log.Error("Errors occurred during service start up. Service will be stopped.");
                Stop();
            }
        }

        protected override void OnStop()
        {
            if (_timer != null)
                _timer.Dispose();

            StopMessage();
        }

        private static void OnTimedEvent(object stateInfo)
        {
            Log.DebugFormat("The service event was raised at {0}", DateTime.Now);
            _worker.DoWork();
            Log.DebugFormat("The service event was completed at {0}", DateTime.Now);
        }

        private static void StartMessage()
        {
            Log.Info("*** VersionOne TeamSync ***");
            Log.Info("Starting service...");
            Log.DebugFormat("Started at {0}", DateTime.Now);
        }

        private static void StopMessage()
        {
            Log.Info("Stopping service...");
            Log.DebugFormat("Stopped at {0}", DateTime.Now);
            Log.Info("");
        }
    }
}