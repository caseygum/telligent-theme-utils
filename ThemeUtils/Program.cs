using System;
using System.Linq;

namespace nDriven.Telligent.ThemeUtils
{
    class Program
    {
        static void Main(string[] args)
        {
            var isExtract = GetArgumentFlag(args, "extract");
            var isPackage = GetArgumentFlag(args, "package");
            if (!isPackage && !isExtract)
            {
                Console.WriteLine("You must specify either --extract or --package");
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

            var themeExtractor = new ThemeExtractor(themeFilePath, outputDirectory);
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
            Console.WriteLine("ERROR: Not implemented yet. Coming soon?");
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