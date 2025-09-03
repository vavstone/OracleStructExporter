using System.Xml.Serialization;

namespace OracleStructExporter.Core
{
    public class BaseRepo
    {
        [XmlAttribute]
        public bool CommitToRepoAfterSuccess { get; set; }
        [XmlElement]
        public string PathToExportDataForRepo { get; set; }

    }
}