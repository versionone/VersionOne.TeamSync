using System;
using System.Collections.Generic;
using System.Windows.Forms;
using log4net.Appender;
using log4net.Core;
using VersionOne.TeamSync.Core;

namespace VersionOne.TeamSync.SystemTray
{
    public class RemoteLoggingSink : MarshalByRefObject, RemotingAppender.IRemoteLoggingSink
    {
        public void LogEvents(LoggingEvent[] events)
        {
            LogAppend("LOGGING EVENTS!!!");

            ViewActivityForm form = (ViewActivityForm)Application.OpenForms["ViewActivityForm"];
            if (form == null)
                return;

            foreach (var loggingEvent in events)
            {
                LogAppend("LOGGING EVENT: " + loggingEvent.RenderedMessage);
                form.AppendText(loggingEvent.RenderedMessage + Environment.NewLine,
                    (LogLevel)Enum.Parse(typeof(LogLevel), loggingEvent.Level.Name));
            }
        }

        private static void LogAppend(string line)
        {
            System.IO.File.AppendAllLines(@"C:\TEAMSYNCLOG.txt", new List<string>() { line });
        }
    }
}