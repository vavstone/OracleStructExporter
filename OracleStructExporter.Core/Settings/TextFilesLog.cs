using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace OracleStructExporter.Core
{
    public class TextFilesLog:Log
    {
        [XmlAttribute]
        public bool Enabled { get; set; }
        [XmlElement]
        public string PathToLogFiles { get; set; }
        [XmlIgnore]
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