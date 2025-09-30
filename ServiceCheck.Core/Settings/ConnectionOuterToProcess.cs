using System.Collections.Generic;
using System.Xml.Serialization;

namespace ServiceCheck.Core
{
    public class ConnectionOuterToProcess
    {
        [XmlAttribute]
        public string DbId { get; set; }
        [XmlAttribute]
        public string UserName { get; set; }

        [XmlElement("DbOuter")]
        public List<DbOuter> DbOuter { get; set; }
    }
}