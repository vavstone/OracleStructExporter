namespace ServiceCheck.Core
{
    public class ConstraintColumnStruct
    {
        public string Owner { get; set; }
        public string TableName { get; set; }
        public string ConstraintName { get; set; }
        public string ColumnName { get; set; }
        public int? Position { get; set; }
    }
}
