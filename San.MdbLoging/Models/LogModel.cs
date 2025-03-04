using MongoDB.Bson.Serialization.Attributes;

namespace San.MdbLogging.Models;

public class LogModel : BaseMongoModel
{
    public string? User { get; set; } = string.Empty;     public long? ReferenceNo { get; set; }
    public DateTime CreateDate { get; set; }
    public string BusinessDate { get; set; } = string.Empty;     public string Logger { get; set; } = string.Empty;     public string Level { get; set; } = string.Empty;     public string Message { get; set; } = string.Empty;     public object Data { get; set; }
    public object Exception { get; set; } = null!;     public string ExStr { get; set; } = string.Empty; 
    public long MId
    {
        get => this.CreateDate.Ticks;
        set { }
    }
}
