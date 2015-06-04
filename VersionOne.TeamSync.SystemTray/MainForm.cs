using System;
using System.Drawing;
using System.Runtime.Remoting;
using System.ServiceProcess;
using System.Windows.Forms;

namespace VersionOne.TeamSync.SystemTray
{
    public partial class SystemTray : Form
    {
        public SystemTray()
        {
            InitializeComponent();
            RemotingConfiguration.Configure("VersionOne.TeamSync.SystemTray.exe.config", false);
            RemotingConfiguration.RegisterWellKnownServiceType(new WellKnownServiceTypeEntry(typeof(RemoteLoggingSink), "LoggingSink", WellKnownObjectMode.SingleCall));

            contextMenuStrip1.Renderer = new CustomRenderer();
            UpdateContextMenuStrip();
        }

        private void startServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TeamSyncServiceController.StartService();
            UpdateContextMenuStrip();
        }

        private void stopServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TeamSyncServiceController.StopService();
            UpdateContextMenuStrip();
        }

        private void pauseServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var serviceStatus = TeamSyncServiceController.GetServiceStatus();
            if (serviceStatus == ServiceControllerStatus.Running)
            {
                TeamSyncServiceController.PauseService();
                UpdateContextMenuStrip();
            }
            else if (serviceStatus == ServiceControllerStatus.Paused)
            {
                TeamSyncServiceController.ContinueService();
                UpdateContextMenuStrip();
            }
        }

        private void recycleServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TeamSyncServiceController.RecycleService();
            UpdateContextMenuStrip();
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

        private void UpdateContextMenuStrip()
        {
            if (TeamSyncServiceController.IsServiceInstalled())
            {

                var serviceStatus = TeamSyncServiceController.GetServiceStatus();
                this.contextMenuStrip1.Items["startServiceToolStripMenuItem"].Enabled = 
                    serviceStatus == ServiceControllerStatus.Stopped;
                this.contextMenuStrip1.Items["pauseServiceToolStripMenuItem"].Enabled = 
                    serviceStatus == ServiceControllerStatus.Running 
                    || serviceStatus == ServiceControllerStatus.Paused;
                this.contextMenuStrip1.Items["recycleServiceToolStripMenuItem"].Enabled = 
                    serviceStatus == ServiceControllerStatus.Running;
                this.contextMenuStrip1.Items["stopServiceToolStripMenuItem"].Enabled = 
                    serviceStatus == ServiceControllerStatus.Running;
                if (serviceStatus == ServiceControllerStatus.Running)
                {
                    this.contextMenuStrip1.Items["pauseServiceToolStripMenuItem"].Text = "Pause";
                }
                else if (serviceStatus == ServiceControllerStatus.Paused)
                {
                    this.contextMenuStrip1.Items["pauseServiceToolStripMenuItem"].Text = "Continue";
                }
                this.contextMenuStrip1.Items["recycleServiceToolStripMenuItem"].Enabled = true;
            }
            else
            {
                this.contextMenuStrip1.Items["startServiceToolStripMenuItem"].Enabled = false;
                this.contextMenuStrip1.Items["stopServiceToolStripMenuItem"].Enabled = false;
                this.contextMenuStrip1.Items["pauseServiceToolStripMenuItem"].Enabled = false;
                this.contextMenuStrip1.Items["recycleServiceToolStripMenuItem"].Enabled = false;
                this.contextMenuStrip1.Items["viewActivityToolStripMenuItem"].Enabled = false;
            }
        }
    }

    public class CustomRenderer : ToolStripProfessionalRenderer
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
