using System;
using System.ServiceProcess;
using System.Text;
using System.Timers;
using log4net;
using VersionOne.TeamSync.Core;
using VersionOne.TeamSync.Core.Config;
using VersionOne.TeamSync.Worker;

namespace VersionOne.TeamSync.Service
{
    public partial class Service1 : ServiceBase
    {
	    private Timer _timer;
        private static TimeSpan _serviceDuration;
	    private static readonly VersionOneToJiraWorker _worker = new VersionOneToJiraWorker();
        private static ILog _log = LogManager.GetLogger(typeof (Service1));

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

            _timer = new Timer() { Interval = _serviceDuration.TotalMilliseconds };
            _timer.Elapsed += OnTimedEvent;
            _timer.Enabled = true;
            startMessage();
            _worker.DoWork(_serviceDuration); //fire immediately at start
        }

        protected override void OnStop()
        {
            _timer.Enabled = false;
            stopMessage();
        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            _log.Info("The service event was raised at " + e.SignalTime);
            _worker.DoWork(_serviceDuration);
            _log.Info(" ************************** Finished at " + e.SignalTime + "");
        }

        private static void startMessage() 
        {
            _log.Info("************************************************************");
            _log.Info("Service started...");
            _log.DebugFormat("-> Started at {0}", DateTime.Now);
        }

        private static void stopMessage()
        {
            _log.Info("Service stopped...");
            _log.Info("************************************************************");
            _log.DebugFormat("-> Stopped at {0}", DateTime.Now);
        }
    }
}
