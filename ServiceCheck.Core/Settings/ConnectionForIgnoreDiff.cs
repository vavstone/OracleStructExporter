using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace ServiceCheck.Core.Settings
{
    public class ConnectionForIgnoreDiff
    {
        [XmlAttribute]
        public string DbId { get; set; }
        [XmlAttribute]
        public string UserName { get; set; }
        [XmlElement("FileForIgnoreDiff")]
        public List<FileForIgnoreDiff> FilesForIgnoreDiff { get; set; }
    }
}
