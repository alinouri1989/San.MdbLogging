using Microsoft.Extensions.Options;
using MongoLogger.Models;
using San.MdbLogging.Models;
using San.SqlLogging;

namespace San.MdbLogging;

public class QueueManagerSql<T> where T : BaseSqlModel
{
    private Queue<T> _queue;
    private int _batchSize = -1;
    private ILogServiceSql<T, LogDbContext<T>> _logService;
    private IOptions<LogDatabaseSettings> _options;

    public QueueManagerSql(IOptions<LogDatabaseSettings> options,
                           IServiceProvider serviceProvider,
                           ILogServiceSql<T, LogDbContext<T>> logService)
    {
        _options = options;
        _logService = logService;
        _queue = new Queue<T>();
        _batchSize = options.Value.BatchSize;
    }

    internal async Task AddToQue(T item)
    {
        int bSize = ((_batchSize == -1) ? _options.Value.BatchSize : _batchSize);
        _queue.Enqueue(item);
        if (_queue.Count >= bSize)
        {
            await _logService.AddAllLog(_queue.ToList());
            _queue.Clear();
        }
    }
}