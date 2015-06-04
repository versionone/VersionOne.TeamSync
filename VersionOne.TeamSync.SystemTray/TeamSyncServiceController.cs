using System;
using System.Linq;
using System.ServiceProcess;

namespace VersionOne.TeamSync.SystemTray
{
    public abstract class TeamSyncServiceController
    {
        private const string ServiceName = "VersionOne.TeamSync.Service";
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
            var serviceController = GetServiceController();
            serviceController.Start();
            serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeOutTimeSpan);
        }

        public static void PauseService()
        {
            var serviceController = GetServiceController();
            serviceController.Pause();
            serviceController.WaitForStatus(ServiceControllerStatus.Paused, TimeOutTimeSpan);
        }

        public static void ContinueService()
        {
            var serviceController = GetServiceController();
            serviceController.Continue();
            serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeOutTimeSpan);
        }

        public static void StopService()
        {
            var serviceController = GetServiceController();
            serviceController.Stop();
            serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeOutTimeSpan);
        }

        public static void RecycleService()
        {
            StopService();
            StartService();
        }

        public static ServiceControllerStatus GetServiceStatus()
        {
            return GetServiceController().Status;
        }

        private static ServiceController GetServiceController()
        {
            return new ServiceController(ServiceName);
        }
    }
}