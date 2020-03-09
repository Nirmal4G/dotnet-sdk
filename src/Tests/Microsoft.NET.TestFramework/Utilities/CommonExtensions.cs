using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.NET.TestFramework.Utilities
{
    public static class CommonExtensions
    {
        public static DirectoryInfo Sub(this DirectoryInfo dir, string name)
        {
            return new DirectoryInfo(Path.Combine(dir.FullName, name));
        }

        public static FileInfo GetFile(this DirectoryInfo dir, string name)
        {
            return new FileInfo(Path.Combine(dir.FullName, name));
        }

        public static string ReadAllText(this FileInfo subject)
        {
            return File.ReadAllText(subject.FullName);
        }
    }
}
