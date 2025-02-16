using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace San.MDbLogging.Attributes;
public class ShouldSerializeContractResolver : DefaultContractResolver
{
    protected new virtual JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        JsonProperty jsonProperty = base.CreateProperty(member, memberSerialization);
        if (Attribute.IsDefined(member, typeof(NoDbLog)))
        {
            jsonProperty.ShouldSerialize = (object instance) => false;
        }

        return jsonProperty;
    }
}
