using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace ServiceCheck.Core.Settings
{
    public class ObjectsListSaveSettings
    {
        [XmlAttribute]
        public bool SaveObjectsList { get; set; }
        [XmlElement]
        public string PathToResFiles { get; set; }
    }
}
