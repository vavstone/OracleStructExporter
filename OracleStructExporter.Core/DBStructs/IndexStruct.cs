using System.Collections.Generic;

namespace OracleStructExporter.Core
{
    public class IndexStruct
    {
        public string TableName { get; set; }
        //public string TableType { get; set; }
        public string IndexName { get; set; }
        public string IndexType { get; set; }
        public string Uniqueness { get; set; }
        public string Compression { get; set; }
        public int? PrefixLength { get; set; }
        public string Logging { get; set; }
        public string Locality { get; set; }
        public List<IndexColumnStruct> IndexColumnStructs { get; set; } = new List<IndexColumnStruct>();
        //public List<string> IndexExpressions { get; set; } = new List<string>();

        public ConstraintStruct BindedConstraintStruct { get; set; }
    }
}
