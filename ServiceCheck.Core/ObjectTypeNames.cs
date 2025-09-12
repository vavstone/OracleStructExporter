using System.Collections.Generic;

namespace ServiceCheck.Core
{
    public class ObjectTypeNames
    {
        public string ObjectType { get; set; }
        public List<string> ObjectNames { get; set; } = new List<string>();
    }
}
