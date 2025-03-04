using Microsoft.Extensions.Hosting;
using San.MdbLogging.Models;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace San.MdbLogging.BgTasks
{
    public class QueuedHostedService<T> : BackgroundService where T : IBaseModel
    {
        private readonly IBackgroundTaskQueue<T> _taskQueue;

        public QueuedHostedService(IBackgroundTaskQueue<T> taskQueue)
        {
            _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
        }

                                                protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Debug.WriteLine("QueuedHostedService is starting.");

                        while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                                        var workItem = await _taskQueue.DequeueAsync(stoppingToken);

                    if (workItem != null)
                    {
                                                await workItem.WorkFunction(workItem.Item, stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                                        Debug.WriteLine("QueuedHostedService is stopping due to cancellation.");
                }
                catch (Exception ex)
                {
                                        Debug.WriteLine($"QueuedHostedService encountered an error: {ex.Message}");
                }
            }

            Debug.WriteLine("QueuedHostedService has stopped.");
        }

                                                public override async Task StopAsync(CancellationToken stoppingToken)
        {
            Debug.WriteLine("QueuedHostedService is stopping.");
            await base.StopAsync(stoppingToken);
        }
    }
}