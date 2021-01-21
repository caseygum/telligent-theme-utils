using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace nDriven.Telligent.ThemeUtils
{
    public class ThemePackager
    {
        private enum PackageType
        {
            SingleTheme,
            MultiTheme,
            Invalid
        }
        
        public string ThemeFilePath { get; }
        public DirectoryInfo SourceDirectory { get; }
        public TextWriter Logger { get; }

        public ThemePackager([NotNull] string themeFilePath, [NotNull] string sourceDirectory, bool clean = false, TextWriter logger = null)
        {
            SourceDirectory = new DirectoryInfo(sourceDirectory.Trim());

            if (!SourceDirectory.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory could not be found: {SourceDirectory.FullName}.");
            }
            
            ThemeFilePath = themeFilePath.Trim();
            
            if (File.Exists(ThemeFilePath))
            {
                if (clean)
                {
                    File.Delete(ThemeFilePath);
                }
                else
                {
                    throw new ArgumentException(
                        $"File already exists: {ThemeFilePath}.  Use the --clean flag to delete existing file before packaging.",
                        themeFilePath);
                }
            }
            
            Logger = logger ?? Console.Out;
        }

        public void Package()
        {
            var fileInfo = new FileInfo(ThemeFilePath);

            if (!Directory.Exists(fileInfo.DirectoryName))
            {
                Directory.CreateDirectory(fileInfo.DirectoryName);
            }

            var themeDoc = CreateThemeDocument();
            
            Package(themeDoc);
            
            Logger.WriteLine("Done!");
        }

        private void Package(XDocument themeDoc)
        {
            if (themeDoc.Root == null)
            {
                Package(themeDoc, SourceDirectory);
            }
            else
            {
                var themeMetadataFiles = SourceDirectory.GetFiles(Constants.ThemeMetadataFilename, SearchOption.AllDirectories);
                Array.Sort(themeMetadataFiles, new ThemeType.ThemeSorter());
                foreach (var themeMetadataFile in themeMetadataFiles)
                {
                    var themeDir = themeMetadataFile.Directory;
                    Package(themeDoc, themeDir);
                }
            }
            
            SerializationUtils.SaveXml(themeDoc, ThemeFilePath);
        }

        private void Package(XDocument themeDoc, DirectoryInfo themeDir)
        {
            var themeElement = CreateThemeElement(themeDoc, themeDir);
            PackageContentScript(themeElement, themeDir, Constants.HeadScriptElementName, true);
            PackageContentScript(themeElement, themeDir, Constants.BodyScriptElementName, true);
            PackageCData(themeElement, themeDir, Constants.ConfigurationElementName, Constants.XmlExtension);
            PackageCData(themeElement, themeDir, Constants.PaletteTypesElementName, Constants.XmlExtension);
            PackageCData(themeElement, themeDir, Constants.LanguageResourcesElementName, Constants.XmlExtension);
            PackageFile(themeElement, themeDir, Constants.PreviewImagePrefix + "*", null, Constants.PreviewImageElementName, x => x.Replace(Constants.PreviewImagePrefix, ""));
            PackageFolder(themeElement, themeDir, Constants.FilesElementName);
            PackageFolder(themeElement, themeDir, Constants.JavascriptFilesElementName);
            PackageFolder(themeElement, themeDir, Constants.StyleFilesElementName);
            PackagePageLayouts(themeElement, themeDir);
            PackageXml(themeElement, themeDir, Constants.ScopedPropertiesElementName, Constants.XmlExtension);
        }

        private void PackagePageLayouts(XElement root, DirectoryInfo directory)
        {
            var folder = directory.GetDirectories(Constants.PageLayoutsElementName, SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (folder == null) return;
            var pageLayoutsElement = new XElement(Constants.PageLayoutsElementName);
            
            PackageXml(pageLayoutsElement, folder, Constants.HeadersElementName, Constants.XmlExtension);
            PackageXml(pageLayoutsElement, folder, Constants.FootersElementName, Constants.XmlExtension);
            PackageXml(pageLayoutsElement, folder, Constants.PagesElementName, Constants.XmlExtension);
            PackageXml(pageLayoutsElement, folder, Constants.ScopedPropertiesElementName, Constants.XmlExtension);
            PackageContentFragments(pageLayoutsElement, folder);
            
            root.Add(pageLayoutsElement);
        }

        private void PackageContentFragments(XElement root, DirectoryInfo directory)
        {
            var contentFragmentElem = new XElement(Constants.ContentFragmentsElementName);
            var scriptedFragmentsElem = new XElement(Constants.ScriptedContentFragmentsElementName);
            contentFragmentElem.Add(scriptedFragmentsElem);
            root.Add(contentFragmentElem);
            var fragsDir = directory.GetDirectories(Constants.ContentFragmentsElementName, SearchOption.TopDirectoryOnly).FirstOrDefault();

            if (fragsDir == null) return;
            var fragDirs = fragsDir.GetDirectories();

            var folderMetadataFile = fragsDir.GetFiles(Constants.FileMetadataFilename).FirstOrDefault();
            if (folderMetadataFile != null)
            {
                var metadata = folderMetadataFile.Deserialize<DirectoryMetadata>();
                var sorter = new FileSorter(metadata.DirectoryOrder);
                Array.Sort(fragDirs, sorter);
            }

            foreach (var fragDir in fragDirs)
            {
                PackageScriptedContentFragment(scriptedFragmentsElem, fragDir);
            }
        }

        private void PackageScriptedContentFragment(XElement root, DirectoryInfo directory)
        {
            var metadataFileInfo = directory.GetFiles(Constants.ContentFragmentMetadataFilename, SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (metadataFileInfo == null)
            {
                Logger.WriteLine($"Could not find {Constants.ContentFragmentMetadataFilename} file in {directory.FullName}. Skipping.");
            }

            var scriptedContentFragmentElement = new XElement(Constants.ScriptedContentFragmentElementName);
            var attributes = metadataFileInfo.Deserialize<Dictionary<string, string>>();
            foreach (var kvp in attributes)
            {
                scriptedContentFragmentElement.SetAttributeValue(kvp.Key, kvp.Value);
            }
            
            root.Add(scriptedContentFragmentElement);
            
            PackageContentScript(scriptedContentFragmentElement, directory, Constants.ContentScriptElementName);
            PackageContentScript(scriptedContentFragmentElement, directory, Constants.HeaderScriptElementName);
            PackageCData(scriptedContentFragmentElement, directory, Constants.ConfigurationElementName, Constants.XmlExtension);
            PackageCData(scriptedContentFragmentElement, directory, Constants.LanguageResourcesElementName, Constants.XmlExtension);
            PackageContentScript(scriptedContentFragmentElement, directory, Constants.AdditionalCssScriptElementName);
            PackageXml(scriptedContentFragmentElement, directory, Constants.RequiredContextElementName, Constants.XmlExtension);
            PackageFolder(scriptedContentFragmentElement, directory, Constants.FilesElementName, false);
        }

        private void PackageFolder(XElement root, DirectoryInfo directory, string folderElementName, bool packageFileContentAsCData = true)
        {
            var folder = directory.GetDirectories(folderElementName, SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (folder == null) return;
            var folderElement = new XElement(folderElementName);

            var fileMetadataFileInfo = folder.GetFiles(Constants.FileMetadataFilename).FirstOrDefault();

            if (fileMetadataFileInfo == null)
            {
                throw new FileNotFoundException("Cannot find file metadata.",
                    Path.Combine(folder.FullName, Constants.FileMetadataFilename));
            }

            var fileMetadata = fileMetadataFileInfo.Deserialize<FileMetadata>();
            
            var files = folder.GetFiles().Where(x => x.Name != Constants.FileMetadataFilename).ToArray();
            var fileSorter = new FileSorter(fileMetadata.FileOrder);
            Array.Sort(files, fileSorter);

            foreach (var fileInfo in files)
            {
                fileMetadata.FileAttributes.TryGetValue(fileInfo.Name, out var attributes);
                PackageFile(folderElement, fileInfo,  attributes, packageFileContentAsCData:packageFileContentAsCData);
            }
            
            root.Add(folderElement);
        }

        private void PackageFile(XElement root, DirectoryInfo directory, string searchPattern, Dictionary<string, string> attributes = null,  string elementName = Constants.FileElementName, Func<string, string> fileNameTransform = null, bool packageFileContentAsCData = true)
        {
            var fileInfo = directory.GetFiles(searchPattern).FirstOrDefault();
            if (fileInfo == null) return;
            PackageFile(root, fileInfo, attributes, elementName,  fileNameTransform, packageFileContentAsCData);
        }

        private void PackageFile(XElement root, FileInfo fileInfo, Dictionary<string, string> attributes = null, string elementName = Constants.FileElementName, Func<string, string> fileNameTransform = null, bool packageFileContentAsCData = true)
        {
            var fileName = fileNameTransform == null ? fileInfo.Name : fileNameTransform(fileInfo.Name);
            var fileElement = new XElement(elementName);
            fileElement.SetAttributeValue(Constants.NameAttributeName, fileName);
            var encodedFile = Convert.ToBase64String(File.ReadAllBytes(fileInfo.FullName));
            if (packageFileContentAsCData)
            {
                var cdata = new XCData(encodedFile);
                fileElement.Add(cdata);
            }
            else
            {
                fileElement.Value = encodedFile;
            }
            
            root.Add(fileElement);

            if (attributes == null) return;
            
            foreach (var kvp in attributes)
            {
                fileElement.SetAttributeValue(kvp.Key, kvp.Value);
            }
        }

        private void PackageContentScript(XElement root, DirectoryInfo directory, string elementName, bool throwIfNotFound = false)
        {
            var scriptFile = directory.GetFiles($"{elementName}.*", SearchOption.TopDirectoryOnly)
                .FirstOrDefault();
            if (scriptFile == null)
            {
                if (throwIfNotFound) throw new FileNotFoundException( $"Could not find file matching: {Path.Combine(directory.FullName, elementName)}.*");
                return;
            }

            var extension = scriptFile.Extension.Trim('.');
            var scriptElement = new XElement(elementName);
            var language = Constants.UnknownLanguageName;
            switch (extension)
            {
                case Constants.VelocityLanguageExtension:
                    language = Constants.VelocityLanguageName;
                    break;
                case Constants.JavaScriptLanguageExtension:
                    language = Constants.JavaScriptLanguageName;
                    break;
            }
            scriptElement.SetAttributeValue(Constants.LanguageAttributeName, language);
            var fileContent = File.ReadAllText(scriptFile.FullName);
            if (!String.IsNullOrEmpty(fileContent))
            {
                var cdata = new XCData(fileContent);
                scriptElement.Add(cdata);
            }
            root.Add(scriptElement);
        }

        private void PackageCData(XElement root, DirectoryInfo directory, string elementName, string fileExtension, bool throwIfNotFound = false)
        {
            var cdataElement = new XElement(elementName);
            var file = directory.GetFiles($"{elementName}.{fileExtension}", SearchOption.TopDirectoryOnly)
                .FirstOrDefault();
            if (file == null)
            {
                if (throwIfNotFound) throw new FileNotFoundException($"Could not find file matching: {Path.Combine(directory.FullName, elementName)}.{fileExtension}");
                return;
            }
            var fileContent = File.ReadAllText(file.FullName);
            if (!String.IsNullOrEmpty(fileContent))
            {
                var cdata = new XCData(fileContent);
                cdataElement.Add(cdata);
                root.Add(cdataElement);
            }
        }
        
        private void PackageXml(XElement root, DirectoryInfo directory, string elementName, string fileExtension, bool throwIfNotFound = false)
        {
            var file = directory.GetFiles($"{elementName}.{fileExtension}", SearchOption.TopDirectoryOnly)
                .FirstOrDefault();
            if (file == null)
            {
                if (throwIfNotFound) throw new FileNotFoundException($"Could not find file matching: {Path.Combine(directory.FullName, elementName)}.{fileExtension}");
                return;
            }

            var xElem = XElement.Load(file.FullName);
            root.Add(xElem);
        }

        private XElement CreateThemeElement(XDocument themeDoc, DirectoryInfo themeDir)
        {
            var themeElement = new XElement("theme");
            if (themeDoc.Root == null)
            {
                themeDoc.Add(themeElement);
            }
            else
            {
                themeDoc.Root.Add(themeElement);
            }

            var metaDataFile = themeDir.GetFiles(Constants.ThemeMetadataFilename, SearchOption.TopDirectoryOnly).FirstOrDefault();

            var metaData = metaDataFile.Deserialize<Dictionary<string, string>>();

            foreach (var kvp in metaData)
            {
                themeElement.SetAttributeValue(kvp.Key, kvp.Value);
            }

            return themeElement;
        }
        

        private XDocument CreateThemeDocument()
        {
            var themeDoc = new XDocument();
            var packageType = GetPackageType();

            switch (packageType)
            {
                case PackageType.MultiTheme:
                    themeDoc.Add(new XElement("themes"));
                    return themeDoc;
                case PackageType.SingleTheme:
                    return themeDoc;
            }
            throw new Exception($"Could not find a {Constants.ThemeMetadataFilename} file in the top level directory or one of the subdirectories of {SourceDirectory.FullName}");
        }


        private PackageType GetPackageType()
        {
            try
            {
                var metadataFile = SourceDirectory.GetFiles(Constants.ThemeMetadataFilename, SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (metadataFile != null)
                {
                    return PackageType.SingleTheme;
                }

                var hasSubTheme = SourceDirectory.GetFiles(Constants.ThemeMetadataFilename, SearchOption.AllDirectories).Any();
                if (hasSubTheme)
                {
                    return PackageType.MultiTheme;
                }
                
                return PackageType.Invalid;
            }
            catch
            {
                return PackageType.Invalid;
            }
        }
    }
    
}