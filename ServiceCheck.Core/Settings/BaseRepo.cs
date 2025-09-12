using System.Xml.Serialization;

namespace ServiceCheck.Core
{
    public class BaseRepo
    {
        [XmlAttribute]
        public bool CommitToRepoAfterSuccess { get; set; }
        [XmlElement]
        public string PathToExportDataForRepo { get; set; }

    }
}