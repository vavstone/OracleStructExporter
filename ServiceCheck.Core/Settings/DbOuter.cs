using System.Xml.Serialization;

namespace ServiceCheck.Core
{
    public class DbOuter
    {
        [XmlAttribute]
        public int OneSuccessResultPerHours { get; set; }
        [XmlAttribute]
        public bool Enabled { get; set; }

        [XmlElement]
        public string UsersOuterInclude { get; set; }
        [XmlElement]
        public string UsersOuterExclude { get; set; }
    }
}