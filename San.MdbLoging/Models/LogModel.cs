using MongoDB.Bson.Serialization.Attributes;

namespace San.MdbLogging.Models;

public class LogModel : BaseMongoModel
{
    public string? User { get; set; } = string.Empty; // Initialize to avoid null  
    public long? ReferenceNo { get; set; }
    public DateTime CreateDate { get; set; }
    public string BusinessDate { get; set; } = string.Empty; // Initialize to avoid null  
    public string Logger { get; set; } = string.Empty; // Initialize to avoid null  
    public string Level { get; set; } = string.Empty; // Initialize to avoid null  
    public string Message { get; set; } = string.Empty; // Initialize to avoid null  
    public object Data { get; set; }
    public object Exception { get; set; } = null!; // Initialize to avoid null  
    public string ExStr { get; set; } = string.Empty; // Initialize to avoid null  

    public long MId
    {
        get => this.CreateDate.Ticks;
        set { }
    }
}
