using System;
using System.Windows.Forms;
using log4net.Appender;
using log4net.Core;

namespace VersionOne.TeamSync.SystemTray
{
    public class RemoteLoggingSink : MarshalByRefObject, RemotingAppender.IRemoteLoggingSink
    {
        public void LogEvents(LoggingEvent[] events)
        {
            ViewActivityForm form = (ViewActivityForm)Application.OpenForms["ViewActivityForm"];
            if (form == null)
                return;

            foreach (var loggingEvent in events)
            {
                form.AppendText(loggingEvent.RenderedMessage + Environment.NewLine,
                    (LogLevel)Enum.Parse(typeof(LogLevel), loggingEvent.Level.Name));
            }
        } 
    }
}