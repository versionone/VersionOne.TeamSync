using System;
using System.Drawing;
using System.Runtime.Remoting;
using System.ServiceProcess;
using System.Timers;
using System.Windows.Forms;
using log4net;

namespace VersionOne.TeamSync.SystemTray
{
    public partial class SystemTray : Form
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SystemTray));
        private System.Timers.Timer _timer;

        public SystemTray()
        {
            InitializeComponent();
            RemotingConfiguration.Configure("VersionOne.TeamSync.SystemTray.exe.config", false);
            RemotingConfiguration.RegisterWellKnownServiceType(new WellKnownServiceTypeEntry(typeof(RemoteLoggingSink), "LoggingSink", WellKnownObjectMode.SingleCall));

            Log.Info("*** VersionOne SystemTray started ***");

            contextMenuStrip1.Renderer = new CustomRenderer();
            UpdateServiceControlOptions();
            StartWatchingServiceStatus();
        }

        private void StartWatchingServiceStatus()
        {
            _timer = new System.Timers.Timer() { Interval = 5000 };
            _timer.Elapsed += UpdateUIServiceControlOptions;
            _timer.Enabled = true;
        }

        private void StopWatchingServiceStatus()
        {
            _timer.Enabled = false;
        }

        private void UpdateUIServiceControlOptions(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            UpdateServiceControlOptions();
            ViewActivityForm vaForm = (ViewActivityForm)Application.OpenForms["ViewActivityForm"];
            if (vaForm != null)
                vaForm.UpdateServiceControlButtons();
        }

        private void startServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                TeamSyncServiceController.StartService();
                UpdateServiceControlOptions();
            }
            catch (ServiceControllerException ex)
            {
                DialogUtils.ShowServiceControllerException(ex);
            }
        }

        private void stopServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                TeamSyncServiceController.StopService();
                UpdateServiceControlOptions();
            }
            catch (ServiceControllerException ex)
            {
                DialogUtils.ShowServiceControllerException(ex);
            }
        }

        private void recycleServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try { 
                TeamSyncServiceController.RecycleService();
                UpdateServiceControlOptions();
            }
            catch (ServiceControllerException ex)
            {
                DialogUtils.ShowServiceControllerException(ex);
            }
        }

        private void exitServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void viewActivityToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ViewActivityForm vaForm = (ViewActivityForm)Application.OpenForms["ViewActivityForm"];
            if (vaForm == null)
            {
                var vaf = new ViewActivityForm();
                vaf.Show();
            }
        }

        delegate void UpdateServiceControlOptionsCallback();

        public void UpdateServiceControlOptions()
        {
            if (this.InvokeRequired)
            {
                UpdateServiceControlOptionsCallback d =
                    new UpdateServiceControlOptionsCallback(UpdateServiceControlOptions);
                Invoke(d, new object[] {});
            }
            else
            {
                if (TeamSyncServiceController.IsServiceInstalled())
                {
                    var serviceStatus = TeamSyncServiceController.GetServiceStatus();

                    this.contextMenuStrip1.Items["startServiceToolStripMenuItem"].Enabled =
                        serviceStatus == ServiceControllerStatus.Stopped;
                    this.contextMenuStrip1.Items["recycleServiceToolStripMenuItem"].Enabled =
                        serviceStatus == ServiceControllerStatus.Running;
                    this.contextMenuStrip1.Items["stopServiceToolStripMenuItem"].Enabled =
                        serviceStatus == ServiceControllerStatus.Running;
                    this.contextMenuStrip1.Items["recycleServiceToolStripMenuItem"].Enabled =
                        serviceStatus == ServiceControllerStatus.Running;
                    this.contextMenuStrip1.Items["configureServiceToolStripMenuItem"].Enabled = true;
                }
                else
                {
                    this.contextMenuStrip1.Items["startServiceToolStripMenuItem"].Enabled = false;
                    this.contextMenuStrip1.Items["stopServiceToolStripMenuItem"].Enabled = false;
                    this.contextMenuStrip1.Items["recycleServiceToolStripMenuItem"].Enabled = false;
                    this.contextMenuStrip1.Items["configureServiceToolStripMenuItem"].Enabled = false;
                }
            }
        }

        private void SystemTray_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopWatchingServiceStatus();
            Log.Info("*** VersionOne SytemTray stopped ***");
        }

        private void configureServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var path = TeamSyncServiceController.GetServicePath() + ".config";
            System.Diagnostics.Process.Start(path);
        }
    }

    public class CustomRenderer : ToolStripSystemRenderer
    {
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.ToolStrip.Items.IndexOf(e.Item) == 0)
            {
                e.Graphics.DrawImage(new Bitmap("versionone-logo-noTagline.png"),
                    new Rectangle(10, 0, 125, 25));
            }
            else base.OnRenderMenuItemBackground(e);
        }
    }
}
