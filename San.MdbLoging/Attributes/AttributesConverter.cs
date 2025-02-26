using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace San.MdbLogging.Attributes;

public class AttributesConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return true;
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        IEnumerable<PropertyInfo> enumerable = from pi in value.GetType().GetProperties()
                                               where Attribute.IsDefined(pi, typeof(CardNoMaskAttribute))
                                               select pi;
        IEnumerable<PropertyInfo> enumerable2 = from pi in value.GetType().GetProperties()
                                                where Attribute.IsDefined(pi, typeof(NoDbLog))
                                                select pi;
        JToken jToken = JToken.FromObject(value);
        JObject jObject = null;
        if (jToken.Type == JTokenType.Object)
        {
            jObject = (JObject)jToken;
        }

        if (enumerable != null && enumerable.Any())
        {
            foreach (PropertyInfo item in enumerable)
            {
                if (jToken.Type != JTokenType.Object)
                {
                    jToken.WriteTo(writer);
                    continue;
                }

                IList<string> list = (from p in jObject.Properties()
                                      select p.Name).ToList();
                jObject.Remove(item.Name);
                jObject.AddFirst(new JProperty(item.Name, mask(item.GetValue(value))));
            }
        }

        if (enumerable2 != null && enumerable2.Any())
        {
            foreach (PropertyInfo item2 in enumerable2)
            {
                if (jToken.Type != JTokenType.Object)
                {
                    jToken.WriteTo(writer);
                    continue;
                }

                IList<string> list2 = (from p in jObject.Properties()
                                       select p.Name).ToList();
                jObject.Remove(item2.Name);
            }
        }
        else if (jToken.Type != JTokenType.Object)
        {
            jToken.WriteTo(writer);
        }

        jObject?.WriteTo(writer);
    }

    private object mask(object value)
    {
        if (value == null)
        {
            return null;
        }

        string text = value.ToString();
        return (text.Length != 16) ? value : (text.Substring(0, 6) + "******" + text.Substring(12, 4));
    }
}