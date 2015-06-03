using System;
using System.Drawing;
using System.Windows.Forms;

namespace VersionOne.TeamSync.SystemTray
{
    public partial class ViewActivityForm : Form
    {
        private LogLevel _levelToShow = LogLevel.ALL;
        private RemoteLoggingSink rls;

        public ViewActivityForm()
        {
            InitializeComponent();
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
            toolStripComboBox1.SelectedIndex = 0;
        }

        private void toolStripComboBox1_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            var comboBox = (ToolStripComboBox)sender;
            string selectedLevel = (string)comboBox.SelectedItem;

            _levelToShow = (LogLevel)Enum.Parse(typeof(LogLevel), selectedLevel);
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
