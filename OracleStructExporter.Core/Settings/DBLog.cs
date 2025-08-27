using System.Xml.Serialization;

namespace OracleStructExporter.Core
{
    public class DBLog:Log
    {
        [XmlAttribute]
        public string DBLogPrefix { get; set; }
        [XmlAttribute]
        public string DBLogDBId { get; set; }
        [XmlAttribute]
        public string DBLogUserName { get; set; }
    }
}