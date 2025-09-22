using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace ServiceCheck.Core.Settings
{
    public class LineRuleForIgnoreDiff
    {
        [XmlAttribute]
        public string StaticMask { get; set; }
        [XmlAttribute]
        public bool TrimEmptySpacesBeforeAndAfter { get; set; }
    }
}
