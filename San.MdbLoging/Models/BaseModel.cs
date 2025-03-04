using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;


namespace San.MdbLogging.Models;



public interface IBaseModel<TKey>
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public TKey Id { get; set; }

    public string TraceCode { get; set; }
}

public interface IBaseModel
{
    }

public class BaseMongoModel : IBaseModel, IBaseModel<string>
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string TraceCode { get; set; }
}

public class BaseSqlModel : IBaseModel, IBaseModel<long>
{
    public long Id { get; set; } = ObjectId.GenerateNewId().CreationTime.Ticks;
    public string TraceCode { get; set; }
    public string Logger { get; set; }
}