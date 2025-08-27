using System;
using System.Collections.Generic;

namespace OracleStructExporter.Core.DBStructs
{
    public class SchedulerJob
    {
        public string JobName { get; set; }
        public string JobType { get; set; }
        public string JobAction { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string RepeatInterval { get; set; }
        public string JobClass { get; set; }
        public string Enabled { get; set; }
        public string AutoDrop { get; set; }
        public string Comments { get; set; }
        public int? NumberOfArguments { get; set; }

        public List<SchedulerJobArgument> ArgumentList { get; set; } = new List<SchedulerJobArgument>();
    }
}
