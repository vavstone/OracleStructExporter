namespace ServiceCheck.Core
{
    public class TableIdentityColumnStruct
    {
        public string Owner { get; set; }
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public string GenerationType { get; set; }
        public string IdentityOptions { get; set; }
    }
}
