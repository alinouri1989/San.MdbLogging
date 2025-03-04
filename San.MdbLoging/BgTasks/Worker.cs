using MongoLogger.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MongoLogger.BgTasks
{
    public class Worker<T> : IWorker<T> where T : BaseMongoModel
    {
        ILogManager<T> _logManager;
        public Worker(ILogManager<T> logManager)
        {
            _logManager = logManager;
        }

        public async Task DoWork(T item, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await _logManager.LogInternal(item);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
                finally
                {
                }
            }
        }
    }
}
