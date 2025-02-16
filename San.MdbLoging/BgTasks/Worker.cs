using San.MDbLogging.Models;
using System.Diagnostics;


namespace San.MDbLogging.BgTasks;

public class Worker<T> : IWorker<T> where T : BaseMongoModel
{
    private LogManager<T> _logManager;

    public Worker(LogManager<T> logManager)
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
            catch (Exception ex2)
            {
                Exception ex = ex2;
                Debug.WriteLine(ex.ToString());
            }
        }
    }
}
public interface IWorker<T> where T : class
{
    Task DoWork(T item, CancellationToken cancellationToken);
}
