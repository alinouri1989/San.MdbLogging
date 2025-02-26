using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace San.MdbLogging.Models;

//public class BaseSqlModel
//{
//    public long Id { get; set; }

//    public string TraceCode { get; set; }
//}

public interface IBaseModel<TKey>
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public TKey Id { get; set; }

    public string TraceCode { get; set; }
}

public interface IBaseModel
{
    // Define common properties/methods that you expect in both models  
}

public class BaseMongoModel : IBaseModel, IBaseModel<string>
{
    public string Id { get; set; }
    public string TraceCode { get; set; }
}

public class BaseSqlModel : IBaseModel, IBaseModel<long>
{
    public long Id { get; set; }
    public string TraceCode { get; set; }
    public string Logger { get; set; }
}