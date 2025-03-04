using MongoLogger.Models;

namespace San.MdbLogging.TestWorker
{
#nullable disable
    public class LogUpdatePrice : BaseSqlModel
    {
        public string Message { get; set; }
        public string Level { get; set; }
        public DateTime? TimeStamp { get; set; }
        public string Exception { get; set; }
        public Guid? RequestId { get; set; }
        public string ActionName { get; set; }
        public string SourceName { get; set; }
        public string Metadata { get; set; }
        public string InsuranceType { get; set; }
        public string NationalCode { get; set; }
        public new string Logger { get; set; }
    }
}
