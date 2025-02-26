using San.MdbLogging.Models;

namespace San.MdbLogging;

public interface IMdbLogger<LType> where LType : ILoggable
{
    void Log(long? referenceNo, string? user, string message, object content, Exception ex = null);
    void Log(Guid traceCode, long? referenceNo, string? user, string message, object content, Exception ex = null);
}
