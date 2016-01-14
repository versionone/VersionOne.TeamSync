using System;
using System.Collections.Generic;
using System.Net;

namespace VersionOne.TeamSync.TfsConnector.Interfaces
{
	public interface ITfsConnector
	{
		string BaseUrl { get; }
		bool IsConnectionValid();
		bool ProjectExists(string projectIdOrKey);
	}
}