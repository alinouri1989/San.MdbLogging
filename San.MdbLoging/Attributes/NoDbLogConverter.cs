using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace MongoLogger.Attributes
{
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
            var propsWithMaskAttrs = value.GetType().GetProperties().Where(pi => Attribute.IsDefined(pi, typeof(NoDbLog)));
            if (propsWithMaskAttrs != null && propsWithMaskAttrs.Any())
                foreach (var pi in propsWithMaskAttrs)
                {
                    JToken t = JToken.FromObject(value);

                    if (t.Type != JTokenType.Object)
                    {
                        t.WriteTo(writer);
                    }
                    else
                    {
                        JObject o = (JObject)t;
                        IList<string> propertyNames = o.Properties().Select(p => p.Name).ToList();
                        o.Remove(pi.Name);
                        o.AddFirst(new JProperty(pi.Name, string.Empty));

                        o.WriteTo(writer);
                    }
                }
            else
            {
                JToken t = JToken.FromObject(value);

                if (t.Type != JTokenType.Object)
                {
                    t.WriteTo(writer);
                }
                else
                {
                    JObject o = (JObject)t;

                    o.WriteTo(writer);
                }

            }
        }
    }
}
