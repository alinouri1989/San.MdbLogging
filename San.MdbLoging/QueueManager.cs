using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoLogger.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MongoLogger
{
    public class QueueManager<T> : IQueueManager<T> where T : BaseMongoModel
    {
        Queue<T> _queue;
        int _batchSize = -1;
        string _colName;
        ILogService<T> _logService;
        IOptions<LogDatabaseSettings> _options;

        private readonly IServiceProvider _ServiceProvider;

        public QueueManager(IOptions<LogDatabaseSettings> options, IServiceProvider serviceProvider, ILogService<T> logService, int batchSize = -1, string colName = null)
        {
            _ServiceProvider = serviceProvider;
            _batchSize = batchSize;
            _colName = colName;
            _options = options;

            _queue = new Queue<T>();
        }
        public async Task AddToQue(T item)
        {
            _logService = (LogService<T>)ActivatorUtilities.CreateInstance(_ServiceProvider, typeof(LogService<T>));
            var bSize = _batchSize == -1 ? _options.Value.BatchSize : _batchSize;
            _queue.Enqueue(item);
            if (_queue.Count >= bSize)
            {
                await _logService.Create(_queue.ToArray());
                _queue.Clear();
            }
        }
    }

    public interface IQueueManager<T>
    {
        Task AddToQue(T item);
    }
}
