using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace ServiceCheck.Core.Settings
{
    public class IgnoreDifferences
    {
        [XmlElement("ConnectionForIgnoreDiff")]
        public List<ConnectionForIgnoreDiff> ConnectionsForIgnoreDiff { get; set; }
    }
}
