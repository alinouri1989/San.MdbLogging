using Microsoft.Extensions.Options;
using San.MdbLogging.Models;

namespace San.MdbLogging;

public class QueueManager<T> where T : BaseMongoModel
{
    private Queue<T> _queue;

    private int _batchSize = -1;

    private string _colName;

    private LogService<T> _logService;

    private IOptions<LogDatabaseSettings> _options;

    public QueueManager(IOptions<LogDatabaseSettings> options, IServiceProvider serviceProvider, LogService<T> logService, int batchSize = -1, string colName = null)
    {
        _batchSize = batchSize;
        _colName = colName;
        _options = options;
        _logService = logService;
        _logService = (LogService<T>)(string.IsNullOrWhiteSpace(colName) ? ((object)logService) : ((object)new LogService<T>(options, colName)));
        _queue = new Queue<T>();
    }

    internal async Task AddToQue(T item)
    {
        int bSize = ((_batchSize == -1) ? _options.Value.BatchSize : _batchSize);
        _queue.Enqueue(item);
        if (_queue.Count >= bSize)
        {
            await _logService.Create(_queue.ToArray());
            _queue.Clear();
        }
    }
}