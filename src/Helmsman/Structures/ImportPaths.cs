#nullable disable
using System;
using System.IO;
using System.Linq;
namespace Helmsman.Structures
{
    public static class ImportPaths
    {
        public static string Normalize(string path,bool windows)
        {
            // Proton exposes the Linux filesystem through its Z: drive.
            if(windows&&path.StartsWith("/",StringComparison.Ordinal))return "Z:"+path.Replace('/','\\');
            return path;
        }
        public static string[] Find(string folder)
        {
            if(!Directory.Exists(folder))throw new DirectoryNotFoundException("Import folder not found: "+folder);
            return Directory.GetFiles(folder).Where(p=>string.Equals(Path.GetExtension(p),".blueprint",StringComparison.OrdinalIgnoreCase)||string.Equals(Path.GetExtension(p),".vbuild",StringComparison.OrdinalIgnoreCase)).OrderBy(Path.GetFileName,StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }
}
