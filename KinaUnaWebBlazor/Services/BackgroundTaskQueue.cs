﻿using System.Collections.Concurrent;

namespace KinaUnaWebBlazor.Services
{
    // Source: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-2.1#queued-background-tasks

    /// <summary>
    /// A service for executing long running tasks in the background.
    /// For example database maintenance, moving files, batch photo editing.
    /// </summary>
    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly ConcurrentQueue<Func<CancellationToken, Task>?> _workItems = new();
        private readonly SemaphoreSlim _signal = new(0);

        public void QueueBackgroundWorkItem(
            Func<CancellationToken, Task>? workItem)
        {
            ArgumentNullException.ThrowIfNull(workItem);

            _workItems.Enqueue(workItem);
            _signal.Release();
        }

        public async Task<Func<CancellationToken, Task>?> DequeueAsync(
            CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            _workItems.TryDequeue(out Func<CancellationToken, Task>? workItem);

            return workItem;
        }
    }
}
