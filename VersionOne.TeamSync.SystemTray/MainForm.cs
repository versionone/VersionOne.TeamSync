using System;
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
        }

        private void startServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ServiceController sc = new ServiceController("VersionOne.TeamSync.Service");
            sc.Start();
        }

        private void stopServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ServiceController sc = new ServiceController("VersionOne.TeamSync.Service");
            sc.Stop();

            TimeSpan ts = new TimeSpan(0, 0, 0, 3, 0); // 3 sec
            sc.WaitForStatus(System.ServiceProcess.ServiceControllerStatus.Stopped, ts);

            if (sc.Status == System.ServiceProcess.ServiceControllerStatus.Stopped)
            {
                this.contextMenuStrip1.Items["stopServiceToolStripMenuItem"].Enabled = false;
                this.contextMenuStrip1.Items["startServiceToolStripMenuItem"].Enabled = true;
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

    }
}
