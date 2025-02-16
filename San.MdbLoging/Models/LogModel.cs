using MongoDB.Bson.Serialization.Attributes;

namespace San.MDbLogging.Models;

public class LogModel : BaseMongoModel
{
    public string? User { get; set; }

    public long? ReferenceNo { get; set; }

    [BsonElement]
    public DateTime CreateDate { get; set; }

    public string BusinessDate { get; set; }

    public string Logger { get; set; }

    public string Level { get; set; }

    public string Message { get; set; }

    public object Data { get; set; }

    public Exception Exception { get; set; }

    public string ExStr { get; set; }

    public long MId
    {
        get => this.CreateDate.Ticks;
        set
        {
        }
    }
}
