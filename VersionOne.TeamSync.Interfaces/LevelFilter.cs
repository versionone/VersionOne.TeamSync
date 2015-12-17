using System;
using log4net.Core;
using log4net.Filter;

namespace VersionOne.TeamSync.Core
{
	public class LevelFilter : FilterSkeleton
	{
		private Level _level;

		public Level Level
		{
			get { return _level; }
			set { _level = value; }
		}

		public override FilterDecision Decide(LoggingEvent loggingEvent)
		{
			var result = FilterDecision.Deny;
			var logLevel = (LogLevel)Enum.Parse(typeof(LogLevel), loggingEvent.Level.Name);
			var levelToLog = (LogLevel)Enum.Parse(typeof(LogLevel), Level.Name);

			if ((int)logLevel >= (int)levelToLog)
			{
				result = FilterDecision.Accept;
			}

			return result;
		}
	}
}