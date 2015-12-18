using log4net;
using System;
using VersionOne.TeamSync.Interfaces;

namespace VersionOne.TeamSync.Core
{
	public class V1Log : IV1Log
	{
        private static readonly string _infoCreatedMessage = "Created {0} V1 {1}";
        private static readonly string _traceCreateFinished = "Finished creating V1 {0}";
        private static readonly string _infoUpdatedMessage = "Updated {0} V1 {1}";
        private static readonly string _traceUpdateFinished = "Finished updating V1 {0}";
        private static readonly string _infoDeleteMessage = "Deleted {0} V1 {1}";
        private static readonly string _traceDeleteFinished = "Finished deleting V1 {0}";
        private static readonly string _debugClosedMessage = "Closed V1 {0} {1}";
        private static readonly string _infoClosedMessage = "Closed {0} V1 {1}";
        private static readonly string _infoReopenMessage = "Reopened {0} V1 {1}";

		private ILog _log;

		public V1Log(ILog log)
		{
			_log = log;
		}

		public virtual void Info(object message)
		{
			_log.Info(message);
		}

		public virtual void InfoFormat(string format, params object[] args)
		{
			_log.InfoFormat(format, args);
		}

		public virtual void Trace(string message, Exception exception = null)
		{
			_log.Logger.Log(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
				log4net.Core.Level.Trace, message, exception);
		}

		public virtual void TraceFormat(string format, params object[] args)
		{
			_log.Logger.Log(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
				log4net.Core.Level.Trace, string.Format(format, args), null);
		}

	    public virtual void Error(object message)
	    {
	        _log.Error(message);
	    }

	    public virtual void ErrorFormat(string format, params object[] args)
	    {
	        _log.ErrorFormat(format, args);
	    }

	    public virtual void Warn(object message)
	    {
	        _log.Warn(message);
	    }

	    public virtual void WarnFormat(string format, params object[] args)
	    {
	        _log.WarnFormat(format, args);
	    }

	    public virtual void Verbose(string message, Exception exception)
		{
			_log.Logger.Log(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
				log4net.Core.Level.Verbose, message, exception);
		}

		public virtual void Verbose(string message)
		{
			_log.Verbose(message, null);
		}

		public virtual void InfoCreated(int count, string pluralAsset)
		{
			_log.Info(string.Format(_infoCreatedMessage, count, pluralAsset));
		}

		public virtual void TraceCreateFinished(string pluralAsset)
		{
			_log.Trace(string.Format(_traceCreateFinished, pluralAsset));
		}

		public virtual void InfoUpdated(int count, string pluralAsset)
		{
			_log.Info(string.Format(_infoUpdatedMessage, count, pluralAsset));
		}

		public virtual void TraceUpdateFinished(string pluralAsset)
		{
			_log.Trace(string.Format(_traceUpdateFinished, pluralAsset));
		}

		public virtual void InfoDelete(int count, string pluralAsset)
		{
			_log.Info(string.Format(_infoDeleteMessage, count, pluralAsset));
		}

		public virtual void TraceDeleteFinished(string pluralAsset)
		{
			_log.Trace(string.Format(_traceDeleteFinished, pluralAsset));
		}

		public virtual void DebugClosedItem(string singularAsset, string assetNumber)
		{
			_log.Debug(string.Format(_debugClosedMessage, singularAsset, assetNumber));
		}

		public virtual void DebugFormat(string format, params object[] args)
		{
			_log.DebugFormat(format, args);
		}

		public virtual void InfoClosed(int count, string pluralAsset)
		{
			_log.Info(string.Format(_infoClosedMessage, count, pluralAsset));
		}

		public virtual void InfoReopen(int count, string pluralAsset)
		{
			_log.Info(string.Format(_infoReopenMessage, count, pluralAsset));
		}
	}
}