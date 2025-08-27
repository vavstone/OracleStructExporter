using System;

namespace OracleStructExporter.Core
{
    public class ThreadInfo
    {
        public ThreadInfo()
        {
            StartDateTime = DateTime.Now;
        }
        public DateTime StartDateTime { get; private set; }
        public Connection Connection { get; set; }
        public ExportSettings ExportSettings { get; set; }
        public string ProcessId { get; set; }
        public bool Finished { get; set; }
    }
}
