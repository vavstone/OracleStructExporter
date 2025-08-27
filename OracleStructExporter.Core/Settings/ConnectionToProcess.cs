namespace OracleStructExporter.Core
{
    public class ConnectionToProcess
    {
        public string DbId { get; set; }
        public string UserName { get; set; }
        public int OneSuccessResultPerHours { get; set; }
        public bool Enabled { get; set; }
    }
}