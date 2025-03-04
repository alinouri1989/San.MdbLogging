using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using San.MdbLogging.BgTasks;
using San.MdbLogging.Models;

namespace San.MdbLogging;

public class LogManagerStandard<LType> : IMdbLogger<LType> where LType : ILoggable
{
    private LogManager<LogModel> _logger;

    private Type _logType;

    private IBackgroundTaskQueue<LogModel> _backgroundTaskQueue;

    private IHttpContextAccessor _httpContextAccessor;

    public LogManagerStandard(IHttpContextAccessor httpContextAccessor, LogManager<LogModel> logger, IBackgroundTaskQueue<LogModel> backgroundTaskQueue)
    {
        _httpContextAccessor = httpContextAccessor;
        _backgroundTaskQueue = backgroundTaskQueue;
        _logger = logger;
        _logType = typeof(LType);
    }

    public void Log(long? referenceNo, string? user, string message, object content, Exception ex = null)
    {
        DateTime date = DateTime.Now.Date;
        DateTime now = DateTime.Now;
        string level = "INFO";
        if (ex != null)
        {
            level = "ERROR";
            _backgroundTaskQueue.QueueBackgroundWorkItem(logModel, async (u, i) => await _logger.LogInternal(u));
        }

        string text = null;
        if (_httpContextAccessor != null && _httpContextAccessor.HttpContext != null)
        {
            text = (string)_httpContextAccessor.HttpContext.Items["LogGuid"];
            if (string.IsNullOrWhiteSpace(text))
            {
                text = Guid.NewGuid().ToString();
                _httpContextAccessor.HttpContext.Items.Add("LogGuid", text);
            }
        }

        LogModel logModel = new LogModel();
        logModel.User = user;
        logModel.BusinessDate = date.ToString("yyyy/MM/dd");
        logModel.CreateDate = now;
        logModel.Exception = (ex == null) ? string.Empty : JsonConvert.SerializeObject(ex);
        logModel.Message = message;
        logModel.Data = (ex == null) ? string.Empty : JsonConvert.SerializeObject(content);
        logModel.Level = level;
        logModel.Logger = _logType.Name;
        logModel.ReferenceNo = referenceNo;
        logModel.TraceCode = text;
        logModel.ExStr = (ex == null) ? string.Empty : JsonConvert.SerializeObject(ex.InnerException);
        _backgroundTaskQueue.QueueBackgroundWorkItem(logModel, async (u, i) => await _logger.LogInternal(u));
    }

    public void Log(Guid traceCode, long? referenceNo, string? user, string message, object content, Exception ex = null)
    {
        DateTime date = DateTime.Now.Date;
        DateTime now = DateTime.Now;
        string level = "INFO";
        if (ex != null)
        {
            level = "ERROR";
        }

        string text = traceCode.ToString();
        if (_httpContextAccessor != null && _httpContextAccessor.HttpContext != null && string.IsNullOrWhiteSpace((string)_httpContextAccessor.HttpContext.Items["LogGuid"]))
        {
            _httpContextAccessor.HttpContext.Items.Add("LogGuid", text);
        }

        LogModel logModel = new LogModel();
        logModel.User = user;
        logModel.BusinessDate = date.ToString("yyyy/MM/dd");
        logModel.CreateDate = now;
        logModel.Exception = (ex == null) ? string.Empty : JsonConvert.SerializeObject(ex);
        logModel.Message = message;
        logModel.Data = JsonConvert.SerializeObject(content);
        logModel.Level = level;
        logModel.Logger = _logType.Name;
        logModel.ReferenceNo = referenceNo;
        logModel.TraceCode = text;
        logModel.ExStr = (ex == null) ? string.Empty : JsonConvert.SerializeObject(ex.InnerException);
        _backgroundTaskQueue.QueueBackgroundWorkItem(logModel, async (u, i) => await _logger.LogInternal(u));
    }
}