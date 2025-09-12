using System.Collections.Generic;

namespace ServiceCheck.Core
{
    public class TabPartitions
    {
        public string TableName { get; set; }
        public string PartitionName { get; set; }
        public int? SubPartitionCount { get; set; }
        public string HighValue { get; set; }
        public int? PartitionPosition { get; set; }
        public string TableSpaceName { get; set; }
        public List<TabSubPartitions> SubPartitions { get; set; } = new List<TabSubPartitions>();
    }
}