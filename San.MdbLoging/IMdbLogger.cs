using System;

namespace MongoLogger
{
    public interface IMdbLogger<LType> where LType : ILoggable
    {
        void Log(string message, object content, Exception ex = null);
        void Log(long? referenceNo, string message, object content, Exception ex = null);
        void Log(long? referenceNo, string message, object content, int status, string ApiName, string UserName, Exception ex = null);
        void Log(string message, object content, int status, string ApiName, string UserName, Exception ex = null);
    }
}