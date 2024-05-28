namespace KinaUnaWebBlazor.Services
{
    // Source: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-2.1#queued-background-tasks

    public class QueuedHostedService(IBackgroundTaskQueue taskQueue, ILoggerFactory loggerFactory) : BackgroundService
    {
        private readonly ILogger _logger = loggerFactory.CreateLogger<QueuedHostedService>();

        private IBackgroundTaskQueue TaskQueue { get; } = taskQueue;

        protected override async Task ExecuteAsync(
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Queued Hosted Service is starting.");

            while (!cancellationToken.IsCancellationRequested)
            {
                Func<CancellationToken, Task>? workItem = await TaskQueue.DequeueAsync(cancellationToken);

                try
                {
                    if (workItem != null) await workItem(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        $"Error occurred executing {nameof(workItem)}.");
                }
            }

            _logger.LogInformation("Queued Hosted Service is stopping.");
        }
    }
}
