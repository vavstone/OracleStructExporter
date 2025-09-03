using OracleStructExporter.Core.Settings;
using System.Xml.Serialization;

namespace OracleStructExporter.Core
{
    public class ExportSettings
    {
        [XmlElement]
        public string PathToExportDataMain { get; set; }
        [XmlElement]
        public string PathToExportDataTemp { get; set; }
        [XmlElement]
        public string PathToExportDataWithErrors { get; set; }
        [XmlAttribute]
        public bool WriteOnlyToMainDataFolder { get; set; }
        [XmlAttribute]
        public bool UseProcessesSubFoldersInMain { get; set; }
        [XmlAttribute]
        public bool ClearMainFolderBeforeWriting { get; set; }
        

        //public DuplicatesClearSettings DuplicatesClearSettings { get; set; }
        public ExportSettingsDetails ExportSettingsDetails { get; set; }
        public RepoSettings RepoSettings { get; set; }
    }
}