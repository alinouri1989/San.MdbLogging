using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace MongoLogger.Attributes
{
    public class ShouldSerializeContractResolver : DefaultContractResolver
    {
        
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            
            if (Attribute.IsDefined(member,typeof(NoDbLog)))
            {
                property.ShouldSerialize =
                    instance =>
                    {
                        return false;
                    };
            }

            return property;
        }
    }
}
