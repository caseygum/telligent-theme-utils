using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace nDriven.Telligent.ThemeUtils
{
    public class ThemeType
    {
        public static ThemeType Site { get; } = new ThemeType("0c647246673542f9875dc8b991fe739b", "Site");
        public static ThemeType Blog { get; } = new ThemeType("a3b17ab0af5f11dda3501fcf55d89593", "Blog");
        public static ThemeType Group { get; } = new ThemeType("c6108064af6511ddb074de1a56d89593", "Group");

        public static ThemeType FromId(string id)
        {
            var types = new[] {Site, Blog, Group};
            var themeType = types.FirstOrDefault(x => x.Id.ToLower() == id.ToLower());
            return themeType ?? new ThemeType(id, $"Unknown Theme Type ({id})");
        }
        
        public static ThemeType FromName(string name)
        {
            var types = new[] {Site, Blog, Group};
            var themeType = types.FirstOrDefault(x => x.Name.ToLower() == name.ToLower());
            if (themeType == null)
            {
                throw new ArgumentException($"Unknown type name: {name}", nameof(name));
            }

            return themeType;
        }
        
        public string Id { get; }
        public string Name { get; }

        private ThemeType(string id, string name)
        {
            Id = id.ToLower();
            Name = name;
        }

        public class ThemeSorter : IComparer<string>, IComparer<FileInfo>
        {
            public int Compare(string x, string y)
            {
                var typeX = FromName(x);
                var typeY = FromName(y);
                return string.Compare(typeX.Id, typeY.Id, StringComparison.Ordinal);
            }

            public int Compare(FileInfo x, FileInfo y)
            {
                return Compare(x.Directory.Name, y.Directory.Name);
            }
        }
        
        
    }
}