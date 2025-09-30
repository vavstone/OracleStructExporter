using System.Collections.Generic;
using System.Xml.Serialization;

namespace ServiceCheck.Core
{
    public class ConnectionsOuterToProcess
    {
        [XmlElement("ConnectionOuterToProcess")]
        public List<ConnectionOuterToProcess> ConnectionListToProcess { get; set; }
    }
}