using System;
using log4net;

namespace VersionOne.TeamSync.Core
{
    public static class ILogExtensions
    {
        public static void Trace(this ILog log, string message, Exception exception = null)
        {
            log.Logger.Log(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                log4net.Core.Level.Trace, message, exception);
        }

        public static void TraceFormat(this ILog log, string format, params object[] args)
        {
            log.Logger.Log(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                log4net.Core.Level.Trace, string.Format(format, args), null);
        }

        public static void Verbose(this ILog log, string message, Exception exception)
        {
            log.Logger.Log(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                log4net.Core.Level.Verbose, message, exception);
        }

        public static void Verbose(this ILog log, string message)
        {
            log.Verbose(message, null);
        }
    }
}