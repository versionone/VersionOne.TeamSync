using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionOne.TeamSync.Interfaces;

namespace VersionOne.TeamSync.Core
{
	public class V1Log : IV1Log
	{
		private ILog _log;

		public V1Log(ILog log)
		{
			_log = log;
		}

		public void Info(object message)
		{
			_log.Info(message);
		}

		public void InfoFormat(string format, params object[] args)
		{
			_log.InfoFormat(format, args);
		}

		public void Trace(string message, Exception exception = null)
		{
			_log.Logger.Log(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
				log4net.Core.Level.Trace, message, exception);
		}

		public void TraceFormat(string format, params object[] args)
		{
			_log.Logger.Log(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
				log4net.Core.Level.Trace, string.Format(format, args), null);
		}

		public void Verbose(string message, Exception exception)
		{
			_log.Logger.Log(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
				log4net.Core.Level.Verbose, message, exception);
		}

		public void Verbose(string message)
		{
			_log.Verbose(message, null);
		}

		private static readonly string _infoCreatedMessage = "Created {0} V1 {1}";
		private static readonly string _traceCreateFinished = "Finished creating V1 {0}";
		private static readonly string _infoUpdatedMessage = "Updated {0} V1 {1}";
		private static readonly string _traceUpdateFinished = "Finished updating V1 {0}";
		private static readonly string _infoDeleteMessage = "Deleted {0} V1 {1}";
		private static readonly string _traceDeleteFinished = "Finished deleting V1 {0}";
		private static readonly string _debugClosedMessage = "Closed V1 {0} {1}";
		private static readonly string _infoClosedMessage = "Closed {0} V1 {1}";
		private static readonly string _infoReopenMessage = "Reopened {0} V1 {1}";

		public void InfoCreated(int count, string pluralAsset)
		{
			_log.Info(string.Format(_infoCreatedMessage, count, pluralAsset));
		}

		public void TraceCreateFinished(string pluralAsset)
		{
			_log.Trace(string.Format(_traceCreateFinished, pluralAsset));
		}

		public void InfoUpdated(int count, string pluralAsset)
		{
			_log.Info(string.Format(_infoUpdatedMessage, count, pluralAsset));
		}

		public void TraceUpdateFinished(string pluralAsset)
		{
			_log.Trace(string.Format(_traceUpdateFinished, pluralAsset));
		}

		public void InfoDelete(int count, string pluralAsset)
		{
			_log.Info(string.Format(_infoDeleteMessage, count, pluralAsset));
		}

		public void TraceDeleteFinished(string pluralAsset)
		{
			_log.Trace(string.Format(_traceDeleteFinished, pluralAsset));
		}

		public void DebugClosedItem(string singularAsset, string assetNumber)
		{
			_log.Debug(string.Format(_debugClosedMessage, singularAsset, assetNumber));
		}

		public void DebugFormat(string format, params object[] args)
		{
			_log.DebugFormat(format, args);
		}

		public void InfoClosed(int count, string pluralAsset)
		{
			_log.Info(string.Format(_infoClosedMessage, count, pluralAsset));
		}

		public void InfoReopen(int count, string pluralAsset)
		{
			_log.Info(string.Format(_infoReopenMessage, count, pluralAsset));
		}
	}
}