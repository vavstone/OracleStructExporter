using System.Collections.Generic;
using System;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace ServiceCheck.Core
{
    public class DbOuter
    {
        [XmlAttribute]
        public string DbLink { get; set; }
        [XmlAttribute]
        public string DbFolder { get; set; }
        [XmlAttribute]
        public int OneSuccessResultPerHours { get; set; }
        [XmlAttribute]
        public bool Enabled { get; set; }
        [XmlAttribute]
        public string Mode { get; set; }

        [XmlElement]
        public string UsersOuterInclude { get; set; }
        [XmlElement]
        public string UsersOuterExclude { get; set; }

        [XmlIgnore]
        public List<string> UsersOuterIncludeC
        {
            get
            {
                if (string.IsNullOrWhiteSpace(UsersOuterInclude))
                    return new List<string>();
                return UsersOuterInclude.Trim().Split(new []{';'}, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
        }

        [XmlIgnore]
        public List<string> UsersOuterExcludeC
        {
            get
            {
                if (string.IsNullOrWhiteSpace(UsersOuterExclude))
                    return new List<string>();
                return UsersOuterExclude.Trim().Split(new []{';'}, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
        }
    }
}