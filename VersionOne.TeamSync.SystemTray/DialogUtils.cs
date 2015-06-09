using System.Windows.Forms;

namespace VersionOne.TeamSync.SystemTray
{
    public abstract class DialogUtils
    {
        public static void ShowServiceControllerException(ServiceControllerException ex)
        {
            MessageBox.Show(ex.Message,
                ex.InnerException != null ? ex.InnerException.Message : "",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error,
                MessageBoxDefaultButton.Button1);
        }
    }
}