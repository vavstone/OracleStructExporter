namespace OracleStructExporter.Core
{
    public class DBLog:Log
    {
        public string DBLogPrefix { get; set; }
        public string DBLogDBId { get; set; }
        public string DBLogUserName { get; set; }
    }
}