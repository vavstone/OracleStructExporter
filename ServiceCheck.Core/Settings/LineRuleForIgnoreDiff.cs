using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceCheck.Core.Settings
{
    public class LineRuleForIgnoreDiff
    {
        public string StaticMask { get; set; }
        public bool TrimEmptySpacesBeforeAndAfter { get; set; }
    }
}
