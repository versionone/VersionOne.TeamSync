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

	    private static string _infoCreatedMessage = "Created {0} V1 {1}";
		private static string _traceCreateFinished = "Finished creating V1 {0}";
		private static string _infoUpdatedMessage = "Updated {0} V1 {1}";
		private static string _traceUpdateFinished = "Finished updating V1 {0}";
		private static string _infoDeleteMessage = "Deleted {0} V1 {1}";
		private static string _traceDeleteFinished = "Finished deleting V1 {0}";
		private static string _debugClosedMessage = "Closed V1 {0} {1}";
        private static string _infoClosedMessage = "Closed {0} V1 {1}";
        private static string _infoReopenMessage = "Reopened {0} V1 {1}";

	    public static void InfoCreated(this ILog log, int count, string pluralAsset)
	    {
		    log.Info(string.Format(_infoCreatedMessage, count, pluralAsset));
	    }

	    public static void TraceCreateFinished(this ILog log, string pluralAsset)
	    {
			log.Trace(string.Format(_traceCreateFinished, pluralAsset));
	    }

		public static void InfoUpdated(this ILog log, int count, string pluralAsset)
		{
			log.Info(string.Format(_infoUpdatedMessage, count, pluralAsset));
		}

		public static void TraceUpdateFinished(this ILog log, string pluralAsset)
		{
			log.Trace(string.Format(_traceUpdateFinished, pluralAsset));
		}

		public static void InfoDelete(this ILog log, int count, string pluralAsset)
		{
			log.Info(string.Format(_infoDeleteMessage, count, pluralAsset));
		}

		public static void TraceDeleteFinished(this ILog log, string pluralAsset)
		{
			log.Trace(string.Format(_traceDeleteFinished, pluralAsset));
		}

	    public static void DebugClosedItem(this ILog log, string singularAsset, string assetNumber)
	    {
		    log.Debug(string.Format(_debugClosedMessage, singularAsset, assetNumber));
	    }

        public static void InfoClosed(this ILog log, int count, string pluralAsset)
        {
            log.Info(string.Format(_infoClosedMessage, count, pluralAsset));
        }

        public static void InfoReopen(this ILog log, int count, string pluralAsset)
        {
            log.Info(string.Format(_infoReopenMessage, count, pluralAsset));
        }
    }
}