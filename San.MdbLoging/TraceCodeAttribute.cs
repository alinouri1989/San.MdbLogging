using System;
using System.Collections.Generic;
using System.Text;

namespace MongoLogger.Attributes
{
    public class TraceCodeAttribute : Attribute
    {
        internal Guid TraceCode { get; set; }
    }
}
