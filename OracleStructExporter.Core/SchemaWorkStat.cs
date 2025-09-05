using System;

namespace OracleStructExporter.Core
{
    public class SchemaWorkStat
    {
        public int ProcessId { get; set; }
        public string DBId { get; set; }
        public string UserName { get; set; }
        public DateTime EventTime { get; set; }
        public ExportProgressDataStage Stage { get; set; }
        public ExportProgressDataLevel Level { get; set; }
        public int? ErrorsCount { get; set; }
        public int? SchemaObjCountPlan { get; set; }
        public int? SchemaObjCountFact { get; set; }
    }
}
