using System.Collections.Generic;

namespace OracleStructExporter.Core
{
    public class ConstraintStruct
    {
        public string Owner { get; set; }
        public string TableName { get; set; }
        public string ConstraintName { get; set; }
        public string ConstraintType { get; set; }
        public string Status { get; set; }
        public string Validated { get; set; }
        public string Generated { get; set; }
        public string ROwner { get; set; }
        public string RConstraintName { get; set; }
        public string DeleteRule { get; set; }
        public string SearchCondition { get; set; }
        public List<ConstraintColumnStruct> ConstraintColumnStructs { get; set; } = new List<ConstraintColumnStruct>();

        public IndexStruct BindedIndexStruct { get; set; }
        //внешний ключ
        public ConstraintStruct ReferenceConstraint { get; set; }
    }
}
