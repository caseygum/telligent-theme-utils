using System.Collections.Generic;
using System.Xml.Linq;

namespace nDriven.Telligent.ThemeUtils
{
    public class FileMetadata
    {
        public Dictionary<string, int> FileOrder { get; set; } = new Dictionary<string, int>();

        public Dictionary<string, Dictionary<string, string>> FileAttributes { get; set; } =
            new Dictionary<string, Dictionary<string, string>>();

        public static FileMetadata FromDirectory(XElement dirElement)
        {
            var fileMetadata = new FileMetadata();

            var i = 0;
            foreach (var fileElement in dirElement.Elements())
            {
                var attributeMap = new Dictionary<string, string>();
                
                foreach (var attribute in fileElement.Attributes())
                {
                    if (attribute.Name == Constants.NameAttributeName)
                    {
                        fileMetadata.FileOrder[attribute.Value] = ++i;
                        fileMetadata.FileAttributes[attribute.Value] = attributeMap;
                    }

                    attributeMap[attribute.Name.LocalName] = attribute.Value;
                }
            }

            return fileMetadata;
        }
    }
}