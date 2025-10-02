namespace ServiceCheck.Core
{
    public class PartOrSubPartKeyColumns
    {
        public string Owner { get; set; }
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public int? ColumnPosition { get; set; }
    }
}