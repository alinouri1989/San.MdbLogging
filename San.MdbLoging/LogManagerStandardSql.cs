using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Newtonsoft.Json;
using San.MdbLogging.BgTasks;
using San.MdbLogging.Models;
using System;

namespace San.MdbLogging;

public class LogManagerStandardSql<TEntity, LType> : ISQLLogger<TEntity, LType>
                                                    where LType : ILoggable
                                                    where TEntity : BaseSqlModel
{
    private LogManagerSql<TEntity> _logger;

    private Type _logType;

    private IBackgroundTaskQueue<TEntity> _backgroundTaskQueue;

    private IHttpContextAccessor _httpContextAccessor;

    public LogManagerStandardSql(IHttpContextAccessor httpContextAccessor, LogManagerSql<TEntity> logger, IBackgroundTaskQueue<TEntity> backgroundTaskQueue)
    {
        _httpContextAccessor = httpContextAccessor;
        _backgroundTaskQueue = backgroundTaskQueue;
        _logger = logger;
        _logType = typeof(LType);
    }

    public void Log(TEntity entityLog, Exception exception)
    {
        DateTime now = DateTime.Now;
        string level = exception != null ? "ERROR" : "INFO";
        string traceCode = Guid.NewGuid().ToString();

        // Set the LogGuid in HttpContext if it doesn't exist  
        if (_httpContextAccessor != null && _httpContextAccessor.HttpContext != null &&
            string.IsNullOrWhiteSpace((string)_httpContextAccessor.HttpContext.Items["LogGuid"]))
        {
            _httpContextAccessor.HttpContext.Items.Add("LogGuid", traceCode);
        }

        //TEntity logModel = (TEntity)Activator.CreateInstance(typeof(TEntity));

        var logModelProperties = typeof(TEntity).GetProperties();

        var staticProperties = new Dictionary<string, object>
            {
                { "Level", level },
                { "TimeStamp", now },
                { "Exception", exception != null ? JsonConvert.SerializeObject(exception) : string.Empty },
                { "TraceCode", traceCode },
                { "Logger", _logType.Name }
            };

        // Also set the static properties  
        foreach (var entry in staticProperties)
        {
            var targetProperty = logModelProperties.FirstOrDefault(p => p.Name == entry.Key);
            if (targetProperty != null)
            {
                targetProperty.SetValue(entityLog, entry.Value);
            }
        }

        // Queue the logging task  
        _backgroundTaskQueue.QueueBackgroundWorkItem(entityLog,async (model,ct) => await _logger.LogInternal(model));
    }

    public void Log(TEntity entityLog)
    {
        DateTime now = DateTime.Now;
        string traceCode = Guid.NewGuid().ToString();

        // Set the LogGuid in HttpContext if it doesn't exist  
        if (_httpContextAccessor != null && _httpContextAccessor.HttpContext != null &&
            string.IsNullOrWhiteSpace((string)_httpContextAccessor.HttpContext.Items["LogGuid"]))
        {
            _httpContextAccessor.HttpContext.Items.Add("LogGuid", traceCode);
        }

        //TEntity logModel = (TEntity)Activator.CreateInstance(typeof(TEntity));

        var logModelProperties = typeof(TEntity).GetProperties();
        //var entityLogProperties = entityLog.GetType().GetProperties();

        //var propertyMap = new Dictionary<string, string>
        //    {
        //        { "Message", "Message" },
        //        { "ActionName", "ActionName" },
        //        { "SourceName", "SourceName" },
        //        { "Metadata", "Metadata" },
        //        { "InsuranceType", "InsuranceType" },
        //        { "NationalCode", "NationalCode" },
        //        { "RequestId", "RequestId" }
        //    };

        var staticProperties = new Dictionary<string, object>
            {
                { "TraceCode", traceCode },
                { "Logger", _logType.Name }
            };

        //foreach (var entry in propertyMap)
        //{
        //    var sourceProperty = entityLogProperties.FirstOrDefault(p => p.Name == entry.Key);
        //    if (sourceProperty != null)
        //    {
        //        var value = sourceProperty.GetValue(entityLog);
        //        var targetProperty = logModelProperties.FirstOrDefault(p => p.Name == entry.Value);
        //        if (targetProperty != null)
        //        {
        //            targetProperty.SetValue(logModel, value);
        //        }
        //    }
        //}

        //// Also set the static properties  
        foreach (var entry in staticProperties)
        {
            var targetProperty = logModelProperties.FirstOrDefault(p => p.Name == entry.Key);
            if (targetProperty != null)
            {
                targetProperty.SetValue(entityLog, entry.Value);
            }
        }

        // Queue the logging task  
        _backgroundTaskQueue.QueueBackgroundWorkItem(entityLog, async (model, ct) => await _logger.LogInternal(model));

    }
}