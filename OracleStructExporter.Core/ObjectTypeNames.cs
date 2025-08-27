using System.Collections.Generic;

namespace OracleStructExporter.Core
{
    public class ObjectTypeNames
    {
        public string ObjectType { get; set; }
        public List<string> ObjectNames { get; set; } = new List<string>();
    }
}
