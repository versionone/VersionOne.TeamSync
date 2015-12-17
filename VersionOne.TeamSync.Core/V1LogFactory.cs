using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionOne.TeamSync.Interfaces;

namespace VersionOne.TeamSync.Core
{
	public class V1LogFactory : IV1LogFactory
	{
		public IV1Log Create<T>() where T : class
		{
			var log = LogManager.GetLogger(typeof(T));
			return new V1Log(log);
		}
	}
}
