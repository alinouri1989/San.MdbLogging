using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using San.MDbLogging.BgTasks;
using San.MDbLogging.Models;

namespace San.MDbLogging;
public class LogManager<T> where T : BaseMongoModel
{
    private QueueManager<T> _queueManager;

    private string _dbColName;

    private int _batchSize;

    private IBackgroundTaskQueue<T> _backgroundTaskQueue;

    private static IHttpContextAccessor _httpContextAccessor;

    public LogManager(IServiceProvider serviceProvider,
        IOptions<LogDatabaseSettings> options, 
        QueueManager<T> queueManager, 
        string dbColName = null, 
        int batchSize = -1)
    {
        _dbColName = dbColName;
        _batchSize = batchSize;
        _backgroundTaskQueue = (IBackgroundTaskQueue<T>)serviceProvider.GetService(typeof(IBackgroundTaskQueue<T>));
        _queueManager = queueManager;
        if (!string.IsNullOrEmpty(dbColName) || batchSize != -1)
        {
            LogService<T> logService = (LogService<T>)serviceProvider.GetService(typeof(LogService<T>));
            _queueManager = new QueueManager<T>(options, serviceProvider, logService, batchSize, dbColName);
        }
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
        _backgroundTaskQueue.QueueBackgroundWorkItem(log, async (model, ct) => await LogInternal(model));
    }

    internal async Task LogInternal(T log)
    {
        try
        {
            await _queueManager.AddToQue(log);
        }
        catch (Exception ex2)
        {
            Exception ex = ex2;
            throw ex;
        }
    }
}