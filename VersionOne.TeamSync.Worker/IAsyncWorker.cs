using System.Threading.Tasks;
using VersionOne.TeamSync.JiraWorker.Domain;

namespace VersionOne.TeamSync.JiraWorker
{
    public interface IAsyncWorker
    {
        Task DoWork(IJira jiraInstance);
        Task DoFirstRun(IJira jiraInstance);
    }
}