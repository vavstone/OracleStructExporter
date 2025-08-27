using System.Collections.Generic;
using System.Xml.Serialization;

namespace OracleStructExporter.Core
{
    public class Connections
    {
        [XmlArray("Connection")]
        public List<Connection> ConnectionsList  { get; set; }
    }
}