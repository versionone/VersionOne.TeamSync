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
            try
            {
                TeamSyncServiceController.StartService();
                UpdateContextMenuStrip();
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
                UpdateContextMenuStrip();
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
                UpdateContextMenuStrip();
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

        public void UpdateContextMenuStrip(bool updateActivityWindowButtons = true)
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
                    serviceStatus == ServiceControllerStatus.Running; ;
            }
            else
            {
                this.contextMenuStrip1.Items["startServiceToolStripMenuItem"].Enabled = false;
                this.contextMenuStrip1.Items["stopServiceToolStripMenuItem"].Enabled = false;
                this.contextMenuStrip1.Items["recycleServiceToolStripMenuItem"].Enabled = false;
            }
            if (updateActivityWindowButtons)
            {
                var form = (ViewActivityForm)Application.OpenForms["ViewActivityForm"];
                if (form != null)
                    form.UpdateButtons(false);
            }
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
