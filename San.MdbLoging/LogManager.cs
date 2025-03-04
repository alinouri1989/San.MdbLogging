using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoLogger.BgTasks;
using MongoLogger.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MongoLogger
{
    public class LogManager<T> : ILogManager<T> where T : BaseMongoModel
    {
        IQueueManager<T> _queueManager;
        string _dbColName;
        int _batchSize;
        IBackgroundTaskQueue<T> _backgroundTaskQueue;
        static IHttpContextAccessor _httpContextAccessor;

        public LogManager(IServiceProvider serviceProvider, IOptions<LogDatabaseSettings> options, IQueueManager<T> queueManager, string dbColName = null, int batchSize = -1)
        {
            _dbColName = dbColName;
            _batchSize = batchSize;
            _backgroundTaskQueue = (IBackgroundTaskQueue<T>)serviceProvider.GetService(typeof(IBackgroundTaskQueue<T>));

            _queueManager = queueManager;
            if (!string.IsNullOrEmpty(dbColName) || batchSize != -1)
            {
                var ls = (ILogService<T>)serviceProvider.GetService(typeof(ILogService<T>));
                _queueManager = (QueueManager<T>)ActivatorUtilities.CreateInstance(serviceProvider, typeof(QueueManager<T>), batchSize, dbColName);
            }
        }


        internal static void SetHttpAccessor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void Log(T log)
        {
            var logGuid = (string)_httpContextAccessor.HttpContext.Items["LogGuid"];
            if (string.IsNullOrWhiteSpace(logGuid))
            {
                logGuid = Guid.NewGuid().ToString();
                _httpContextAccessor.HttpContext.Items.Add("LogGuid", logGuid);
            }
            log.TraceCode = logGuid;
            _backgroundTaskQueue.QueueBackgroundWorkItem(log, async (it, ct) =>
            {
                await LogInternal(it);
            });
        }
        public async Task LogInternal(T log)
        {
            try
            {
                await _queueManager.AddToQue(log);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }

    public interface ILogManager<T>
    {
        void Log(T log);
        Task LogInternal(T log);
    }
}
