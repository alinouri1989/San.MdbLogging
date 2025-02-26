using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;

namespace San.MdbLogging
{
    public class MyMongoDBDateTimeSerializer : IBsonSerializer<DateTime>
    {
        public Type ValueType => typeof(DateTime);

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateTime value)
        {
            // Convert to UTC before serialization  
            var utcDateTime = DateTime.SpecifyKind(value, DateTimeKind.Utc);
            context.Writer.WriteDateTime(utcDateTime.Ticks);
        }

        public DateTime Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            // Read the date time from BSON and convert to local time  
            var ticks = context.Reader.ReadDateTime();
            return DateTime.SpecifyKind(new DateTime(ticks), DateTimeKind.Local);
        }

        // Explicit interface implementation to satisfy IBsonSerializer  
        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            return Deserialize(context, args);
        }

        void IBsonSerializer.Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            if (value is DateTime dateTime)
            {
                Serialize(context, args, dateTime);
            }
            else
            {
                throw new ArgumentException("Value is not a DateTime.");
            }
        }
    }
}