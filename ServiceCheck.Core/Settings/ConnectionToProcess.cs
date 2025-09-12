using System.Xml.Serialization;

namespace ServiceCheck.Core
{
    public class ConnectionToProcess
    {
        [XmlAttribute]
        public string DbId { get; set; }
        [XmlAttribute]
        public string UserName { get; set; }
        [XmlAttribute]
        public int OneSuccessResultPerHours { get; set; }
        [XmlAttribute]
        public bool Enabled { get; set; }
    }
}