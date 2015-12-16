using System.Net;

namespace VersionOne.TeamSync.Interfaces
{
    public interface IProxyProvider
    {
        IWebProxy CreateWebProxy();
    }
}