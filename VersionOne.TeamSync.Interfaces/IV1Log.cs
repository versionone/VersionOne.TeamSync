using System;
using System.ComponentModel.Composition;

namespace VersionOne.TeamSync.Interfaces
{
	public interface IV1Log
	{
		void DebugClosedItem(string singularAsset, string assetNumber);
		void DebugFormat(string format, params object[] args);
		void Info(object message);
		void InfoFormat(string format, params object[] args);
		void InfoClosed(int count, string pluralAsset);
		void InfoCreated(int count, string pluralAsset);
		void InfoDelete(int count, string pluralAsset);
		void InfoReopen(int count, string pluralAsset);
		void InfoUpdated(int count, string pluralAsset);
		void Trace(string message, Exception exception = null);
		void TraceCreateFinished(string pluralAsset);
		void TraceDeleteFinished(string pluralAsset);
		void TraceFormat(string format, params object[] args);
		void TraceUpdateFinished(string pluralAsset);
		void Verbose(string message);
		void Verbose(string message, Exception exception);
	}
}