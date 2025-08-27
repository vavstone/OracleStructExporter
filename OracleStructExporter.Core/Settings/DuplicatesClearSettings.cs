using System.Collections.Generic;
using System.Xml.Serialization;

namespace OracleStructExporter.Core.Settings
{
    public class DuplicatesClearSettings
    {
        [XmlAttribute]
        public bool ClearDuplicatesInMainFolder { get; set; }

        [XmlArray("FilesToExcludeFromCheckingOnDoubles")]
        [XmlArrayItem("Item")]
        public List<string> FilesToExcludeFromCheckingOnDoubles { get; set; }
    }
}
