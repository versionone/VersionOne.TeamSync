using System.Windows.Forms;

namespace VersionOne.TeamSync.SystemTray
{
    public abstract class DialogUtils
    {
        public static void ShowServiceControllerException(ServiceControllerException ex)
        {
            MessageBox.Show(ex.Message,
                ex.InnerException != null ? ex.InnerException.Message : "Warning",
                MessageBoxButtons.OK,
                ex.InnerException != null ? MessageBoxIcon.Error : MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button1);
        }
    }
}