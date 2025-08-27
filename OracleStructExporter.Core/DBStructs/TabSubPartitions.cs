namespace OracleStructExporter.Core
{
    public class TabSubPartitions
    {
        public string TableName { get; set; }
        public string PartitionName { get; set; }
        public string SubPartitionName { get; set; }
        public string HighValue { get; set; }
        public int? SubPartitionPosition { get; set; }
        public string TableSpaceName { get; set; }
    }
}