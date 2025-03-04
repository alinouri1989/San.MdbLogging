using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MongoLogger
{
    public class ComplexTypeSerializer : SerializerBase<object>
    {
        public override object Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var serializer = BsonSerializer.LookupSerializer(typeof(BsonDocument));
            var document = serializer.Deserialize(context, args);

            var bsonDocument = document.ToBsonDocument();

            var result = BsonExtensionMethods.ToJson(bsonDocument);
            return JsonConvert.DeserializeObject<IDictionary<string, object>>(result);
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            try
            {
                var serializer = BsonSerializer.LookupSerializer(typeof(BsonDocument));
                if (value != null)
                {
                    var jsonDocument = JsonConvert.SerializeObject(value);
                    var bsonDocument = BsonSerializer.Deserialize<BsonDocument>(jsonDocument);

                    serializer.Serialize(context, bsonDocument.AsBsonValue);

                }
                else

                    serializer.Serialize(context, new BsonDocument());
            }
            catch (Exception ex)
            {
                throw;
            }

        }
    }
}