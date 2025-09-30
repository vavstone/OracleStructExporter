using System;

namespace ServiceCheck.Core
{
    public class SchemaWorkStatOuter
    {
        public int ProcessId { get; set; }
        public string DBId { get; set; }
        public string UserName { get; set; }
        public string DbLink { get; set; }
        public DateTime EventTime { get; set; }
        public ExportProgressDataStageOuter Stage { get; set; }
        public ExportProgressDataLevel Level { get; set; }
        public int? ErrorsCount { get; set; }
        //public int? SchemaObjCountPlan { get; set; }
        public int? SchemaObjCountFact { get; set; }
    }
}
