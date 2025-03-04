using Microsoft.AspNetCore.Http;
using MongoLogger.BgTasks;
using MongoLogger.Extensions;
using MongoLogger.Models;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MongoLogger
{
    public class LogManagerStandard<LType> : IMdbLogger<LType> where LType : ILoggable
    {
        #region PrivateMembers
        private readonly ILogManager<LogModel> _logger;
        private readonly Type _logType;
        private readonly IBackgroundTaskQueue<LogModel> _backgroundTaskQueue;
        private readonly IServiceProvider _serviceProvider;
        #endregion

        #region ctor
        public LogManagerStandard(IServiceProvider serviceProvider, ILogManager<LogModel> logger, IBackgroundTaskQueue<LogModel> backgroundTaskQueue)
        {
            _backgroundTaskQueue = backgroundTaskQueue;
            _logger = logger;
            _logType = typeof(LType);
            _serviceProvider = serviceProvider;
        }
        #endregion

        #region WithPassRefrence
        public void Log(long? referenceNo, string message, object content, Exception ex = null)
        {
            var bDate = DateTime.Now.Date;
            var cDate = DateTime.Now;

            var level = "INFO";
            if (ex != null)
                level = "ERROR";

            var logModel = new LogModel
            {
                BusinessDate = bDate.ToString("yyyy/MM/dd"),
                CreateDate = cDate,
                Exception = ex,
                Message = message,
                Data = content,
                Level = level,
                Logger = _logType.Name,
                ReferenceNo = referenceNo,
                ExternalRefrenceCode = TraceIdExplorer.ExternalRefrenceNumber,
                TraceCode = TraceIdExplorer.TraceCode,
                SourceIP = _serviceProvider.GetService(typeof(IHttpContextAccessor)) is IHttpContextAccessor _httpContextAccessor ? MDbExtensions.GetIP(_httpContextAccessor.HttpContext) : "127.0.0.1",
                ExStr = ex == null ? string.Empty : JsonConvert.SerializeObject(ex)
            };
            _backgroundTaskQueue.QueueBackgroundWorkItem(logModel, async (u, i) => await _logger.LogInternal(u));

        }

        public void Log(long? referenceNo, string message, object content, int status, string ApiName, string UserName, Exception ex = null)
        {
            var bDate = DateTime.Now.Date;
            var cDate = DateTime.Now;

            var level = "INFO";
            if (ex != null)
                level = "ERROR";
            IHttpContextAccessor _httpContextAccessor = _serviceProvider.GetService(typeof(IHttpContextAccessor)) as IHttpContextAccessor;

            var logModel = new LogModelWithExteraParam
            {
                BusinessDate = bDate.ToString("yyyy/MM/dd"),
                CreateDate = cDate,
                Exception = ex,
                Message = message,
                Data = content,
                Level = level,
                Logger = _logType.Name,
                ReferenceNo = referenceNo,
                ExternalRefrenceCode = TraceIdExplorer.ExternalRefrenceNumber,
                TraceCode = TraceIdExplorer.TraceCode,
                ApiName = ApiName,
                Status = status,
                UserName = UserName,
                SourceIP = _httpContextAccessor != null ? MDbExtensions.GetIP(_httpContextAccessor.HttpContext) : "127.0.0.1",
                ExStr = ex == null ? string.Empty : JsonConvert.SerializeObject(ex),
            };


            _backgroundTaskQueue.QueueBackgroundWorkItem(logModel, async (u, i) => await _logger.LogInternal(u));

        }

        #endregion

        #region WithOutPassRefrence
        public void Log(string message, object content, Exception ex = null)
        {
            var bDate = DateTime.Now.Date;
            var cDate = DateTime.Now;

            var level = "INFO";
            if (ex != null)
                level = "ERROR";

            IHttpContextAccessor _httpContextAccessor = _serviceProvider.GetService(typeof(IHttpContextAccessor)) as IHttpContextAccessor;


            var logModel = new LogModel
            {
                BusinessDate = bDate.ToString("yyyy/MM/dd"),
                CreateDate = cDate,
                Exception = ex,
                Message = message,
                Data = content,
                Level = level,
                Logger = _logType.Name,
                ReferenceNo = TraceIdExplorer.RefrenceNumber,
                ExternalRefrenceCode = TraceIdExplorer.ExternalRefrenceNumber,
                TraceCode = TraceIdExplorer.TraceCode,
                SourceIP = _httpContextAccessor != null ? MDbExtensions.GetIP(_httpContextAccessor.HttpContext) : "127.0.0.1",
                ExStr = ex == null ? string.Empty : JsonConvert.SerializeObject(ex)
            };
            _backgroundTaskQueue.QueueBackgroundWorkItem(logModel, async (u, i) => await _logger.LogInternal(u));
        }

        public void Log(string message, object content, int status, string ApiName, string UserName, Exception ex = null)
        {
            var bDate = DateTime.Now.Date;
            var cDate = DateTime.Now;

            var level = "INFO";
            if (ex != null)
                level = "ERROR";

            IHttpContextAccessor _httpContextAccessor = _serviceProvider.GetService(typeof(IHttpContextAccessor)) as IHttpContextAccessor;

            var logModel = new LogModelWithExteraParam
            {
                BusinessDate = bDate.ToString("yyyy/MM/dd"),
                CreateDate = cDate,
                Exception = ex,
                Message = message,
                Data = content,
                Level = level,
                Logger = _logType.Name,
                ReferenceNo = TraceIdExplorer.RefrenceNumber,
                ExternalRefrenceCode = TraceIdExplorer.ExternalRefrenceNumber,
                TraceCode = TraceIdExplorer.TraceCode.ToString().ToLower(),
                ApiName = ApiName,
                Status = status,
                UserName = UserName,
                SourceIP = _httpContextAccessor != null ? MDbExtensions.GetIP(_httpContextAccessor.HttpContext) : "127.0.0.1",
                ExStr = ex == null ? string.Empty : JsonConvert.SerializeObject(ex),
            };

            _backgroundTaskQueue.QueueBackgroundWorkItem(logModel, async (u, i) => await _logger.LogInternal(u));
        }
        #endregion

    }
}
