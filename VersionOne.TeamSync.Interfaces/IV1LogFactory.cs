using System.ComponentModel.Composition;

namespace VersionOne.TeamSync.Interfaces
{
	[InheritedExport]
	public interface IV1LogFactory
	{
		IV1Log Create<T>() where T : class;
	}
}