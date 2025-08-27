using System.IO;
using System.Reflection;

namespace OracleStructExporter.Core
{
    public class TextFilesLog:Log
    {
        internal string PathToLogFiles { get; set; }

        public string PathToLogFilesC
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(PathToLogFiles)) return PathToLogFiles;
                return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logs");
            }
        }
    }
}