using San.MDbLogging.Models;

namespace San.MdbLogging.TestWorker
{
    public class LogUpdatePrice : BaseSqlModel
    {
        public string Message { get; set; }
        public string Level { get; set; }
        public DateTime? TimeStamp { get; set; } // Nullable DateTime  
        public string Exception { get; set; }
        public Guid? RequestId { get; set; } // Nullable Guid  
        public string ActionName { get; set; }
        public string SourceName { get; set; }
        public string Metadata { get; set; }
        public string InsuranceType { get; set; }
        public string NationalCode { get; set; }
        public string Logger { get; set; }
    }
}
