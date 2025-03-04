using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace MongoLogger.Models
{
    public class LogModel : BaseMongoModel
    {
        public LogModel()
        {
        }


        [BsonElement]
        public DateTime CreateDate { get; set; }
        public string Logger { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public Exception Exception { get; set; }
        public string ExStr { get; set; }
        public long MId { get { return CreateDate.Ticks; } set { } }
        public string SourceIP { get; set; }
    }

    public class LogModelWithExteraParam : LogModel
    {
        public int Status { get; set; }
        public string ApiName { get; set; }
        public string UserName { get; set; }
    }

    public class MessageModel
    {
        public string Type { get; set; }
        public List<object> Input { get; set; }
        public object Output { get; set; }
        public string MethodName { get; set; }
        public string ClassName { get; set; }
    }

}
