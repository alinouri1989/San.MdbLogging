using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson.IO;
using Newtonsoft.Json;
using San.MdbLogging.BgTasks;
using San.MdbLogging.Models;
using San.SqlLogging;

namespace San.MdbLogging;

public class LogManagerSql<T> where T : BaseSqlModel
{
    private QueueManagerSql<T> _queueManager;
    private int _batchSize;
    private IBackgroundTaskQueue<T> _backgroundTaskQueue;
    private static IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<LogManagerSql<T>> _logger;

    public LogManagerSql(IServiceProvider serviceProvider, IOptions<LogDatabaseSettings> options, QueueManagerSql<T> queueManager)
    {
        _batchSize = options.Value.BatchSize;
        _logger = serviceProvider.GetRequiredService<ILogger<LogManagerSql<T>>>();
        _backgroundTaskQueue = (IBackgroundTaskQueue<T>)serviceProvider.GetService(typeof(IBackgroundTaskQueue<T>));
        _queueManager = queueManager;
        if (_batchSize != -1)
        {
            var logService = serviceProvider.GetService<ILogServiceSql<T, LogDbContext<T>>>();
            _queueManager = new QueueManagerSql<T>(options, serviceProvider, logService);
        }
        _logger.LogInformation("Batch size file =>", _batchSize);
    }

    internal static void SetHttpAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Log(T log)
    {
        string text = (string)_httpContextAccessor.HttpContext.Items["LogGuid"];
        if (string.IsNullOrWhiteSpace(text))
        {
            text = Guid.NewGuid().ToString();
            _httpContextAccessor.HttpContext.Items.Add("LogGuid", text);
        }

        log.TraceCode = text;

        _backgroundTaskQueue.QueueBackgroundWorkItem(log, async (log, ct) =>
        {
            _logger.LogInformation("file logger INFO => " + Newtonsoft.Json.JsonConvert.SerializeObject(log));
            await LogInternal(log);
        });
    }

    internal async Task LogInternal(T log)
    {
        try
        {
            await _queueManager.AddToQue(log);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "file logger ERROR => " + ex.Message);
            throw ex;
        }
    }
}