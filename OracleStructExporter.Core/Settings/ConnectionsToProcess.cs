using System.Collections.Generic;

namespace OracleStructExporter.Core
{
    public class ConnectionsToProcess
    {
        public int MaxConnectPerOneProcess { get; set; }
        public List<ConnectionToProcess> ConnectionListToProcess { get; set; }
    }
}