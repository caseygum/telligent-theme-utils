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
        private const string PreviewImagePrefix = "previewImage__";
        
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
            if (Directory.Exists(OutputDirectory) && clean)
            {
                Directory.Delete(OutputDirectory, true);
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
            if (xdoc.Root == null) throw new ArgumentException("Xml root element not found.");
            if (xdoc.Root.Name == "themes")
            {
                foreach (var themeElement in xdoc.Root.Elements())
                {
                    ExtractTheme(themeElement);
                }
                
            } else if (xdoc.Root.Name == "theme")
            {
                ExtractTheme(xdoc.Root);
            }
            else throw new ArgumentException($"XML root element must be <themes> or <theme>.  Found <{xdoc.Root.Name}> instead.");
            
            Logger.WriteLine("Done!");
        }

        private void ExtractTheme(XElement themeElement)
        {
            // Get theme name.
            var themeName = themeElement.Attribute("name")?.Value ?? "theme";
            
            var themeType = GetThemeType(themeElement);
            Logger.WriteLine($"Found theme: {themeName} - {themeType}");

            // Create theme folder.
            var themeDir = Path.Combine(OutputDirectory, themeName, themeType);
            if (!Directory.Exists(themeDir))
            {
                Directory.CreateDirectory(themeDir);
            }
            
            Logger.WriteLine($"Extracting theme to {themeDir}...");
            
            
            // Write theme metadata
            ExtractMetadata(themeElement, themeDir);
            
            foreach (var elem in themeElement.Elements())
            {
                var nodeName = elem.Name.LocalName;
                switch (nodeName)
                {
                    case "headScript":
                    case "bodyScript":
                        ExtractContentScript(elem, themeDir);
                        break;
                    case "configuration":
                    case "paletteTypes":
                        ExtractCData(elem, themeDir, "xml");
                        break;
                    case "languageResources":
                        ExtractLanguageResources(elem, themeDir);
                        break;
                    case "files":
                    case "javascriptFiles":
                    case "styleFiles":
                        ExtractDirectory(elem, themeDir);
                        break;
                    case "previewImage":
                        ExtractFile(elem, themeDir, PreviewImagePrefix);
                        break;
                    case "pageLayouts":
                        ExtractPageLayouts(elem, themeDir);
                        break;
                }
            }
        }

        private string GetThemeType(XElement themeElement)
        {
            var typeId = themeElement.Attribute("themeTypeId")?.Value ?? Guid.Empty.ToString();

            switch (typeId)
            {
                case "0c647246673542f9875dc8b991fe739b":
                    return "Site";
                case "a3b17ab0af5f11dda3501fcf55d89593":
                    return "Blog";
                case "c6108064af6511ddb074de1a56d89593":
                    return "Group";
                default:
                    return $"Unknown Theme Type ({typeId})";
            }
        }

        private void ExtractMetadata(XElement element, string outputDir, string fileName = "metadata.json")
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
            var language = scriptElement.Attribute("language")?.Value.ToLowerInvariant() ?? "txt";
            var type = scriptElement.Name.LocalName;
            var fileName = scriptElement.Name.LocalName;
            switch (language)
            {
                case "velocity":
                    fileName += ".vm";
                    break;
                case "javascript":
                    fileName += ".js";
                    break;
                case "unknown":
                    fileName += ".txt";
                    break;
                default:
                    fileName += $".{language}";
                    break;
            }

            var outputPath = Path.Combine(outputDir, fileName);
            
            Logger.WriteLine($"Extracting {type} to {outputPath}.");
            
            File.WriteAllText(outputPath, scriptElement.Value);
        }

        private void ExtractLanguageResources(XElement resElement, string outputDir)
        {
            var langResPath = ExtractCData(resElement, outputDir, "xml");
            var resDoc = XDocument.Load(langResPath);
            var resources = resDoc.Root.Elements().ToArray();
            Array.Sort(resources, (e1, e2) => e1.Attribute("name").Value.CompareTo(e2.Attribute("name").Value));
            resDoc.Root.ReplaceAll(resources);
            resDoc.Save(langResPath);
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
            var fileName = $"{element.Name.LocalName}.xml";
            
            var outputPath = Path.Combine(outputDir, fileName);
            Logger.WriteLine($"Extracting {type} to {outputPath}.");
            
            element.Save(outputPath);

            return outputPath;
        }
        
        private string ExtractPageLayouts(XElement layoutsElement, string outputDir)
        {
            var layoutsDir = Path.Combine(outputDir, "pageLayouts");
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
                    case "headers":
                    case "footers":
                    case "pages":
                    case "scopedProperties":
                        ExtractXml(elem, layoutsDir);
                        break;
                    case "contentFragments":
                        ExtractContentFragments(elem, layoutsDir);
                        break;
                }
            }

            return layoutsDir;
        }

        private string ExtractContentFragments(XElement fragmentsElement, string outputDir)
        {
            var fragmentsDir = Path.Combine(outputDir, "contentFragments");
            if (!Directory.Exists(fragmentsDir))
            {
                Directory.CreateDirectory(fragmentsDir);
            }
            
            Logger.WriteLine($"Extracting content fragments to {fragmentsDir}...");

            var scriptedFragmentsElement = fragmentsElement.Elements("scriptedContentFragments").FirstOrDefault();
            if (scriptedFragmentsElement != null)
            {
                foreach (var elem in scriptedFragmentsElement.Elements())
                {
                    ExtractScriptedContentFragment(elem, fragmentsDir);
                }
            }

            return fragmentsDir;
        }

        private string ExtractScriptedContentFragment(XElement contentElement, string outputDir)
        {
            var fragmentId = contentElement.Attribute("instanceIdentifier").Value;
            var fragmentDir = Path.Combine(outputDir, fragmentId);

            if (!Directory.Exists(fragmentDir))
            {
                Directory.CreateDirectory(fragmentDir);
            }
            
            Logger.WriteLine($"Extracting scripted content fragment [{fragmentId}] to {fragmentDir}...");
            
            ExtractMetadata(contentElement, fragmentDir);

            foreach (var elem in contentElement.Elements())
            {
                var nodeName = elem.Name.LocalName;
                switch (nodeName)
                {
                    case "additionalCssScript":
                    case "headerScript":
                    case "contentScript":
                        ExtractContentScript(elem, fragmentDir);
                        break;
                    case "configuration":
                        ExtractCData(elem, fragmentDir, "xml");
                        break;
                    case "languageResources":
                        ExtractLanguageResources(elem, fragmentDir);
                        break;
                    
                    case "files":
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

            foreach (var fileElement in dirElement.Elements())
            {
                ExtractFile(fileElement, dirPath);
            }

            return dirPath;
        }


        private string ExtractFile(XElement fileElement, string outputDir, string prefix = "")
        {
            var fileName = $"{prefix}{fileElement.Attribute("name")?.Value}";
            var filePath = Path.Combine(outputDir, fileName);

            var base64FileData = fileElement.Value;
            var fileData = Convert.FromBase64String(base64FileData);
            
            Logger.WriteLine($"Extracting file {fileName} to {filePath}.");
            
            File.WriteAllBytes(filePath, fileData);

            return filePath;
        }

    }
}