using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceCheck.Core.DBStructs
{
    public class DbObjectTextByLines
    {
        public string Owner { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }
        public int Line { get; set; }
    }
}
