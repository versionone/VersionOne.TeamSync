using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ServiceProcess;
using System.Threading;
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
        [Import] 
        private IV1LogFactory _v1LogFactory;
        private readonly IV1Log _v1Log;
        private CompositionContainer _container;

        public Service1()
        {
            InitializeComponent();
            var dirCatalog = new DirectoryCatalog(@".\", "*.TeamSync.*.dll");
            _container = new CompositionContainer(dirCatalog);
            _container.ComposeParts(this);
            _v1Log = _v1LogFactory.Create<Service1>();
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
                _v1Log.Error(e);
                _v1Log.Error("Errors occurred during service start up. Service will be stopped.");
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
            _v1Log.DebugFormat("The service event was raised at {0}", DateTime.Now);
            _worker.DoWork();
            _v1Log.DebugFormat("The service event was completed at {0}", DateTime.Now);
        }

        protected override void OnContinue()
        {
            _v1Log.Info("*** VersionOne starting again ***");
            base.OnContinue();
            _v1Log.Info("*** VersionOne after oncontinue ***");
        }

        private void StartMessage()
        {
            _v1Log.Info("*** VersionOne TeamSync ***");
            _v1Log.Info("Starting service...");
            _v1Log.DebugFormat("Started at {0}", DateTime.Now);
        }

        private void StopMessage()
        {
            _v1Log.Info("Stopping service...");
            _v1Log.DebugFormat("Stopped at {0}", DateTime.Now);
            _v1Log.Info("");
        }
    }
}