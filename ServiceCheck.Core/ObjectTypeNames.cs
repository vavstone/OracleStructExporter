using System.Collections.Generic;
using System.Linq;

namespace ServiceCheck.Core
{
    public class ObjectTypeNames
    {
        public string SchemaName { get; set; }
        public string ObjectType { get; set; }
        public List<ObjectTypeName> Objects { get; set; } = new List<ObjectTypeName>();

        public List<string> UniqueNames
        {
            get { return Objects.Select(c => c.ObjectName).Distinct().ToList(); }
        }
    }
}
