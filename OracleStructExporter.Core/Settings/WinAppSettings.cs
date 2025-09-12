using System.Xml.Serialization;

namespace OracleStructExporter.Core
{
    public class WinAppSettings
    {
        [XmlAttribute]
        public bool ClearMainFolderBeforeWriting { get; set; }
    }
}