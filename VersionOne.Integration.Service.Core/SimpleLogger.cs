using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionOne.Integration.Service.Core
{
    public static class SimpleLogger
    {

        private const string LOGFILE = "\\VersionOne.Integration.Service.Log.txt";

        public static void WriteLogException(Exception ex)
        {
            var message = DateTime.Now + ": " + ex.Source + "; " + ex.Message.Trim();
            File.AppendAllLines(AppDomain.CurrentDomain.BaseDirectory + LOGFILE, new[] { message });
        }

        public static void WriteLogMessage(string message)
        {
            File.AppendAllLines(AppDomain.CurrentDomain.BaseDirectory + LOGFILE, new []{ message });
        }
    }
}
