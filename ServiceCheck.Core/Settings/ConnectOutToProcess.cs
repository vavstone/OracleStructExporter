using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace ServiceCheck.Core.Settings
{
    public class SchemaWorkAggrFullStatOut
    {
        public string DbId { get; set; }
        public string UserName { get; set; }
        public string DbLink { get; set; }
        public int? OneSuccessResultPerHours { get; set; }
        public bool Enabled { get; set; }
    }
}
