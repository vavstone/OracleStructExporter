using System.Collections.Generic;

namespace ServiceCheck.Core
{
    public class PartTables
    {
        public string TableName { get; set; }
        public string PartitioningType { get; set; }
        public string SubPartitioningType { get; set; }
        public int? PartitionCount { get; set; }
        public int? DefSubPartitionCount { get; set; }
        public int? PartitioningKeyCount { get; set; }
        public int? SubPartitioningKeyCount { get; set; }
        public string DefTableSpaceName { get; set; }
        public string Interval { get; set; }
        public List<PartOrSubPartKeyColumns> PartKeyColumns { get; set; } = new List<PartOrSubPartKeyColumns>();
        public List<PartOrSubPartKeyColumns> SubPartKeyColumns { get; set; } = new List<PartOrSubPartKeyColumns>();
        public List<TabPartitions> Partitions { get; set; } = new List<TabPartitions>();
        
    }
}