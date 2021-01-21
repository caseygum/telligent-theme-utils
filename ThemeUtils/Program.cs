using System;
using System.Linq;

namespace nDriven.Telligent.ThemeUtils
{
    class Program
    {
        static void Main(string[] args)
        {
            var isHelp = GetArgumentFlag(args, "help");
            if (isHelp)
            {
                ShowUsage();
                return;
            }
            
            var isExtract = GetArgumentFlag(args, "extract");
            var isPackage = GetArgumentFlag(args, "package");
            if (!isPackage && !isExtract)
            {
                Console.WriteLine("You must specify either --extract or --package");
                Console.WriteLine("");
                ShowUsage();
                return;
            }
            
            if (isPackage && isExtract)
            {
                Console.WriteLine("You must specify either --extract or --package, but not both.");
                return;
            }

            if (isExtract) Extract(args);
            if (isPackage) Package(args);
        }

        private static void Extract(string[] args)
        {
            var themeFilePath = GetArgumentValue(args, "themeFile");
            if (themeFilePath == null)
            {
                Console.WriteLine("You must specify the theme file to extract --themeFile=PATH_TO_THEME_FILE");
                return;
            }
            var outputDirectory = GetArgumentValue(args, "outputDir");
            if (outputDirectory == null)
            {
                Console.WriteLine("You must specify an output directory --outputDir=OUTPUT_DIRECTORY");
                return;
            }
            var clean = GetArgumentFlag(args, "clean");

            var themeExtractor = new ThemeExtractor(themeFilePath, outputDirectory, clean);
            try
            {
                themeExtractor.Extract();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting theme [{themeFilePath}] to [{outputDirectory}].");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private static void Package(string[] args)
        {
            var themeFilePath = GetArgumentValue(args, "themeFile");
            if (themeFilePath == null)
            {
                Console.WriteLine("You must specify the theme file to write to --themeFile=PATH_TO_OUTPUT_THEME_FILE");
                return;
            }
            var sourceDirectory = GetArgumentValue(args, "sourceDir");
            if (sourceDirectory == null)
            {
                Console.WriteLine("You must specify a source directory --sourceDir=PATH_TO_SOURCE_DIR");
                return;
            }
            var clean = GetArgumentFlag(args, "clean");

            var themePackager = new ThemePackager(themeFilePath, sourceDirectory, clean);
            try
            {
                themePackager.Package();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error packaging theme from [{sourceDirectory}] to [{themeFilePath}].");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private static void ShowUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("\tExtract directory from theme file: ");
            Console.WriteLine("\t\tThemeUtils --extract [--clean] --themeFile=PATH_TO_INPUT_THEME_FILE --outputDir=PATH_TO_OUTPUT_DIR ");
            Console.WriteLine();
            Console.WriteLine("\t\t--clean: [optional] delete output directory before extracting.  An exception will be thrown if the directory already exists.");
            
            Console.WriteLine("\tPackage directory into theme file: ");
            Console.WriteLine("\t\tThemeUtils --package [--clean] --sourceDir=PATH_TO_SOURCE_DIR --themeFile=PATH_TO_OUTPUT_THEME_FILE");
            Console.WriteLine();
            Console.WriteLine("\t\t--clean: [optional] delete theme file before packaging.  An exception will be thrown if the file already exists.");
        }
        
        private static string GetArgumentValue(string[] args, string argumentName)
        {
            var fullArgument = args.FirstOrDefault(arg => arg.StartsWith($"--{argumentName}="));
            var parts = fullArgument?.Split('=', 2);
            return parts?[1].Trim();
        }
        
        private static bool GetArgumentFlag(string[] args, string argumentName)
        {
            var fullArgument = args.FirstOrDefault(arg => arg.Equals($"--{argumentName}"));
            return fullArgument != null;
        }
    }
}