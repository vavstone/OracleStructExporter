using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceCheck.Core.DBStructs
{
    public class DbPackageText
    {
        public string Owner { get; set; }
        public string Name { get; set; }
        public string Header { get; set; }
        public string Body { get; set; }
    }
}
