using System.Xml.Serialization;

namespace OracleStructExporter.Core
{
    public class MaskForFileNames
    {
        [XmlElement]
        public string Include { get; set; }
        [XmlElement]
        public string Exclude { get; set; }
    }
}