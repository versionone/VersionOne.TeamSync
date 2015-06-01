using System.ServiceProcess;
namespace VersionOne.TeamSync.Service
{
    static class Program
    {
        static void Main()
        {
#if DEBUG
            Service1 service = new Service1();
            service.OnDebugStart();
            System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
#else
            var ServicesToRun = new ServiceBase[] 
            { 
                new Service1() 
            };
            ServiceBase.Run(ServicesToRun);
#endif
        }
    }
}
