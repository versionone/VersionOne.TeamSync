using System;
using System.Linq;
using System.ServiceProcess;
using log4net;
using Microsoft.Win32;

namespace VersionOne.TeamSync.SystemTray
{
    public abstract class TeamSyncServiceController
    {
        private const string ServiceName = "VersionOne.TeamSync.Service";
        private const string NoAdminPrivilegesMessage =
            "Unable to perform this action. The VersionOne TeamSync system tray application must be running with administrator privileges to control the TeamSync service.";
        private const string TimeoutMessageFormat =
            "TeamSync service is taking too long to respond. Timeout value is set to {0} secs";

        private static readonly ILog Log = LogManager.GetLogger(typeof(TeamSyncServiceController));
        private static readonly TimeSpan TimeOutTimeSpan = TimeSpan.FromSeconds(15);
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

            return path;
        }

        public static void StartService()
        {
            ValidateServiceInstallation();
            try
            {
                Log.Info("Attempting to start TeamSync Service from SystemTray");
                var serviceController = GetServiceController();
                serviceController.Start();
                serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeOutTimeSpan);
                Log.Info("TeamSync Service started");
            }
            catch (InvalidOperationException ex)
            {
                Log.Error(ex);
                throw new ServiceControllerException(NoAdminPrivilegesMessage, ex.InnerException);
            }
            catch (System.ServiceProcess.TimeoutException)
            {
                Log.WarnFormat(TimeoutMessageFormat, TimeOutTimeSpan.TotalSeconds);
            }
        }

        public static void PauseService()
        {
            ValidateServiceInstallation();
            try
            {
                Log.Info("Attempting to pause TeamSync Service from SystemTray");
                var serviceController = GetServiceController();
                serviceController.Pause();
                serviceController.WaitForStatus(ServiceControllerStatus.Paused, TimeOutTimeSpan);
                Log.Info("TeamSync Service paused");
            }
            catch (InvalidOperationException ex)
            {
                Log.Error(ex);
                throw new ServiceControllerException(NoAdminPrivilegesMessage, ex.InnerException);
            }
            catch (System.ServiceProcess.TimeoutException)
            {
                Log.WarnFormat(TimeoutMessageFormat, TimeOutTimeSpan.TotalSeconds);
            }
        }

        public static void ContinueService()
        {
            ValidateServiceInstallation();
            try
            {
                Log.Info("Attempting to continue TeamSync Service from SystemTray");
                var serviceController = GetServiceController();
                serviceController.Continue();
                serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeOutTimeSpan);
                Log.Info("TeamSync Service continued");
            }
            catch (InvalidOperationException ex)
            {
                Log.Error(ex);
                throw new ServiceControllerException(NoAdminPrivilegesMessage, ex.InnerException);
            }
            catch (System.ServiceProcess.TimeoutException)
            {
                Log.WarnFormat(TimeoutMessageFormat, TimeOutTimeSpan.TotalSeconds);
            }
        }

        public static void StopService()
        {
            try
            {
                Log.Info("Attempting to stop TeamSync Service from SystemTray");
                var serviceController = GetServiceController();
                serviceController.Stop();
                serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeOutTimeSpan);
                Log.Info("TeamSync Service stopped");
            }
            catch (InvalidOperationException ex)
            {
                Log.Error(ex);
                throw new ServiceControllerException(NoAdminPrivilegesMessage, ex.InnerException);
            }
            catch (System.ServiceProcess.TimeoutException)
            {
                Log.WarnFormat(TimeoutMessageFormat, TimeOutTimeSpan.TotalSeconds);
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