using System;
using System.Diagnostics;
using System.Linq;
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
			var service = new Service1();
			if (Environment.UserInteractive)
			{
				Console.WriteLine("Starting service...");
				service.OnDebugStart();
				Console.WriteLine("Service is running.");
				Console.WriteLine("Press any key to stop...");
				Console.ReadKey(true);
				Console.WriteLine("Stopping service...");
				service.Stop();
				Console.WriteLine("Service stopped.");
			}
			else
			{
				var servicesToRun = new ServiceBase[] { service };
				ServiceBase.Run(servicesToRun);
			};
#endif
		}
	}
}
