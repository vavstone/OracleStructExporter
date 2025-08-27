using System.Collections.Generic;

namespace OracleStructExporter.Core.Settings
{
    public class DuplicatesClearSettings
    {
        public bool ClearDuplicatesInMainFolder { get; set; }
        public List<string> FilesToExcludeFromCheckingOnDoubles { get; set; }
    }
}
