using System;
using System.Drawing;
using System.ServiceProcess;
using System.Windows.Forms;
using VersionOne.TeamSync.SystemTray.Properties;
using VersionOne.TeamSync.Core;

namespace VersionOne.TeamSync.SystemTray
{
    public partial class ViewActivityForm : Form
    {
        private LogLevel _levelToShow = LogLevel.ALL;

        public ViewActivityForm()
        {
            InitializeComponent();
            InitializeLogLevelComboBox();
            UpdateServiceControlButtons();
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
                    if ((int)level >= (int)_levelToShow)
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

        delegate void UpdateServiceControlButtonsCallback();

        public void UpdateServiceControlButtons()
        {
            if (richTextBox1.InvokeRequired)
            {
                UpdateServiceControlButtonsCallback d = new UpdateServiceControlButtonsCallback(UpdateServiceControlButtons);
                Invoke(d, new object[] {});
            }
            else
            {
                if (TeamSyncServiceController.IsServiceInstalled())
                {

                    var serviceStatus = TeamSyncServiceController.GetServiceStatus();
                    this.toolStripStartButton.Enabled =
                        serviceStatus == ServiceControllerStatus.Stopped;
                    this.toolStripRecyleButton.Enabled =
                        serviceStatus == ServiceControllerStatus.Running;
                    this.toolStripStopButton.Enabled =
                        serviceStatus == ServiceControllerStatus.Running;
                }
                else
                {
                    this.toolStripStartButton.Enabled = false;
                    this.toolStripStopButton.Enabled = false;
                    this.toolStripRecyleButton.Enabled = false;
                }
            }
        }

        private Color GetLevelColor(LogLevel level)
        {
            if (level == LogLevel.DEBUG)
                return Color.Green;
            if (level == LogLevel.INFO)
                return Color.White;
            if (level == LogLevel.TRACE)
                return Color.Yellow;
            if (level == LogLevel.ERROR)
                return Color.Red;

            return Color.White;
        }

        private void ViewActivityForm_Load(object sender, EventArgs e)
        {
            var defaultLocation = Settings.Default.ActivityWindowLocation;
            var defaultSize = Settings.Default.ActivityWindowSize;
            LogLevel defaultLogLevel;

            if (Enum.TryParse(Settings.Default.LogLevel, out defaultLogLevel))
            {
                this.toolStripComboBox1.SelectedItem = defaultLogLevel.ToString();
            }
            if (defaultLocation != new Point(-1, -1))
            {
                this.Location = defaultLocation;
            }
            if (defaultSize != new Size(0, 0))
            {
                this.Size = defaultSize;
            }
        }

        private void toolStripComboBox1_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            var comboBox = (ToolStripComboBox)sender;
            string selectedLevel = (string)comboBox.SelectedItem;

            _levelToShow = (LogLevel)Enum.Parse(typeof(LogLevel), selectedLevel);
        }

        private void toolStripStartButton_Click(object sender, EventArgs e)
        {
            try
            {
                TeamSyncServiceController.StartService();
                UpdateServiceControlButtons();
            }
            catch (ServiceControllerException ex)
            {
                DialogUtils.ShowServiceControllerException(ex);
            }
        }

        private void toolStripRecyleButton_Click(object sender, EventArgs e)
        {
            try
            {
                TeamSyncServiceController.RecycleService();
                UpdateServiceControlButtons();
            }
            catch (ServiceControllerException ex)
            {
                DialogUtils.ShowServiceControllerException(ex);
            }
        }

        private void toolStripStopButton_Click(object sender, EventArgs e)
        {
            try
            {
                TeamSyncServiceController.StopService();
                UpdateServiceControlButtons();
            }
            catch (ServiceControllerException ex)
            {
                DialogUtils.ShowServiceControllerException(ex);
            }
        }

        private void InitializeLogLevelComboBox()
        {
            toolStripComboBox1.Items.Add(LogLevel.ALL.ToString());
            toolStripComboBox1.Items.Add(LogLevel.INFO.ToString());
            toolStripComboBox1.Items.Add(LogLevel.DEBUG.ToString());
            toolStripComboBox1.Items.Add(LogLevel.TRACE.ToString());
            toolStripComboBox1.Items.Add(LogLevel.ERROR.ToString());
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
            Settings.Default.LogLevel = toolStripComboBox1.SelectedItem.ToString();
            Settings.Default.Save();
        }
    }
}
