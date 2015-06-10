using System;
using System.ServiceProcess;
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
	    private static VersionOneToJiraWorker _worker;
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
            try
            {
                _worker = new VersionOneToJiraWorker(_serviceDuration);
                _worker.ValidateConnections();
              //_timer = new Timer() { Interval = _serviceDuration.TotalMilliseconds };
              //_timer.Elapsed += OnTimedEvent;
              //_timer.Enabled = true;
              //startMessage();
                _worker.DoWork(); //fire immediately at start
            }
            catch (Exception e)
            {
                _log.Error(e);
                OnStop();
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
            _log.Debug("The service event was raised at " + e.SignalTime);
            _worker.DoWork();
            _log.Trace("Test trace logging...");
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
