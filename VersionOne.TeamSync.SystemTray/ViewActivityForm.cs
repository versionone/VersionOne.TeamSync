using System;
using System.Drawing;
using System.ServiceProcess;
using System.Windows.Forms;
using VersionOne.TeamSync.SystemTray.Properties;

namespace VersionOne.TeamSync.SystemTray
{
    public partial class ViewActivityForm : Form
    {
        private LogLevel _levelToShow = LogLevel.ALL;

        public ViewActivityForm()
        {
            InitializeComponent();
            UpdateButtons();
        }

        delegate void AppendTextCallback(string text, LogLevel level);

        public void AppendText(string text, LogLevel level)
        {
            if (!richTextBox1.IsDisposed)
            {
                if (richTextBox1.InvokeRequired)
                {
                    AppendTextCallback d = new AppendTextCallback(AppendText);
                    Invoke(d, new object[] {text, level});
                }
                else
                {
                    if (_levelToShow == LogLevel.ALL || level == _levelToShow)
                    {
                        richTextBox1.SelectionStart = richTextBox1.TextLength;
                        richTextBox1.SelectionLength = 0;
                        richTextBox1.SelectionColor = GetLevelColor(level);
                        richTextBox1.AppendText(text);
                        richTextBox1.SelectionColor = richTextBox1.ForeColor;
                    }
                }
            }
        }

        private Color GetLevelColor(LogLevel level)
        {
            if (level == LogLevel.DEBUG)
                return Color.DarkGreen;
            if (level == LogLevel.INFO)
                return Color.White;
            if (level == LogLevel.WARN)
                return Color.Yellow;
            if (level == LogLevel.ERROR)
                return Color.Red;

            return Color.White;
        }

        private void ViewActivityForm_Load(object sender, EventArgs e)
        {
            var defaultLocation = Settings.Default.ActivityWindowLocation;
            var defaultSize = Settings.Default.ActivityWindowSize;
            
            if (defaultLocation != new Point(-1, -1))
            {
                this.Location = defaultLocation;
            }
            if (defaultSize != new Size(0, 0))
            {
                this.Size = defaultSize;
            }
            
            toolStripComboBox1.SelectedIndex = 0;
        }

        private void toolStripComboBox1_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            var comboBox = (ToolStripComboBox)sender;
            string selectedLevel = (string)comboBox.SelectedItem;

            _levelToShow = (LogLevel)Enum.Parse(typeof(LogLevel), selectedLevel);
        }

        private void toolStripStartButton_Click(object sender, EventArgs e)
        {
            TeamSyncServiceController.StartService();
            UpdateButtons();
        }

        private void toolStripPasueButton_Click(object sender, EventArgs e)
        {
            //var serviceStatus = TeamSyncServiceController.GetServiceStatus();
            //if (serviceStatus == ServiceControllerStatus.Running)
            //{
            //    TeamSyncServiceController.PauseService();
            //    UpdateButtons();
            //}
            //else if (serviceStatus == ServiceControllerStatus.Paused)
            //{
            //    TeamSyncServiceController.ContinueService();
            //    UpdateButtons();
            //}
        }

        private void toolStripRecyleButton_Click(object sender, EventArgs e)
        {
            TeamSyncServiceController.RecycleService();
            UpdateButtons();
        }

        private void toolStripStopButton_Click(object sender, EventArgs e)
        {
            TeamSyncServiceController.StopService();
            UpdateButtons();
        }

        public void UpdateButtons(bool updateContextMenuStrip = true)
        {
            if (TeamSyncServiceController.IsServiceInstalled())
            {

                var serviceStatus = TeamSyncServiceController.GetServiceStatus();
                this.toolStripStartButton.Enabled = 
                    serviceStatus == ServiceControllerStatus.Stopped;
                this.toolStripPasueButton.Enabled = 
                    serviceStatus == ServiceControllerStatus.Running 
                    || serviceStatus == ServiceControllerStatus.Paused;
                this.toolStripRecyleButton.Enabled = 
                    serviceStatus == ServiceControllerStatus.Running;
                this.toolStripStopButton.Enabled = 
                    serviceStatus == ServiceControllerStatus.Running;
                if (serviceStatus == ServiceControllerStatus.Running)
                {
                    //this.contextMenuStrip1.Items["pauseServiceToolStripMenuItem"].Text = "Pause";
                }
                else if (serviceStatus == ServiceControllerStatus.Paused)
                {
                    //this.contextMenuStrip1.Items["pauseServiceToolStripMenuItem"].Text = "Continue";
                }
            }
            else
            {
                this.toolStripStartButton.Enabled = false;
                this.toolStripStopButton.Enabled = false;
                //this.contextMenuStrip1.Items["pauseServiceToolStripMenuItem"].Enabled = false;
                this.toolStripRecyleButton.Enabled = false;
            }

            if (updateContextMenuStrip)
            {
                var form = (SystemTray) Application.OpenForms["SystemTray"];
                if (form != null)
                    form.UpdateContextMenuStrip(false);
            }
        }

        private void ViewActivityForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Settings.Default.ActivityWindowLocation = this.Location;
            if (this.WindowState == FormWindowState.Normal)
            {
                Settings.Default.ActivityWindowSize = this.Size;
            }
            else
            {
                Settings.Default.ActivityWindowSize = this.RestoreBounds.Size;
            }
            Settings.Default.Save();
        }
    }

    public enum LogLevel
    {
        ALL,
        DEBUG,
        INFO,
        WARN,
        ERROR
    }
}
