using System;
using System.Linq;
using System.ServiceProcess;
using Microsoft.Win32;

namespace VersionOne.TeamSync.SystemTray
{
    public abstract class TeamSyncServiceController
    {
        private const string ServiceName = "VersionOne.TeamSync.Service";

        private const string NoAdminPrivilegesMessage =
            "Unable to perform this action. The VersionOne TeamSync system tray application must be running with administrator privileges to control the TeamSync service.";
        private const string TimeoutMessage =
            "TeamSync service is taking too long to respond. If this problem continues, please contact your system administrator.";
        private static readonly TimeSpan TimeOutTimeSpan = new TimeSpan(0, 0, 0, 5, 0); // 5 sec
        private static bool? _isServiceInstalled = null;

        public static bool IsServiceInstalled()
        {
            if (_isServiceInstalled == null)
            {
                ServiceController[] services = ServiceController.GetServices();
                _isServiceInstalled = services.Any(service => service.ServiceName == ServiceName);
            }

            return _isServiceInstalled.Value;
        }

        public static string GetServicePath()
        {
            ValidateServiceInstallation();
            var registryPath = @"SYSTEM\CurrentControlSet\Services\" + ServiceName;
            var keyHKLM = Registry.LocalMachine;
            var key = keyHKLM.OpenSubKey(registryPath);
            var path = key.GetValue("ImagePath").ToString();
            key.Close();

            return Environment.ExpandEnvironmentVariables(path).Trim(new char[] { '"' });
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
            catch (InvalidOperationException ioex)
            {
                throw new ServiceControllerException(NoAdminPrivilegesMessage, ioex.InnerException);
            }
            catch (System.ServiceProcess.TimeoutException toex)
            {
                throw new ServiceControllerException(TimeoutMessage);
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
                throw new ServiceControllerException(NoAdminPrivilegesMessage, ex.InnerException);
            }
            catch (System.ServiceProcess.TimeoutException toex)
            {
                throw new ServiceControllerException(TimeoutMessage);
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
                throw new ServiceControllerException(NoAdminPrivilegesMessage, ex.InnerException);
            }
            catch (System.ServiceProcess.TimeoutException toex)
            {
                throw new ServiceControllerException(TimeoutMessage);
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
                throw new ServiceControllerException(NoAdminPrivilegesMessage, ex.InnerException);
            }
            catch (System.ServiceProcess.TimeoutException toex)
            {
                throw new ServiceControllerException(TimeoutMessage);
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
        public ServiceControllerException(string message, Exception innerExecption = null) : base(message, innerExecption) { }
        public ServiceControllerException(string message) : base(message) { }
    }
}