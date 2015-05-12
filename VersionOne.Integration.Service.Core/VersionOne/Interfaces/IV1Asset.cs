using System.Xml.Linq;

namespace VersionOne.Integration.Service.Core.VersionOne.Interfaces
{
	public interface IV1Asset
	{
		XDocument ToPostPayload();
		string AssetType { get;  }
		string ID { get; }
	}
}