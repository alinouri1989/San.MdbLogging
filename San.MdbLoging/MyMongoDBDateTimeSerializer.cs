using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace San.MDbLogging;

public class MyMongoDBDateTimeSerializer : DateTimeSerializer
{
    public new virtual DateTime Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return new DateTime(base.Deserialize(context, args).Ticks, DateTimeKind.Unspecified);
    }

    public new virtual void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateTime value)
    {
        DateTime value2 = new DateTime(value.Ticks, DateTimeKind.Local);
        base.Serialize(context, args, value2);
    }
}