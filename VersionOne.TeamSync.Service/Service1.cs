using System;
using System.ServiceProcess;
using System.Timers;
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
            _serviceDuration = new TimeSpan(0, 0, ServiceSettings.Settings.syncIntervalInSeconds);

            try
            {
                startMessage();
                _worker = new VersionOneToJiraWorker(_serviceDuration);
                try
                {
                    _worker.ValidateConnections();
                    _worker.ValidateProjectMappings();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    Log.Error("Errors occurred during service start up. Service will be stopped.");
                    Stop(); // Do we really want to stop the service on a Exception? This makes app crash because some async tasks might be still running
                }
                _timer = new Timer { Interval = _serviceDuration.TotalMilliseconds };
                _timer.Elapsed += OnTimedEvent;
                _timer.Enabled = true;
                _worker.DoWork(); //HACK: fire immediately at start. Use a different Timer control instead? (System.Threading.Timer)
            }
            catch (Exception e)
            {
                Log.Error(e);
                Log.Error("Errors occurred during sync.");
            }
        }

        protected override void OnStop()
        {
            if (_timer != null)
                _timer.Enabled = false;

            stopMessage();
        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            Log.DebugFormat("The service event was raised at {0}", e.SignalTime);
            _worker.DoWork();
            Log.DebugFormat("The service event was completed at {0}", e.SignalTime);
        }

        private static void startMessage()
        {
            Log.Info("*** VersionOne TeamSync ***");
            Log.Info("Starting service...");
            Log.DebugFormat("Started at {0}", DateTime.Now);
        }

        private static void stopMessage()
        {
            Log.Info("Stopping service...");
            Log.DebugFormat("Stopped at {0}", DateTime.Now);
            Log.Info("");
        }
    }
}
