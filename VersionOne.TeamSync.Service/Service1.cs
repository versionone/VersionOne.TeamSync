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
        [ImportMany] 
        private IEnumerable<IV1StartupWorkerFactory> _startupWorkerFactories;
        private IV1StartupWorker _worker;
        private static readonly ILog Log = LogManager.GetLogger(typeof(Service1));
		private CompositionContainer _container;

        public Service1()
        {
            InitializeComponent();

            var dirCatalog = new DirectoryCatalog(@".\");
            _container = new CompositionContainer(dirCatalog);
            _container.ComposeParts(this);
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
                var firstStartupWorkerFactory = _startupWorkerFactories.FirstOrDefault();

                StartMessage();
                _worker = firstStartupWorkerFactory.Create(_container);
                _worker.ValidateConnections();
                _worker.ValidateProjectMappings();
                _worker.ValidateMemberAccountPermissions();
                //_worker.ValidateVersionOneSchedules(); D-09877
                _worker.ValidatePriorityMappings();
                _worker.ValidateStatusMappings();

                _worker.DoFirstRun();

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