using System;
using System.Threading.Tasks;
using VersionOne.TeamSync.Worker.Domain;

namespace VersionOne.TeamSync.Worker
{
    public interface IAsyncWorker
    {
        Task DoWork(IJira jiraInstance);
        Task DoFirstRun(IJira jiraInstance);
    }
}