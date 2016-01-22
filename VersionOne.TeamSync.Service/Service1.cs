using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using log4net;
using VersionOne.TeamSync.Core.Config;
using VersionOne.TeamSync.Interfaces;

namespace VersionOne.TeamSync.Service
{
    public partial class Service1 : ServiceBase
    {
        private Timer _timer;
        private static TimeSpan _serviceDuration;
        [Import]
        private IV1StartupWorker _worker;
        private static readonly ILog Log = LogManager.GetLogger(typeof(Service1));
		private CompositionContainer _container;

        public Service1()
        {
            InitializeComponent();
            try
            {

                var dirCatalog = new DirectoryCatalog(@".\", "*.TeamSync.*.dll");
				LogAppend(dirCatalog.FullPath);
                _container = new CompositionContainer(dirCatalog);
                _container.ComposeParts(this);
            }
            catch (Exception e)
            {
                LogAppend(e.GetBaseException().StackTrace + " constructor method");
            }
        }

		private static void LogAppend(string line)
		{
			System.IO.File.AppendAllLines(@"C:\TEAMSYNCLOG.txt", new List<string>() { line });
		}

        public void OnDebugStart()
        {
            OnStart(null);
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                _serviceDuration = TimeSpan.FromMinutes(ServiceSettings.Settings.SyncIntervalInMinutes);
                StartMessage();
                if (_worker == null)
                {
                    LogAppend("worker is null");
                }
                else
                {
                    LogAppend("worker not null");
                    
                }
                _worker.ValidateConnections();
                LogAppend("validation connection pass");
                _worker.ValidateProjectMappings();
                LogAppend("validate project mapping pass");
                _worker.ValidateMemberAccountPermissions();
                LogAppend("validate member account pass");
                //_worker.ValidateVersionOneSchedules(); D-09877
                _worker.ValidatePriorityMappings();
                LogAppend("validate priority mapping pass");
                _worker.ValidateStatusMappings();
                LogAppend("validate status mapping pass");

                _worker.DoFirstRun();

                _timer = new Timer(OnTimedEvent, null, 0, (int)_serviceDuration.TotalMilliseconds);
            }
            catch (Exception e)
            {
                LogAppend(e.GetBaseException().StackTrace + " on start method");
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

        private void OnTimedEvent(object stateInfo)
        {
            Log.DebugFormat("The service event was raised at {0}", DateTime.Now);
            _worker.DoWork();
            Log.DebugFormat("The service event was completed at {0}", DateTime.Now);
        }

        protected override void OnContinue()
        {
            Log.Info("*** VersionOne starting again ***");
            base.OnContinue();
            Log.Info("*** VersionOne after oncontinue ***");
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