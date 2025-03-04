using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace MongoLogger.Attributes
{
    public class NoDbLog: BsonIgnoreAttribute
    {
    }
}
