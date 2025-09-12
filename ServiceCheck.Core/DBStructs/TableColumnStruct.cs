namespace ServiceCheck.Core
{
    public class TableColumnStruct
    {
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public string DataTypeOwner { get; set; }
        public int DataLength { get; set; }
        public int? CharColDeclLength { get; set; }
        public int? CharLength { get; set; }
        public int? DataPrecision { get; set; }
        public int? DataScale { get; set; }
        public string Nullable { get; set; }
        public int? ColumnId { get; set; }
        public string DataDefault { get; set; }
        public string HiddenColumn { get; set; }
        public string VirtualColumn { get; set; }
        public string CharUsed { get; set; }
        public string DefaultOnNull { get; set; }

        public string ColumnNameToShow =>
            ColumnName.ContainsLowerCaseSymbols()
                ? $@"""{ColumnName}"""
                : ColumnName.ToLower();

        public TableIdentityColumnStruct IdentityColumnStruct { get; set; }
        
    }
}
