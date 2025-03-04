using Microsoft.Extensions.Hosting;
using MongoLogger.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MongoLogger.BgTasks
{
    public class QueuedHostedService<T> : BackgroundService where T : IBaseModel
    {

        public QueuedHostedService(IBackgroundTaskQueue<T> taskQueue)
        {
            TaskQueue = taskQueue;
        }

        public IBackgroundTaskQueue<T> TaskQueue { get; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Debug.WriteLine("QueuedHostedService is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var workItem = await TaskQueue.DequeueAsync(stoppingToken);

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

            await base.StopAsync(stoppingToken);
        }

    }
}
