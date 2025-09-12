using System.Xml.Serialization;

namespace ServiceCheck.Core
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

        [XmlElement]
        public int GetStatForLastDays { get; set; }
        [XmlElement]
        public int MinSuccessResultsForStat { get; set; }
        
    }
}