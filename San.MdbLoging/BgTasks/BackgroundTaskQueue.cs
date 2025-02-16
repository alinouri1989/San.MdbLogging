using Microsoft.Extensions.Options;
using San.MDbLogging.Models;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace San.MDbLogging.BgTasks
{
    public class BackgroundTaskQueue<T> : IBackgroundTaskQueue<T> where T : IBaseModel
    {
        private readonly ConcurrentQueue<WorkItem<T>> _workItems = new ConcurrentQueue<WorkItem<T>>();
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);
        private readonly int _batchSize;

        public BackgroundTaskQueue(IOptions<LogDatabaseSettings> options)
        {
            if (options == null || options.Value == null)
            {
                throw new ArgumentNullException(nameof(options), "LogDatabaseSettings options must be provided.");
            }

            _batchSize = options.Value.BatchSize; // Batch size from configuration  
        }

        /// <summary>  
        /// Adds a new work item to the queue.  
        /// </summary>  
        /// <param name="item">The item to process.</param>  
        /// <param name="workFunction">The function to execute for the item.</param>  
        public void QueueBackgroundWorkItem(T item, Func<T, CancellationToken, Task> workFunction)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (workFunction == null) throw new ArgumentNullException(nameof(workFunction));

            var workItem = new WorkItem<T>(item, workFunction);
            _workItems.Enqueue(workItem);
            _signal.Release(); // Signal that a new item is available  
        }

        /// <summary>  
        /// Dequeues a work item from the queue.  
        /// </summary>  
        /// <param name="cancellationToken">A token to cancel the operation.</param>  
        /// <returns>The dequeued work item.</returns>  
        public async Task<WorkItem<T>> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken); // Wait for an item to become available  
            _workItems.TryDequeue(out WorkItem<T> workItem);
            return workItem; // This can be null if the queue was empty, but shouldn't be due to the semaphore  
        }
    }

    /// <summary>  
    /// Represents a work item in the background task queue.  
    /// </summary>  
    /// <typeparam name="T">The type of the item being processed.</typeparam>  
    public class WorkItem<T>
    {
        public T Item { get; }
        public Func<T, CancellationToken, Task> WorkFunction { get; }

        public WorkItem(T item, Func<T, CancellationToken, Task> workFunction)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));
            WorkFunction = workFunction ?? throw new ArgumentNullException(nameof(workFunction));
        }
    }

    /// <summary>  
    /// Interface for a background task queue.  
    /// </summary>  
    /// <typeparam name="T">The type of items in the queue.</typeparam>  
    public interface IBackgroundTaskQueue<T> where T : IBaseModel
    {
        void QueueBackgroundWorkItem(T item, Func<T, CancellationToken, Task> workFunction);
        Task<WorkItem<T>> DequeueAsync(CancellationToken cancellationToken);
    }
}