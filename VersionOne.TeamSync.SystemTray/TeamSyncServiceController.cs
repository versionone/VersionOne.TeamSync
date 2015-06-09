using System;
using System.Linq;
using System.ServiceProcess;

namespace VersionOne.TeamSync.SystemTray
{
    public abstract class TeamSyncServiceController
    {
        private const string ServiceName = "VersionOne.TeamSync.Service";

        private const string NoAdminPrivilegesReason =
            "The VersionOne TeamSync system tray application must be running with administrator privileges to control the TeamSync service.";
        private static readonly TimeSpan TimeOutTimeSpan = new TimeSpan(0, 0, 0, 3, 0); // 3 sec
        private static bool? _isServiceInstaleld = null;

        public static bool IsServiceInstalled()
        {
            if (_isServiceInstaleld == null)
            {
                ServiceController[] services = ServiceController.GetServices();
                _isServiceInstaleld = services.Any(service => service.ServiceName == ServiceName);
            }

            return _isServiceInstaleld.Value;
        }

        public static void StartService()
        {
            ValidateServiceInstallation();
            try
            {
                var serviceController = GetServiceController();
                serviceController.Start();
                serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeOutTimeSpan);
            }
            catch (InvalidOperationException ex)
            {
                throw new ServiceControllerException(NoAdminPrivilegesReason, ex.InnerException);
            }
        }

        public static void PauseService()
        {
            ValidateServiceInstallation();
            try
            {
                var serviceController = GetServiceController();
                serviceController.Pause();
                serviceController.WaitForStatus(ServiceControllerStatus.Paused, TimeOutTimeSpan);
            }
            catch (InvalidOperationException ex)
            {
                throw new ServiceControllerException(NoAdminPrivilegesReason, ex.InnerException);
            }
        }

        public static void ContinueService()
        {
            ValidateServiceInstallation();
            try
            {
                var serviceController = GetServiceController();
                serviceController.Continue();
                serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeOutTimeSpan);
            }
            catch (InvalidOperationException ex)
            {
                throw new ServiceControllerException(NoAdminPrivilegesReason, ex.InnerException);
            }
        }

        public static void StopService()
        {
            try
            {
                var serviceController = GetServiceController();
                serviceController.Stop();
                serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeOutTimeSpan);
            }
            catch (InvalidOperationException ex)
            {
                throw new ServiceControllerException(NoAdminPrivilegesReason, ex.InnerException);
            }
        }

        public static void RecycleService()
        {
            StopService();
            StartService();
        }

        public static ServiceControllerStatus GetServiceStatus()
        {
            ValidateServiceInstallation();
            return GetServiceController().Status;
        }

        private static ServiceController GetServiceController()
        {
            return new ServiceController(ServiceName);
        }

        private static void ValidateServiceInstallation()
        {
            if (!IsServiceInstalled())
                throw new ServiceControllerException("Service is not installed");
        }
    }

    public class ServiceControllerException : Exception
    {
        public ServiceControllerException(string reason, Exception innerExecption = null) : base(string.Format("Unable to perform this action. {0}", reason), innerExecption) { }
        public ServiceControllerException(string message) : base(message) { }
    }
}