using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace nDriven.Telligent.ThemeUtils
{
    public static class SerializationUtils
    {
        public static JsonSerializer Serializer { get; } = SerializationUtils.CreateSerializer();
        
        public static JsonSerializer CreateSerializer()
        {
            var settings = new JsonSerializerSettings();
            settings.Formatting = Formatting.Indented;
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

            return JsonSerializer.Create(settings);
        }

        public static string Serialize(this object obj)
        {
            using var textWriter = new StringWriter();
            return Serialize(textWriter, obj);
        }
        
        public static string Serialize(this object obj, string outputPath)
        {
            using var textWriter = new StreamWriter(new FileStream(outputPath, FileMode.Create, FileAccess.Write));
            return Serialize(textWriter, obj);
        }

        public static string Serialize(TextWriter textWriter, object obj)
        {
            Serializer.Serialize(textWriter, obj);
            textWriter.Flush();
            return textWriter.ToString();
        }
    }
}