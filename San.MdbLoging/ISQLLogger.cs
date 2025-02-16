using San.MDbLogging.Models;

namespace San.MDbLogging;

public interface ISQLLogger<TEntity, LType> where LType : ILoggable
    where TEntity : BaseSqlModel
{
    void Log(TEntity entityLog);
    void Log(TEntity entityLog, Exception exception);
}