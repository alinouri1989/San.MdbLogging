using Microsoft.Extensions.Hosting;
using San.MDbLogging.Models;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace San.MDbLogging.BgTasks
{
    public class QueuedHostedService<T> : BackgroundService where T : IBaseModel
    {
        private readonly IBackgroundTaskQueue<T> _taskQueue;

        public QueuedHostedService(IBackgroundTaskQueue<T> taskQueue)
        {
            _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
        }

        /// <summary>  
        /// Executes the background processing loop.  
        /// </summary>  
        /// <param name="stoppingToken">Token to signal cancellation.</param>  
        /// <returns>A task that represents the execution.</returns>  
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Debug.WriteLine("QueuedHostedService is starting.");

            // Main processing loop  
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Dequeue the next work item  
                    var workItem = await _taskQueue.DequeueAsync(stoppingToken);

                    if (workItem != null)
                    {
                        // Execute the work item's function  
                        await workItem.WorkFunction(workItem.Item, stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Gracefully handle cancellation  
                    Debug.WriteLine("QueuedHostedService is stopping due to cancellation.");
                }
                catch (Exception ex)
                {
                    // Log or handle unexpected errors  
                    Debug.WriteLine($"QueuedHostedService encountered an error: {ex.Message}");
                }
            }

            Debug.WriteLine("QueuedHostedService has stopped.");
        }

        /// <summary>  
        /// Stops the background service gracefully.  
        /// </summary>  
        /// <param name="stoppingToken">Token to signal cancellation.</param>  
        /// <returns>A task that represents the stop operation.</returns>  
        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            Debug.WriteLine("QueuedHostedService is stopping.");
            await base.StopAsync(stoppingToken);
        }
    }
}