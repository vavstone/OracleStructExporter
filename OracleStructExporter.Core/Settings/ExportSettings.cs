using OracleStructExporter.Core.Settings;

namespace OracleStructExporter.Core
{
    public class ExportSettings
    {
        public string PathToExportDataMain { get; set; }
        public string PathToExportDataTemp { get; set; }
        public string PathToExportDataWithErrors { get; set; }
        public RepoSettings RepoSettings { get; set; }

        public bool WriteOnlyToMainDataFolder { get; set; }
        public bool UseProcessesSubFolders { get; set; }
        public DuplicatesClearSettings DuplicatesClearSettings { get; set; }
        public ExportSettingsDetails ExportSettingsDetails { get; set; }

    }
}