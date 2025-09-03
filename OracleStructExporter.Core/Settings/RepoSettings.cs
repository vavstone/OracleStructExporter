using System.Xml.Serialization;

namespace OracleStructExporter.Core
{
    public class RepoSettings
    {
        public SimpleFileRepo SimpleFileRepo { get; set; }
        public GitLabRepo GitLabRepo { get; set; }

    }
}