using System.Xml.Serialization;

namespace OracleStructExporter.Core
{
    public class RepoSettings
    {
        [XmlAttribute]
        public bool CommitToRepoAfterSuccess { get; set; }
        [XmlElement]
        public string PathToExportDataForRepo { get; set; }

    }
}