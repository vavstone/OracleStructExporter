using System.IO;
using System.Linq;

namespace OracleStructExporter.Core
{
    public static class FilesManager
    {
        

        public static int CopyDirectory(string sourceDir, string destDir)
        {
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);
            var filesCounter = 0;
            foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                string relativePath = file.Substring(sourceDir.Length + 1);
                string destFile = Path.Combine(destDir, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destFile));
                File.Copy(file, destFile,true);
                filesCounter++;
            }
            return filesCounter;
        }

        public static int MoveDirectory(string sourceDir, string destDir)
        {
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);
            Directory.CreateDirectory(destDir);
            var filesCounter = 0;
            if (Directory.Exists(sourceDir))
            {
                foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
                {
                    string relativePath = file.Substring(sourceDir.Length + 1);
                    string destFile = Path.Combine(destDir, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(destFile));
                    File.Copy(file, destFile, true);
                    File.Delete(file);
                    filesCounter++;
                }
                Directory.Delete(sourceDir, true);
            }

            return filesCounter;
        }

        public static void CleanDirectory(string path)
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
            //Directory.CreateDirectory(path);
        }

        public static bool DirectoryIsEmpty(string path)
        {
            return !Directory.GetFiles(path).Any() && !Directory.GetDirectories(path).Any();
        }
    }
}
