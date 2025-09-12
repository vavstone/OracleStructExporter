using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace ServiceCheck.Core
{
    public class TextFilesLog:Log
    {
        [XmlAttribute]
        public bool Enabled { get; set; }
        [XmlElement]
        public string PathToMainLogFiles { get; set; }
        [XmlElement]
        public string PathToThreadsLogFiles { get; set; }
        [XmlElement]
        public string PathToStatFiles { get; set; }
        [XmlElement]
        public string PathToLogCommitsFiles { get; set; }

        [XmlElement]
        public LogSplitPeriod MainLogSplitPeriod { get; set; }
        [XmlElement]
        public LogSplitPeriod ThreadLogSplitPeriod { get; set; }
        [XmlElement]
        public LogSplitPeriod StatSplitPeriod { get; set; }
        [XmlElement]
        public LogSplitPeriod LogCommitSplitPeriod { get; set; }

        [XmlIgnore]
        public string PathToMainLogFilesC
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(PathToMainLogFiles)) return PathToMainLogFiles;
                return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logs", "main");
            }
        }
        [XmlIgnore]
        public string PathToThreadsLogFilesC
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(PathToThreadsLogFiles)) return PathToThreadsLogFiles;
                return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logs", "threads");
            }
        }
        [XmlIgnore]
        public string PathToStatFilesC
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(PathToStatFiles)) return PathToStatFiles;
                return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logs", "stat");
            }
        }
        [XmlIgnore]
        public string PathToLogCommitsFilesC
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(PathToLogCommitsFiles)) return PathToLogCommitsFiles;
                return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logs", "commits");
            }
        }
    }
}