using Microsoft.Extensions.Options;
using San.MdbLogging.Models;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace San.MdbLogging.BgTasks
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

            _batchSize = options.Value.BatchSize;         }

                                                public void QueueBackgroundWorkItem(T item, Func<T, CancellationToken, Task> workFunction)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (workFunction == null) throw new ArgumentNullException(nameof(workFunction));

            var workItem = new WorkItem<T>(item, workFunction);
            _workItems.Enqueue(workItem);
            _signal.Release();         }

                                                public async Task<WorkItem<T>> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);             _workItems.TryDequeue(out WorkItem<T> workItem);
            return workItem;         }
    }

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

                    public interface IBackgroundTaskQueue<T> where T : IBaseModel
    {
        void QueueBackgroundWorkItem(T item, Func<T, CancellationToken, Task> workFunction);
        Task<WorkItem<T>> DequeueAsync(CancellationToken cancellationToken);
    }
}