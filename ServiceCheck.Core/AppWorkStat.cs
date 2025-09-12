using System;

namespace ServiceCheck.Core
{
    public class AppWorkStat
    {
        public int ProcessId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
