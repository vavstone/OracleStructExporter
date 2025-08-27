namespace OracleStructExporter.Core
{
    public class TableStruct
    {
        public string TableName { get; set; }
        public string Partitioned { get; set; }
        public string Temporary { get; set; }
        public string Duration { get; set; }
        public string Compression { get; set; }
        public string IOTType { get; set; }
        public string Logging { get; set; }
        public string Dependencies { get; set; }
    }
}
