namespace ServiceCheck.Core
{
    public class IndexColumnStruct
    {
        public string IndexName { get; set; }
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public int? ColumnPosition { get; set; }
        public string Descend { get; set; }
        public string Expression { get; set; }

        public TableColumnStruct BindedTableColumnStruct { get; set; }
    }
}
