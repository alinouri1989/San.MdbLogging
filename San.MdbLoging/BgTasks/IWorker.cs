using MongoLogger.Models;
using System.Threading;
using System.Threading.Tasks;

namespace MongoLogger.BgTasks
{
    public interface IWorker<T> where T : BaseMongoModel
    {
        Task DoWork(T item, CancellationToken cancellationToken);
    }
}