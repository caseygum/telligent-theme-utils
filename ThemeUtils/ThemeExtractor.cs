using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace nDriven.Telligent.ThemeUtils
{
    public class ThemeExtractor
    {
        
        public string ThemeFilePath { get; }
        public string OutputDirectory { get; }

        public TextWriter Logger { get; }

        public ThemeExtractor([NotNull] string themeFilePath, [NotNull] string outputDirectory, bool clean = false, TextWriter logger = null)
        {
            ThemeFilePath = themeFilePath.Trim();
            if (!File.Exists(ThemeFilePath))
            {
                throw new FileNotFoundException("File could not be found.", ThemeFilePath);
            }

            OutputDirectory = outputDirectory.Trim();
            if (Directory.Exists(OutputDirectory))
            {
                if (clean)
                {
                    Directory.Delete(OutputDirectory, true);
                }
                else
                {
                    throw new ArgumentException(
                        $"Directory already exists: {outputDirectory}.  Use the --clean flag to delete directory before extracting.",
                        outputDirectory);
                }
            }
            

            if (!Directory.Exists(OutputDirectory))
            {
                Directory.CreateDirectory(OutputDirectory);
            }
            
            Logger = logger ?? Console.Out;
        }

        public void Extract()
        {
            var xdoc = XDocument.Load(ThemeFilePath);
            if (xdoc.Root == null) throw new ArgumentException("XML root element not found.");
            if (xdoc.Root.Name == Constants.ThemesElementName)
            {
                foreach (var themeElement in xdoc.Root.Elements())
                {
                    ExtractTheme(themeElement);
                }
                
            } else if (xdoc.Root.Name == Constants.ThemeElementName)
            {
                ExtractTheme(xdoc.Root);
            }
            else throw new ArgumentException($"XML root element must be <{Constants.ThemesElementName}> or <{Constants.ThemeElementName}>.  Found <{xdoc.Root.Name}> instead.");
            
            Logger.WriteLine("Done!");
        }

        private void ExtractTheme(XElement themeElement)
        {
            // Get theme name.
            var themeName = themeElement.Attribute(Constants.NameAttributeName)?.Value ?? Constants.ThemesElementName;
            
            var themeTypeName = GetThemeType(themeElement).Name;
            Logger.WriteLine($"Found theme: {themeName} - {themeTypeName}");

            // Create theme folder.
            var themeDir = Path.Combine(OutputDirectory, themeName, themeTypeName);
            if (!Directory.Exists(themeDir))
            {
                Directory.CreateDirectory(themeDir);
            }
            
            Logger.WriteLine($"Extracting theme to {themeDir}...");
            
            // Write theme metadata
            ExtractMetadata(themeElement, themeDir, Constants.ThemeMetadataFilename);
            
            foreach (var elem in themeElement.Elements())
            {
                var nodeName = elem.Name.LocalName;
                switch (nodeName)
                {
                    case Constants.HeadScriptElementName:
                    case Constants.BodyScriptElementName:
                        ExtractContentScript(elem, themeDir);
                        break;
                    case Constants.ConfigurationElementName:
                    case Constants.PaletteTypesElementName:
                        ExtractCData(elem, themeDir, "xml");
                        break;
                    case Constants.LanguageResourcesElementName:
                        ExtractLanguageResources(elem, themeDir);
                        break;
                    case Constants.FilesElementName:
                    case Constants.JavascriptFilesElementName:
                    case Constants.StyleFilesElementName:
                        ExtractDirectory(elem, themeDir);
                        break;
                    case Constants.PreviewImageElementName:
                        ExtractFile(elem, themeDir, Constants.PreviewImagePrefix);
                        break;
                    case Constants.PageLayoutsElementName:
                        ExtractPageLayouts(elem, themeDir);
                        break;
                    case Constants.ScopedPropertiesElementName:
                        ExtractXml(elem, themeDir);
                        break;
                }
            }
        }

        private ThemeType GetThemeType(XElement themeElement)
        {
            var typeId = themeElement.Attribute(Constants.ThemeTypeIdAttributeName)?.Value ?? Guid.Empty.ToString().Replace("-","");
            return ThemeType.FromId(typeId);
        }

        private void ExtractMetadata(XElement element, string outputDir, string fileName)
        {
            var metaData = new Dictionary<string, string>();
            foreach (var attr in element.Attributes())
            {
                metaData[attr.Name.LocalName] = attr.Value;
            }

            var outputPath = Path.Combine(outputDir, fileName);
            Logger.WriteLine($"Extracting metadata to {outputPath}.");

            metaData.Serialize(outputPath);
        }

        private void ExtractContentScript(XElement scriptElement, string outputDir)
        {
            var language = scriptElement.Attribute(Constants.LanguageAttributeName)?.Value ?? Constants.UnknownLanguageName;
            var type = scriptElement.Name.LocalName;
            var fileName = scriptElement.Name.LocalName;
            switch (language)
            {
                case Constants.VelocityLanguageName:
                    fileName += $".{Constants.VelocityLanguageExtension}";
                    break;
                case Constants.JavaScriptLanguageName:
                    fileName += $".{Constants.JavaScriptLanguageExtension}";
                    break;
                default:
                    fileName += $".{Constants.UnknownLanguageExtension}";
                    break;
            }

            var outputPath = Path.Combine(outputDir, fileName);
            
            Logger.WriteLine($"Extracting {type} to {outputPath}.");
            
            File.WriteAllText(outputPath, scriptElement.Value);
        }

        private void ExtractLanguageResources(XElement resElement, string outputDir)
        {
            var langResPath = ExtractCData(resElement, outputDir, Constants.XmlExtension);
            var resDoc = XDocument.Load(langResPath);
            var resources = resDoc.Root.Elements().ToArray();
            Array.Sort(resources, 
                (e1, e2) => 
                    e1.Attribute(Constants.NameAttributeName).Value
                        .CompareTo(e2.Attribute(Constants.NameAttributeName).Value));
            var langKey = resDoc.Root.Attribute(Constants.LanguageKeyAttributeName);
            resDoc.Root.ReplaceAll(resources);
            if (langKey != null)
            {
                resDoc.Root.SetAttributeValue(langKey.Name, langKey.Value);
            }
            SerializationUtils.SaveXml(resDoc, langResPath);
        }

        private string ExtractCData(XElement element, string outputDir, string fileExtension)
        {
            var type = element.Name.LocalName;
            var fileName = $"{element.Name.LocalName}.{fileExtension}";
            
            var outputPath = Path.Combine(outputDir, fileName);
            Logger.WriteLine($"Extracting {type} to {outputPath}.");

            File.WriteAllText(outputPath, element.Value);

            return outputPath;
        }
        
        private string ExtractXml(XElement element, string outputDir)
        {
            var type = element.Name.LocalName;
            var fileName = $"{element.Name.LocalName}.{Constants.XmlExtension}";
            
            var outputPath = Path.Combine(outputDir, fileName);
            Logger.WriteLine($"Extracting {type} to {outputPath}.");
            
            SerializationUtils.SaveXml(element, outputPath);

            return outputPath;
        }
        
        private string ExtractPageLayouts(XElement layoutsElement, string outputDir)
        {
            var layoutsDir = Path.Combine(outputDir, Constants.PageLayoutsElementName);
            if (!Directory.Exists(layoutsDir))
            {
                Directory.CreateDirectory(layoutsDir);
            }
            
            Logger.WriteLine($"Extracting page layouts to {layoutsDir}.");
            
            foreach (var elem in layoutsElement.Elements())
            {
                var nodeName = elem.Name.LocalName;
                switch (nodeName)
                {
                    case Constants.HeadersElementName:
                    case Constants.FootersElementName:
                    case Constants.PagesElementName:
                    case Constants.ScopedPropertiesElementName:
                        ExtractXml(elem, layoutsDir);
                        break;
                    case Constants.ContentFragmentsElementName:
                        ExtractContentFragments(elem, layoutsDir);
                        break;
                }
            }

            return layoutsDir;
        }

        private string ExtractContentFragments(XElement fragmentsElement, string outputDir)
        {
            var fragmentsDir = Path.Combine(outputDir, Constants.ContentFragmentsElementName);
            if (!Directory.Exists(fragmentsDir))
            {
                Directory.CreateDirectory(fragmentsDir);
            }
            
            Logger.WriteLine($"Extracting content fragments to {fragmentsDir}...");

            var scriptedFragmentsElement = fragmentsElement.Elements(Constants.ScriptedContentFragmentsElementName).FirstOrDefault();
            
            if (scriptedFragmentsElement != null)
            {
                var dirMetadata = new DirectoryMetadata();
                var i = 0;
                foreach (var elem in scriptedFragmentsElement.Elements())
                {
                    ExtractScriptedContentFragment(elem, fragmentsDir, out var fragmentId);
                    dirMetadata.DirectoryOrder[fragmentId] = ++i;
                }

                var metadataFileName = Path.Combine(fragmentsDir, Constants.FileMetadataFilename);
                dirMetadata.Serialize(metadataFileName);
            }

            return fragmentsDir;
        }

        private string ExtractScriptedContentFragment(XElement contentElement, string outputDir, out string fragmentId)
        {
            fragmentId = contentElement.Attribute(Constants.InstanceIdentifierAttributeName).Value;
            var fragmentDir = Path.Combine(outputDir, fragmentId);

            if (!Directory.Exists(fragmentDir))
            {
                Directory.CreateDirectory(fragmentDir);
            }
            
            Logger.WriteLine($"Extracting scripted content fragment [{fragmentId}] to {fragmentDir}...");
            
            ExtractMetadata(contentElement, fragmentDir, Constants.ContentFragmentMetadataFilename);

            foreach (var elem in contentElement.Elements())
            {
                var nodeName = elem.Name.LocalName;
                switch (nodeName)
                {
                    case Constants.RequiredContextElementName:
                        ExtractXml(elem, fragmentDir);
                        break;
                    case Constants.AdditionalCssScriptElementName:
                    case Constants.HeaderScriptElementName:
                    case Constants.ContentScriptElementName:
                        ExtractContentScript(elem, fragmentDir);
                        break;
                    case Constants.ConfigurationElementName:
                        ExtractCData(elem, fragmentDir, Constants.XmlExtension);
                        break;
                    case Constants.LanguageResourcesElementName:
                        ExtractLanguageResources(elem, fragmentDir);
                        break;
                    case Constants.FilesElementName:
                        ExtractDirectory(elem, fragmentDir);
                        break;
                }
            }

            return fragmentDir;
        }

        private string ExtractDirectory(XElement dirElement, string outputDir)
        {
            var dirName = dirElement.Name.LocalName;
            var dirPath = Path.Combine(outputDir, dirName);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            
            Logger.WriteLine($"Extracting folder {dirName} to {dirPath}...");

            var metadata = FileMetadata.FromDirectory(dirElement);

            foreach (var fileElement in dirElement.Elements())
            {
                ExtractFile(fileElement, dirPath);
            }

            metadata.Serialize(Path.Combine(dirPath, Constants.FileMetadataFilename));

            return dirPath;
        }


        private string ExtractFile(XElement fileElement, string outputDir, string prefix = "")
        {
            var fileName = $"{prefix}{fileElement.Attribute(Constants.NameAttributeName)?.Value}";
            var filePath = Path.Combine(outputDir, fileName);

            var base64FileData = fileElement.Value;
            var fileData = Convert.FromBase64String(base64FileData);
            
            Logger.WriteLine($"Extracting file {fileName} to {filePath}.");
            
            File.WriteAllBytes(filePath, fileData);

            return filePath;
        }

    }
}