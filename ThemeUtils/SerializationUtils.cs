using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Formatting = Newtonsoft.Json.Formatting;

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

        public static T Deserialize<T>(this FileInfo fileInfo)
        {
            using var reader = new JsonTextReader(new StreamReader(new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read)));
            return Serializer.Deserialize<T>(reader);
        }


        public static void SaveXml(XElement xElem, string outputPath)
        {
            SaveXml(outputPath, xElem.Save);
        }
        public static void SaveXml(XDocument xDoc, string outputPath)
        {
            SaveXml(outputPath, xDoc.Save);
        }

        private static void SaveXml(string outputPath, Action<XmlWriter> saveAction)
        {
            var xws = new XmlWriterSettings();
            xws.Indent = true;
            xws.IndentChars = "\t";
            xws.OmitXmlDeclaration = true;
            xws.Encoding = Encoding.UTF8;

            var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            using (var xw = XmlWriter.Create(fs, xws))
            {
                saveAction(xw);
            }
            fs.Close();
        }
        
        
        
    }
}