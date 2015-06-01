using System;
using System.IO;

namespace VersionOne.TeamSync.Service.Core
{
    public static class SimpleLogger
    {

        private const string LOGFILE = "\\VersionOne.TeamSync.Service.Log.txt";

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
