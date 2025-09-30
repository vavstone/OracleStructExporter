using System.Collections.Generic;
using System.Xml.Serialization;

namespace ServiceCheck.Core
{
    [XmlRoot("OSESettings")]
    public class OSESettings
    {
        [XmlElement]
        public string AppType { get; set; }
        [XmlElement]
        public bool TestMode { get; set; }
        

        [XmlArray("Connections")]
        [XmlArrayItem("Connection")]
        public List<Connection> Connections { get; set; }

        public SchedulerSettings SchedulerSettings { get; set; }
        public WinAppSettings WinAppSettings { get; set; }
        public SchedulerOuterSettings SchedulerOuterSettings { get; set; }

        public ExportSettings ExportSettings { get; set; }
        public TextFilesLog TextFilesLog { get; set; }

        //public LogSettings LogSettings { get; set; }

        [XmlIgnore]
        public bool IsScheduler
        {
            get
            {
                return AppType == "Scheduler";
            }
        }

        [XmlIgnore]
        public bool IsWinApp
        {
            get
            {
                return AppType == "WinApp";
            }
        }

        [XmlIgnore]
        public bool IsSchedulerOuter
        {
            get
            {
                return AppType == "SchedulerOuter";
            }
        }

        //public void RepairSettingsValues()
        //{
            //некоторые значения настроек несовместимы между собой
            //Если в БД не сохраняем общий лог и лог по потокам, то пока нет возможности отслеживать графики выполнения заданий планировщика
            //if (!LogSettings.DBLog.Enabled && SchedulerSettings!=null && SchedulerSettings.ConnectionsToProcess!=null)
            //    SchedulerSettings.ConnectionsToProcess.ConnectionListToProcess.ForEach(c => c.Enabled = false);
            //Если в БД не сохраняем общий лог, то пока нет возможности раскладывать результаты в разрезах: дата-processid
            //if (!LogSettings.DBLog.Enabled)
            //    ExportSettings.UseProcessesSubFoldersInMain = false;
            //if (!ExportSettings.UseProcessesSubFolders && ExportSettings.DuplicatesClearSettings!=null)
            //    ExportSettings.DuplicatesClearSettings.ClearDuplicatesInMainFolder = false;
        //}
    }
}