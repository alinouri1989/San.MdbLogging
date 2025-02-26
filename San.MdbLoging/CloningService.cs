using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization.Conventions;
using Newtonsoft.Json;
using San.MdbLogging.Attributes;

namespace San.MdbLogging;
public static class CloningService
{
    public static T Clone<T>(this T source)
    {
        if (source == null)
        {
            return default(T);
        }

        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            ObjectCreationHandling = ObjectCreationHandling.Auto
        };
        JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings();
        jsonSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        List<JsonConverter> list = new List<JsonConverter>();
        list.Add(new AttributesConverter());
        jsonSerializerSettings.Converters = list;
        JsonSerializerSettings settings2 = jsonSerializerSettings;
        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source, settings2), settings);
    }

    public static T CloneB<T>(this T source)
    {
        if (source == null)
        {
            return default(T);
        }

        using (var stream = new MemoryStream())
        {
            using (var writer = new StreamWriter(stream))
            {
                var json = JsonConvert.SerializeObject(source);
                writer.Write(json);
                writer.Flush();
                stream.Seek(0, SeekOrigin.Begin);
            }
            using (var reader = new StreamReader(stream))
            {
                var json = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<T>(json);
            }
        }
    }
}