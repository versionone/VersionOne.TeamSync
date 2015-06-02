using System;
using System.ServiceProcess;
using System.Text;
using System.Timers;
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
            SimpleLogger.WriteLogMessage(startMessage());
            _worker.DoWork(_serviceDuration); //fire immediately at start
        }

        protected override void OnStop()
        {
            _timer.Enabled = false;
            SimpleLogger.WriteLogMessage(stopMessage());
        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            SimpleLogger.WriteLogMessage("The service event was raised at " + e.SignalTime);
            _worker.DoWork(_serviceDuration);
			SimpleLogger.WriteLogMessage(" ************************** Finished at " + e.SignalTime + "");
        }

        private static string startMessage() 
        {
            var sb = new StringBuilder();
            sb.AppendLine("************************************************************");
            sb.AppendLine("Service started...");
            return sb.ToString();
        }

        private static string stopMessage()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Service stopped...");
            sb.AppendLine("************************************************************");
            return sb.ToString();
        }
    }
}
