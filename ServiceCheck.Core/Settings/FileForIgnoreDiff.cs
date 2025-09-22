using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace ServiceCheck.Core.Settings
{
    public class FileForIgnoreDiff
    {
        [XmlAttribute]
        public string FolderName { get; set; }
        [XmlAttribute]
        public string FileName { get; set; }
        [XmlElement("LineRuleForIgnoreDiff")]
        public List<LineRuleForIgnoreDiff> LineRulesForIgnoreDiff { get; set; }
    }
}
