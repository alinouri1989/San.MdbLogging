using Microsoft.Extensions.Hosting;
using MongoLogger.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MongoLogger.BgTasks
{
    public class BackgroundWorker<T> : IHostedService where T : BaseMongoModel
    {
        readonly IWorker<T> _worker;
        readonly T _item;
        public BackgroundWorker(IWorker<T> worker)
        {
            _worker = worker;
        }

        public T Item { get; set; }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (Item != null)
                await _worker.DoWork(Item, cancellationToken);

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
