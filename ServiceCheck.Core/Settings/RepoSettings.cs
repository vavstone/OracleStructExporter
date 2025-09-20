using ServiceCheck.Core.Settings;

namespace ServiceCheck.Core
{
    public class RepoSettings
    {
        public SimpleFileRepo SimpleFileRepo { get; set; }
        public IgnoreDifferences IgnoreDifferences { get; set; }
    }
}