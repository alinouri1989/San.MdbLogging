using Microsoft.Extensions.Hosting;
using San.MdbLogging.Models;

namespace San.MdbLogging.BgTasks;

public class BackgroundWorker<T> : IHostedService where T : class
{
    private readonly IWorker<T> _worker;

    private readonly T _item;

    public T Item { get; set; }

    public BackgroundWorker(IWorker<T> worker)
    {
        _worker = worker;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (Item != null)
        {
            await _worker.DoWork(Item, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

