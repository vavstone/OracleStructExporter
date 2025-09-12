using System.Xml.Serialization;

namespace ServiceCheck.Core
{
    public class WinAppSettings
    {
        [XmlAttribute]
        public bool ClearMainFolderBeforeWriting { get; set; }
    }
}