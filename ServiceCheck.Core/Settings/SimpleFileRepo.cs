using System.Xml.Serialization;
using ServiceCheck.Core.Settings;

namespace ServiceCheck.Core
{
    public class SimpleFileRepo:BaseRepo
    {
        [XmlElement]
        public IgnoreDifferences IgnoreDifferences { get; set; }
    }
}