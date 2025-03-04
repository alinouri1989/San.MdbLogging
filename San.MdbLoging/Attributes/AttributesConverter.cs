using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoLogger.Attributes
{
    public class AttributesConverter : JsonConverter
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
            var propsWithMaskAttrs = value.GetType().GetProperties().Where(pi => Attribute.IsDefined(pi, typeof(CardNoMaskAttribute)));
            var propsWithNoLogAttrs = value.GetType().GetProperties().Where(pi => Attribute.IsDefined(pi, typeof(NoDbLog)));

            JToken t = JToken.FromObject(value);
            JObject o = null;
            if (t.Type != JTokenType.Object)
            {
                            }
            else
            {
                o = (JObject)t;
            }

            if (propsWithMaskAttrs != null && propsWithMaskAttrs.Any())
                foreach (var pi in propsWithMaskAttrs)
                {
                    

                    if (t.Type != JTokenType.Object)
                    {
                        t.WriteTo(writer);
                    }
                    else
                    {
                        IList<string> propertyNames = o.Properties().Select(p => p.Name).ToList();
                        o.Remove(pi.Name);
                        o.AddFirst(new JProperty(pi.Name, mask(pi.GetValue(value))));

                        
                    }
                }
            
            if (propsWithNoLogAttrs != null && propsWithNoLogAttrs.Any())
            {
                foreach (var pi in propsWithNoLogAttrs)
                {

                    if (t.Type != JTokenType.Object)
                    {
                        t.WriteTo(writer);
                    }
                    else
                    {
                        IList<string> propertyNames = o.Properties().Select(p => p.Name).ToList();
                        o.Remove(pi.Name);

                    }
                }
            }
            else
            {

                if (t.Type != JTokenType.Object)
                {
                    t.WriteTo(writer);
                }
            }

            if(o!= null)
            o.WriteTo(writer);

        }

        private object mask(object value)
        {
            if (value == null)
                return null;

            var valStr = value.ToString();
            if (valStr.Length != 16)
                return value;

            var firstPart = valStr.Substring(0, 6);
            var lastPart = valStr.Substring(12, 4);
            var convertedVal = firstPart + "******" + lastPart;
            return convertedVal;
        }
    }
}
