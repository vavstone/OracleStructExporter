using System.Collections.Generic;
using System;
using System.Linq;
using System.Xml.Serialization;

namespace ServiceCheck.Core
{
    public class DBLog:Log
    {
        [XmlAttribute]
        public string DBLogPrefix { get; set; }
        [XmlAttribute]
        public string DBLogDBId { get; set; }
        [XmlAttribute]
        public string DBLogUserName { get; set; }

        [XmlElement]
        public string ExludeCONNWORKLOGColumns { get; set; }
        [XmlIgnore]
        public List<string> ExludeCONNWORKLOGColumnsC
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ExludeCONNWORKLOGColumns))
                    return new List<string>();
                return ExludeCONNWORKLOGColumns.Trim().Split(new []{';'}, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
        }
    
    }
}