using System;
using System.ServiceProcess;
using System.Windows.Forms;

namespace VersionOne.TeamSync.Service.SystemTray
{
    public partial class SystemTray : Form
    {
        public SystemTray()
        {
            InitializeComponent();
        }

        private void startServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
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

    }
}
