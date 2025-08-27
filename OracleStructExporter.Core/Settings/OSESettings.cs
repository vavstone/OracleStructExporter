namespace OracleStructExporter.Core
{
    public class OSESettings
    {
        public Connections Connections { get; set; }
        public SchedulerSettings SchedulerSettings { get; set; }
        public ExportSettings ExportSettings { get; set; }
        public LogSettings LogSettings { get; set; }

        public void RepairSettingsValues()
        {
            //некоторые значения настроек несовместимы между собой
            //Если в БД не сохраняем общий лог и лог по потокам, то пока нет возможности отслеживать графики выполнения заданий планировщика
            if (!LogSettings.DBLog.Enabled)
                SchedulerSettings.ConnectionsToProcess.ConnectionListToProcess.ForEach(c => c.Enabled = false);
            //Если в БД не сохраняем общий лог, то пока нет возможности раскладывать результаты в разрезах: дата-processid
            if (!LogSettings.DBLog.Enabled)
                ExportSettings.UseProcessesSubFolders = false;
            if (!ExportSettings.UseProcessesSubFolders && ExportSettings.DuplicatesClearSettings!=null)
                ExportSettings.DuplicatesClearSettings.ClearDuplicatesInMainFolder = false;
        }
    }
}