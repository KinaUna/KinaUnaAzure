using System;
using System.Threading;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services
{
    /// <summary>
    /// A service for executing long running tasks in the background.
    /// For example database maintenance, moving files, batch photo editing.
    /// </summary>
    public interface IBackgroundTaskQueue
    {
        // Source: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-2.1#queued-background-tasks

        void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem);

        Task<Func<CancellationToken, Task>> DequeueAsync(
            CancellationToken cancellationToken);
    }
}
