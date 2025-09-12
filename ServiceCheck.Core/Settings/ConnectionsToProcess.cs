using System.Collections.Generic;
using System.Xml.Serialization;

namespace ServiceCheck.Core
{
    public class ConnectionsToProcess
    {
        [XmlAttribute]
        public int MaxConnectPerOneProcess { get; set; }
        [XmlElement("ConnectionToProcess")]
        public List<ConnectionToProcess> ConnectionListToProcess { get; set; }
    }
}