using System;
using System.Collections.Generic;
using System.Net;

namespace VersionOne.TeamSync.TfsConnector.Interfaces
{
	public interface ITfsConnector
	{
		bool IsConnectionValid();
		bool ProjectExists(string projectIdOrKey);
	}
}