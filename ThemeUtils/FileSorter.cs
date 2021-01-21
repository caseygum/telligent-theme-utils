using System.Collections.Generic;
using System.IO;

namespace nDriven.Telligent.ThemeUtils
{
    public class FileSorter : IComparer<FileInfo>, IComparer<DirectoryInfo>
    {
        public Dictionary<string, int> SortMap { get; }

        public FileSorter(Dictionary<string, int> sortMap)
        {
            SortMap = sortMap;
        }

        public FileSorter(string fileOrderPath)
        {
            SortMap = new FileInfo(fileOrderPath).Deserialize<Dictionary<string, int>>();
        }

        public FileSorter()
        {
        }
        
        public int Compare(FileInfo x, FileInfo y)
        {
            return Compare(x.Name, y.Name);
        }

        public int Compare(DirectoryInfo x, DirectoryInfo y)
        {
            return Compare(x.Name, y.Name);
        }

        private int Compare(string x, string y)
        {
            if (SortMap != null && SortMap.ContainsKey(x) && SortMap.ContainsKey(y))
            {
                return SortMap[x].CompareTo(SortMap[y]);
            }

            return x.CompareTo(y);
        }
    }
}