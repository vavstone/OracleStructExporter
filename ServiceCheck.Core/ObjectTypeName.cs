using System;
using System.Collections.Generic;

namespace ServiceCheck.Core
{
    public class ObjectTypeName
    {
        public string Owner { get; set; }
        public string ObjectType { get; set; }
        public string ObjectName { get; set; }
        public int? ObjectId { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? LastDDLTime { get; set; }
        public string Status { get; set; }
        public string Generated { get; set; }
    }
}
