using System.Xml.Serialization;

namespace OracleStructExporter.Core
{
    public class SchedulerSettings
    {
        public ConnectionsToProcess ConnectionsToProcess { get; set; }

        [XmlElement]
        public string PathToExportDataTemp { get; set; }
        [XmlElement]
        public string PathToExportDataWithErrors { get; set; }

        public RepoSettings RepoSettings { get; set; }

        public DBLog DBLog { get; set; }
    }
}