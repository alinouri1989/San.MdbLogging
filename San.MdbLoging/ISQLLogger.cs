using San.MdbLogging.Models;

namespace San.MdbLogging;

public interface ISQLLogger<TEntity, LType> where LType : ILoggable
    where TEntity : BaseSqlModel
{
    void Log(TEntity entityLog);
    void Log(TEntity entityLog, Exception exception);
}