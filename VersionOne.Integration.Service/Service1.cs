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

namespace VersionOne.Integration.Service
{
    public partial class Service1 : ServiceBase
    {
        private Timer timer = null;

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
            timer = new Timer();
            timer.Interval = 5000; //Every 5 seconds.
            timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            timer.Enabled = true;
            SimpleLogger.WriteLogMessage("VersionOne.Integration.Service started");
        }

        protected override void OnStop()
        {
            timer.Enabled = false;
            SimpleLogger.WriteLogMessage("VersionOne.Integration.Service stopped");
        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            SimpleLogger.WriteLogMessage("The service event was raised at " + e.SignalTime);
        }
    }
}
