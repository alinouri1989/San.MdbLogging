using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace San.MDbLogging.Attributes;
public class NoDbLogConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return true;
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        IEnumerable<PropertyInfo> enumerable = from pi in value.GetType().GetProperties()
                                               where Attribute.IsDefined(pi, typeof(NoDbLog))
                                               select pi;
        if (enumerable != null && enumerable.Any())
        {
            foreach (PropertyInfo item in enumerable)
            {
                JToken jToken = JToken.FromObject(value);
                if (jToken.Type != JTokenType.Object)
                {
                    jToken.WriteTo(writer);
                }
                else
                {
                    JObject jObject = (JObject)jToken;
                    IList<string> list = (from p in jObject.Properties()
                                          select p.Name).ToList();
                    jObject.Remove(item.Name);
                    jObject.AddFirst(new JProperty(item.Name, string.Empty));
                    jObject.WriteTo(writer);
                }
            }

            return;
        }

        JToken jToken2 = JToken.FromObject(value);
        if (jToken2.Type != JTokenType.Object)
        {
            jToken2.WriteTo(writer);
        }
        else
        {
            jToken2.WriteTo(writer);
        }
    }
}
