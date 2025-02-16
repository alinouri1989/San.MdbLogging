using San.MDbLogging.Models;

namespace San.MDbLogging;

public interface IMdbLogger<LType> where LType : ILoggable
{
    void Log(long? referenceNo, string? user, string message, object content, Exception ex = null);
    void Log(Guid traceCode, long? referenceNo, string? user, string message, object content, Exception ex = null);
}
