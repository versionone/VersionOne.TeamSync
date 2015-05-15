using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using VersionOne.Integration.Service.Core;
using VersionOne.Integration.Service.Worker;

namespace VersionOne.Integration.Service
{
    public partial class Service1 : ServiceBase
    {
	    private Timer _timer;
	    private int _secondsToWait = 10;
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
	        _timer = new Timer {Interval = _secondsToWait * 1000};
	        _timer.Elapsed += OnTimedEvent;
            _timer.Enabled = true;
            SimpleLogger.WriteLogMessage(startMessage());
        }

        protected override void OnStop()
        {
            _timer.Enabled = false;
            SimpleLogger.WriteLogMessage(stopMessage());
        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            SimpleLogger.WriteLogMessage("The service event was raised at " + e.SignalTime);
			_worker.DoWork();
			SimpleLogger.WriteLogMessage("Finished at " + e.SignalTime);

        }

        private static string startMessage() 
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("************************************************************");
            sb.AppendLine("* VersionOne.Integration.Service");
            sb.AppendLine("************************************************************");
            sb.AppendLine("Service started...");
            return sb.ToString();
        }

        private static string stopMessage()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Service stopped...");
            return sb.ToString();
        }
    }
}
