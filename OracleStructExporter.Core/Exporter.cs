using OracleStructExporter.Core.DBStructs;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.OracleClient;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OracleStructExporter.Core
{
    public class Exporter
    {
        // Для управления отменой задачи
        CancellationTokenSource _cancellationTokenSource;
        // Для обновления UI из фоновой задачи
        IProgress<ExportProgressData> _progressReporter;
        public event EventHandler<ExportProgressChangedEventArgs> ProgressChanged;
        OSESettings _settings;
        private string _processId;
        private List<ThreadInfo> _threadInfoList;
        private DbWorker _mainDbWorker;
        private ProgressDataManager _mainProgressManager;
        private DateTime _startDateTime;

        public string LogDBConnectionString
        {
            get
            {
                return _mainDbWorker.ConnectionString;
            }
        }

        private void OnProgressChanged(ExportProgressChangedEventArgs e)
        {
            if (ProgressChanged != null)
                ProgressChanged(this, e);
        }

        public void ReportMainProcessError(string message)
        {
            var progressDatErr =
                new ExportProgressData(ExportProgressDataLevel.ERROR,
                    ExportProgressDataStage.PROCESS_MAIN);
            progressDatErr.Error = message;
            //progressDatErr.ErrorDetails = ex.StackTrace;
            _mainProgressManager.ReportCurrentProgress(progressDatErr);
        }

        public void ReportMainProcessMessage(string message)
        {
            var progressData =
                new ExportProgressData(ExportProgressDataLevel.MOMENTALEVENTINFO,
                    ExportProgressDataStage.PROCESS_MAIN);
            progressData.SetTextAddInfo("MOMENTAL_INFO", message);
            _mainProgressManager.ReportCurrentProgress(progressData);
        }

        public Exporter()
        {
            // Создаем объект для безопасного обновления UI
            _progressReporter = new Progress<ExportProgressData>(report =>
            {
                OnProgressChanged(new ExportProgressChangedEventArgs(report));
            });
        }

        static void SaveObjectToFile(ThreadInfo thread, string objectName,
            string objectTypeSubdirName, string ddl, string fileExtension)
        {
            string fileName = $"{objectName}{fileExtension}".ToLower();
            string targetFolder;
            if (thread.ExportSettings.WriteOnlyToMainDataFolder)
            {
                targetFolder = thread.ExportSettings.PathToExportDataMain;
                if (thread.ExportSettings.UseProcessesSubFoldersInMain)
                    targetFolder = Path.Combine(targetFolder, thread.ProcessSubFolder);
            }
            else
                targetFolder = thread.ExportSettings.PathToExportDataTemp;
            string objectTypePath = Path.Combine(targetFolder, thread.DBSubfolder, thread.UserNameSubfolder, objectTypeSubdirName.ToLower());
            string fullPath = Path.Combine(objectTypePath, fileName);
            if (!Directory.Exists(objectTypePath))
                Directory.CreateDirectory(objectTypePath);

            var encodingToFile1251 = Encoding.GetEncoding(1251);
            //var encodingToFileISO = Encoding.GetEncoding("ISO-8859-1");
            using (StreamWriter writer = new StreamWriter(fullPath, false, encodingToFile1251))
            {
                // Записываем DDL объекта
                writer.WriteLine(ddl);
            }
        }

        void MoveFilesToErrorFolder(ThreadInfo thread, ProgressDataManager progressManager)
        {
            var progressData = new ExportProgressData(
                ExportProgressDataLevel.STAGESTARTINFO,
                ExportProgressDataStage.MOVE_FILES_TO_ERROR_DIR);
            progressManager.ReportCurrentProgress(progressData);
            int filesCount = 0;

            try
            {
                var sourceFolder = Path.Combine(thread.ExportSettings.PathToExportDataTemp, /*thread.ProcessSubFolder,*/ thread.DBSubfolder, thread.UserNameSubfolder); 
                var destFolder = Path.Combine(thread.ExportSettings.PathToExportDataWithErrors, thread.ProcessSubFolder, thread.DBSubfolder, thread.UserNameSubfolder);
                FilesManager.CleanDirectory(destFolder);
                filesCount = FilesManager.MoveDirectory(sourceFolder, destFolder);
            }
            catch (Exception ex)
            {
                var progressDatErr =
                    new ExportProgressData(ExportProgressDataLevel.ERROR,
                        ExportProgressDataStage.MOVE_FILES_TO_ERROR_DIR);
                progressDatErr.Error = ex.Message;
                progressDatErr.ErrorDetails = ex.StackTrace;
                progressManager.ReportCurrentProgress(progressDatErr);
            }

            var progressData2 = new ExportProgressData(
                ExportProgressDataLevel.STAGEENDINFO, ExportProgressDataStage.MOVE_FILES_TO_ERROR_DIR);
            progressData2.MetaObjCountFact = filesCount;
            progressManager.ReportCurrentProgress(progressData2);

        }

        static void MoveFilesToMainFolder(ThreadInfo thread, ProgressDataManager progressManager)
        {
            var progressData = new ExportProgressData(
                ExportProgressDataLevel.STAGESTARTINFO,
                ExportProgressDataStage.MOVE_FILES_TO_MAIN_DIR);
            progressManager.ReportCurrentProgress(progressData);
            int filesCount = 0;

            try
            {
                var sourceFolder = Path.Combine(thread.ExportSettings.PathToExportDataTemp, /*thread.ProcessSubFolder,*/ thread.DBSubfolder, thread.UserNameSubfolder);
                var destFolder = thread.ExportSettings.PathToExportDataMain;
                if (thread.ExportSettings.UseProcessesSubFoldersInMain)
                    destFolder = Path.Combine(destFolder, thread.ProcessSubFolder);
                destFolder = Path.Combine(destFolder, thread.DBSubfolder, thread.UserNameSubfolder);
                if (thread.ExportSettings.ClearMainFolderBeforeWriting)
                    FilesManager.CleanDirectory(destFolder);
                filesCount = FilesManager.MoveDirectory(sourceFolder, destFolder);
            }
            catch (Exception ex)
            {
                var progressDatErr =
                    new ExportProgressData(ExportProgressDataLevel.ERROR,
                        ExportProgressDataStage.MOVE_FILES_TO_MAIN_DIR);
                progressDatErr.Error = ex.Message;
                progressDatErr.ErrorDetails = ex.StackTrace;
                progressManager.ReportCurrentProgress(progressDatErr);
            }

            var progressData2 = new ExportProgressData(
                ExportProgressDataLevel.STAGEENDINFO, ExportProgressDataStage.MOVE_FILES_TO_MAIN_DIR);
            progressData2.MetaObjCountFact = filesCount;
            progressManager.ReportCurrentProgress(progressData2);
        }

        //static void SearchAndDeleteDuplicatesInMainFolder(ThreadInfo thread)
        //{
        //    
        //    //здесь необходимо руководствоваться флагом ClearDuplicatesInMainFolder на уровне общих настроек и перекрывающих флагов на уровне настроек Connection, а также при сравнении папок исключать из списка сравниваемых файлы из блока FilesToExcludeFromCheckingOnDoubles
        //}

        //static void CopyFilesToSimpleFileRepoFolder(ThreadInfo thread)
        //{
        //    var targetFolder = Path.Combine(thread.ExportSettings.RepoSettings.SimpleFileRepo.PathToExportDataForRepo)
        //}

        static void CreateSimpleRepoCommit(ThreadInfo thread, ProgressDataManager progressManager)
        {
            var progressData = new ExportProgressData(
                ExportProgressDataLevel.STAGESTARTINFO,
                ExportProgressDataStage.CREATE_SIMPLE_FILE_REPO_COMMIT);
            progressManager.ReportCurrentProgress(progressData);
            int changesCount = 0;
            try
            {
                var sourceFolder = thread.ExportSettings.PathToExportDataMain;
                if (thread.ExportSettings.UseProcessesSubFoldersInMain)
                    sourceFolder = Path.Combine(sourceFolder, thread.ProcessSubFolder);
                var targetFolder = thread.ExportSettings.RepoSettings.SimpleFileRepo.PathToExportDataForRepo;
                var vcsManager = new VcsManager();
                var currentRepoName = $"{thread.DBSubfolder}\\{thread.UserNameSubfolder}";
                vcsManager.CreateCommit(sourceFolder, new List<string> { currentRepoName }, targetFolder, int.Parse(thread.ProcessId), thread.StartDateTime, out changesCount);
            }
            catch (Exception ex)
            {
                var progressDatErr =
                    new ExportProgressData(ExportProgressDataLevel.ERROR,
                        ExportProgressDataStage.CREATE_SIMPLE_FILE_REPO_COMMIT);
                progressDatErr.Error = ex.Message;
                progressDatErr.ErrorDetails = ex.StackTrace;
                progressManager.ReportCurrentProgress(progressDatErr);
            }

            var progressData2 = new ExportProgressData(
                ExportProgressDataLevel.STAGEENDINFO, ExportProgressDataStage.CREATE_SIMPLE_FILE_REPO_COMMIT);
            progressData2.MetaObjCountFact = changesCount;
            progressManager.ReportCurrentProgress(progressData2);
        }

        static void CopyFilesToGitLabRepoFolder(ThreadInfo thread)
        {
            //TODO
        }

        static void CreateAndSendCommitToGitLab(ThreadInfo thread)
        {
            //TODO
        }

        static string getExtensionForObjectType(string objectType, bool packageHasHeader, bool packageHasBody)
        {
            if (objectType == "PACKAGES")
            {
                if (!packageHasHeader && packageHasBody)
                    return ".bdy";
                if (!packageHasBody && packageHasHeader)
                    return ".spc";
                return ".pck";
            }

            switch (objectType)
            {
                case "FUNCTIONS": return ".fnc";
                case "PROCEDURES": return ".prc";
                case "TRIGGERS": return ".trg";
                case "TYPES": return ".tps";
                case "VIEWS": return ".vw";
                case "SEQUENCES": return ".seq";
                case "SYNONYMS": return ".syn";
                case "TABLES": return ".tab";
                case "JOBS": return ".job";
                case "DBLINKS": return ".dblink";
                default: return ".sql";
            }
        }

        //public async void StartWork(ThreadInfo threadInfo)
        //{
        //    // Если задача уже выполняется
        //    if (_cancellationTokenSource != null)
        //        throw new Exception("Задача уже выполняется");
        //    try
        //    {
        //        _cancellationTokenSource = new CancellationTokenSource();
        //        var ct = _cancellationTokenSource.Token;
        //        await Task.Run(() => StartWork(threadInfo, ct), ct);
        //    }
        //    finally
        //    {
        //        _cancellationTokenSource?.Dispose();
        //        _cancellationTokenSource = null;
        //    }
        //}

        public void SetSettings(OSESettings settings)
        {
            _settings = settings;
            var dbLogConn = _settings.Connections.First(c =>
                c.DBIdC.ToUpper() == _settings.LogSettings.DBLog.DBLogDBId.ToUpper() && c.UserName.ToUpper() ==
                _settings.LogSettings.DBLog.DBLogUserName.ToUpper());
            var connectionString = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)" +
                                   $"(HOST={dbLogConn.Host})(PORT={dbLogConn.Port}))" +
                                   $"(CONNECT_DATA=(SID={dbLogConn.SID})));" +
                                   $"User Id={dbLogConn.UserName};Password={dbLogConn.PasswordC};";
            _mainProgressManager = new ProgressDataManager(_progressReporter, null, dbLogConn);
            _mainDbWorker = new DbWorker(connectionString, _mainProgressManager, null);
        }

        public async void StartWork(List<ThreadInfo> threadInfoList, bool isAsyncMode)
        {
            // Если задача уже выполняется
            if (_cancellationTokenSource != null)
                throw new Exception("Задача уже выполняется");
            
            _startDateTime = DateTime.Now;

            _threadInfoList = threadInfoList;


            //try
            //{
                _cancellationTokenSource = new CancellationTokenSource();
                var ct = _cancellationTokenSource.Token;

                _mainDbWorker.SetCancellationToken(ct);

            //using (OracleConnection connection = new OracleConnection(connectionString))
            //{
            //    connection.Open();

            var schemasToWork = threadInfoList.Select(c => c.Connection.UserNameAndDBIdC).ToList()
                .MergeFormatted("", ",");

            StartProcess(_startDateTime, schemasToWork/*, ct*/);
                    //_mainDbWorker.SaveNewProcessInDBLog(_startDateTime, threadInfoList.Count,
                    //    _settings.LogSettings.DBLog.DBLogPrefix, out _processId);
                //}

                //_mainProgressManager.SetProcessId(_processId);
                _threadInfoList.ForEach(c => c.ProcessId = _processId);
                foreach (var threadInfo in _threadInfoList)
                {
                    if (isAsyncMode)
                        Task.Run(() => StartWork(threadInfo, ct), ct);
                    else
                    StartWork(threadInfo, ct);
            }
            //}
            //finally
            //{
            //    _cancellationTokenSource?.Dispose();
            //    _cancellationTokenSource = null;
            //}
        }

        //public void StartWorkSync(List<ThreadInfo> threadInfoList)
        //{
        //    // Если задача уже выполняется
        //    if (_cancellationTokenSource != null)
        //        throw new Exception("Задача уже выполняется");
            
        //    _startDateTime = DateTime.Now;
        //    _threadInfoList = threadInfoList;
            
        //    _cancellationTokenSource = new CancellationTokenSource();
        //    var ct = _cancellationTokenSource.Token;

        //    _mainDbWorker.SetCancellationToken(ct);

        //    StartProcess(_startDateTime, ct);
            
            
        //    _threadInfoList.ForEach(c => c.ProcessId = _processId);
        //    foreach (var threadInfo in _threadInfoList)
        //    {
        //        StartWork(threadInfo, ct);
        //    }

        //}

        public void CancelWork()
        {
            _cancellationTokenSource?.Cancel();
        }


        void ProcessSchema(ThreadInfo threadInfo, CancellationToken ct, ProgressDataManager progressManager, out int schemaObjectsCountPlan, out int schemaObjectsCountFact)
        {
            schemaObjectsCountPlan = 0;
            schemaObjectsCountFact = 0;

            var currentObjectNumber = 0;
            //var currentObjectName = string.Empty;
            //var currentObjectTypes = string.Empty;
            bool canceledByUser;
            //var currentTime = DateTime.Now;

            List<TableOrViewComment> tablesComments = new List<TableOrViewComment>();
            List<TableOrViewComment> viewsComments = new List<TableOrViewComment>();
            List<ColumnComment> tablesAndViewsColumnsComments = new List<ColumnComment>();
            List<TableStruct> tablesStructs = new List<TableStruct>();
            List<TableColumnStruct> tablesAndViewsColumnStruct = new List<TableColumnStruct>();
            List<IndexStruct> tablesIndexes = new List<IndexStruct>();
            List<ConstraintStruct> tablesConstraints = new List<ConstraintStruct>();
            List<PartTables> partTables = new List<PartTables>();


            var exportSettingsDetails = threadInfo.ExportSettings.ExportSettingsDetails;
            var settingsConnection = threadInfo.Connection;
            var objectNameMask = exportSettingsDetails.MaskForFileNames?.Include;
            //var outputFolder = threadInfo.ExportSettings.PathToExportDataMain;
            var objectTypesToProcess = threadInfo.ExportSettings.ExportSettingsDetails.ObjectTypesToProcessC;

            string connectionString = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)" +
                                      $"(HOST={settingsConnection.Host})(PORT={settingsConnection.Port}))" +
                                      $"(CONNECT_DATA=(SID={settingsConnection.SID})));" +
                                      $"User Id={settingsConnection.UserName};Password={settingsConnection.PasswordC};";


            //var currentSchemaDescr =
            //    $"{settingsConnection.UserName}@{settingsConnection.Host}:{settingsConnection.Port}/{settingsConnection.SID}";

            //if (ct.IsCancellationRequested)
            //{
            //    progressManager.ReportCurrentProgress(ExportProgressDataLevel.CANCEL,
            //        ExportProgressDataStage.UNPLANNED_EXIT, null, 0,
            //        0, null, 0, null, null);
            //    return;
            //}

            var progressDataForSchema = new ExportProgressData(ExportProgressDataLevel.STAGESTARTINFO, ExportProgressDataStage.PROCESS_SCHEMA);
            progressManager.ReportCurrentProgress(progressDataForSchema);

            try
            {
                using (OracleConnection connection = new OracleConnection(connectionString))
                {

                    connection.Open();
                    var osb = new OracleConnectionStringBuilder(connection.ConnectionString);
                    var userId = osb.UserID.ToUpper();

                    var dbWorker = new DbWorker(connection, progressManager, objectNameMask);
                    dbWorker.SetCancellationToken(ct);

                    var systemViewsToCheck = new List<string>
                    {
                        "USER_COL_COMMENTS",
                        "USER_CONSTRAINTS",
                        "USER_CONS_COLUMNS",
                        "USER_INDEXES",
                        "USER_IND_COLUMNS",
                        "USER_IND_EXPRESSIONS",
                        "USER_PART_INDEXES",
                        "USER_PART_KEY_COLUMNS",
                        "USER_PART_TABLES",
                        "USER_SUBPART_KEY_COLUMNS",
                        "USER_TABLES",
                        "USER_TAB_COLS",
                        "USER_TAB_COMMENTS",
                        "USER_TAB_IDENTITY_COLS",
                        "USER_TAB_PARTITIONS",
                        "USER_TAB_SUBPARTITIONS"
                    };

                    var systemViewInfo = dbWorker.GetInfoAboutSystemViews(systemViewsToCheck,
                        ExportProgressDataStage.GET_INFO_ABOUT_SYS_VIEW, out canceledByUser);
                    if (canceledByUser) return;

                    dbWorker.SetSessionTransform(exportSettingsDetails.SessionTransformC);



                    List<ObjectTypeNames> namesList = dbWorker.GetObjectsNames(objectTypesToProcess,
                        ExportProgressDataStage.GET_OBJECTS_NAMES, out canceledByUser);
                    if (canceledByUser) return;

                    schemaObjectsCountPlan = namesList.Sum(c => c.ObjectNames.Count);



                    var grants = dbWorker.GetAllObjectsGrants(userId, exportSettingsDetails.SkipGrantOptionsC,
                        ExportProgressDataStage.GET_GRANTS, schemaObjectsCountPlan, out canceledByUser);
                    if (canceledByUser) return;

                    if (objectTypesToProcess.Contains("TABLES") || objectTypesToProcess.Contains("VIEWS"))
                    {
                        tablesAndViewsColumnStruct = dbWorker.GetTablesAndViewsColumnsStruct(
                            ExportProgressDataStage.GET_COLUMNS, schemaObjectsCountPlan, systemViewInfo,
                            out canceledByUser);
                        if (canceledByUser) return;

                        tablesAndViewsColumnsComments = dbWorker.GetTablesAndViewsColumnComments(
                            ExportProgressDataStage.GET_COLUMNS_COMMENTS, schemaObjectsCountPlan, out canceledByUser);
                        if (canceledByUser) return;
                    }

                    var synonymsStructs = new List<SynonymAttributes>();
                    var sequencesStructs = new List<SequenceAttributes>();
                    var schedulerJobsStructs = new List<SchedulerJob>();
                    var dbmsJobsStructs = new List<DBMSJob>();
                    var packagesHeaders = new Dictionary<string, string>();
                    var packagesBodies = new Dictionary<string, string>();
                    var functionsText = new Dictionary<string, string>();
                    var proceduresText = new Dictionary<string, string>();
                    var triggersText = new Dictionary<string, string>();
                    var typesText = new Dictionary<string, string>();
                    var viewsText = new Dictionary<string, string>();

                    if (!threadInfo.ExportSettings.WriteOnlyToMainDataFolder || threadInfo.ExportSettings.ClearMainFolderBeforeWriting)
                    {
                        var destFolder = threadInfo.ExportSettings.WriteOnlyToMainDataFolder
                            ? threadInfo.ExportSettings.PathToExportDataMain
                            : threadInfo.ExportSettings.PathToExportDataTemp;
                        if (threadInfo.ExportSettings.WriteOnlyToMainDataFolder && threadInfo.ExportSettings.UseProcessesSubFoldersInMain)
                            destFolder = Path.Combine(destFolder, threadInfo.ProcessSubFolder);
                        destFolder = Path.Combine(destFolder, threadInfo.DBSubfolder, threadInfo.UserNameSubfolder);
                        FilesManager.CleanDirectory(destFolder);
                    }


                    foreach (var objectType in objectTypesToProcess)
                    {
                        var currentType = namesList.FirstOrDefault(c => c.ObjectType == objectType);
                        var curentNamesList = currentType.ObjectNames;

                        

                        int currentTypeObjectsCounter = 0;
                        var typeObjCountPlan = curentNamesList.Count;
                        try
                        {
                            //currentObjectTypes = objectType;
                            string dbObjectType = DbWorker.GetObjectTypeName(objectType);
                            
                            var progressDataForType = new ExportProgressData(ExportProgressDataLevel.STAGESTARTINFO, ExportProgressDataStage.PROCESS_OBJECT_TYPE);
                            progressDataForType.SchemaObjCountPlan = schemaObjectsCountPlan;
                            progressDataForType.TypeObjCountPlan = typeObjCountPlan;
                            if (currentObjectNumber == 0)
                                progressDataForType.Current = null;
                            else
                                progressDataForType.Current = currentObjectNumber;

                            progressDataForType.ObjectType = objectType;
                            progressManager.ReportCurrentProgress(progressDataForType);


                            if (objectType == "SYNONYMS")
                            {
                                synonymsStructs = dbWorker.GetSynonyms(ExportProgressDataStage.GET_SYNONYMS,
                                    schemaObjectsCountPlan, typeObjCountPlan,  currentObjectNumber, objectType, out canceledByUser);
                                if (canceledByUser) return;
                            }

                            if (objectType == "SEQUENCES")
                            {
                                sequencesStructs = dbWorker.GetSequences(ExportProgressDataStage.GET_SEQUENCES,
                                    schemaObjectsCountPlan, typeObjCountPlan, currentObjectNumber, objectType, out canceledByUser);
                                if (canceledByUser) return;
                            }

                            if (objectType == "JOBS")
                            {
                                schedulerJobsStructs = dbWorker.GetSchedulerJobs(
                                    ExportProgressDataStage.GET_SCHEDULER_JOBS, schemaObjectsCountPlan, typeObjCountPlan,
                                    currentObjectNumber, objectType, out canceledByUser);
                                if (canceledByUser) return;
                                dbmsJobsStructs = dbWorker.GetDBMSJobs(ExportProgressDataStage.GET_DMBS_JOBS,
                                    schemaObjectsCountPlan, typeObjCountPlan, currentObjectNumber, objectType, out canceledByUser);
                                if (canceledByUser) return;
                            }

                            if (objectType == "PACKAGES")
                            {
                                packagesHeaders = dbWorker.GetObjectsSourceByType("PACKAGE", userId,
                                    ExportProgressDataStage.GET_PACKAGES_HEADERS, schemaObjectsCountPlan, typeObjCountPlan,
                                    currentObjectNumber, objectType, out canceledByUser);
                                if (canceledByUser) return;

                                packagesBodies =
                                    dbWorker.GetObjectsSourceByType("PACKAGE BODY", userId,
                                        ExportProgressDataStage.GET_PACKAGES_BODIES, schemaObjectsCountPlan, typeObjCountPlan,
                                        currentObjectNumber, objectType, out canceledByUser);
                                if (canceledByUser) return;
                            }

                            if (objectType == "FUNCTIONS")
                            {
                                functionsText = dbWorker.GetObjectsSourceByType(DbWorker.GetObjectTypeName(objectType),
                                    userId, ExportProgressDataStage.GET_FUNCTIONS, schemaObjectsCountPlan, typeObjCountPlan,
                                    currentObjectNumber, objectType, out canceledByUser);
                                if (canceledByUser) return;
                            }

                            if (objectType == "PROCEDURES")
                            {
                                proceduresText = dbWorker.GetObjectsSourceByType(DbWorker.GetObjectTypeName(objectType),
                                    userId, ExportProgressDataStage.GET_PROCEDURES, schemaObjectsCountPlan, typeObjCountPlan,
                                    currentObjectNumber, objectType, out canceledByUser);
                                if (canceledByUser) return;
                            }

                            if (objectType == "TRIGGERS")
                            {
                                triggersText = dbWorker.GetObjectsSourceByType(DbWorker.GetObjectTypeName(objectType),
                                    userId, ExportProgressDataStage.GET_TRIGGERS, schemaObjectsCountPlan, typeObjCountPlan,
                                    currentObjectNumber, objectType, out canceledByUser);
                                if (canceledByUser) return;
                            }

                            if (objectType == "TYPES")
                            {
                                typesText = dbWorker.GetObjectsSourceByType(DbWorker.GetObjectTypeName(objectType),
                                    userId, ExportProgressDataStage.GET_TYPES, schemaObjectsCountPlan, typeObjCountPlan,
                                    currentObjectNumber, objectType, out canceledByUser);
                                if (canceledByUser) return;
                            }

                            if (objectType == "TABLES")
                            {

                                tablesConstraints = dbWorker.GetTablesConstraints(userId,
                                    ExportProgressDataStage.GET_TABLE_CONSTRAINTS, schemaObjectsCountPlan, typeObjCountPlan,
                                    currentObjectNumber, objectType, out canceledByUser);
                                if (canceledByUser) return;

                                tablesStructs = dbWorker.GetTablesStruct(ExportProgressDataStage.GET_TABLES_STRUCTS,
                                    schemaObjectsCountPlan, typeObjCountPlan, currentObjectNumber, objectType, out canceledByUser);
                                if (canceledByUser) return;

                                tablesIndexes = dbWorker.GetTablesIndexes(ExportProgressDataStage.GET_TABLES_INDEXES,
                                    schemaObjectsCountPlan, typeObjCountPlan, currentObjectNumber, objectType, out canceledByUser);
                                if (canceledByUser) return;

                                tablesComments =
                                    dbWorker.GetTableOrViewComments(dbObjectType,
                                        ExportProgressDataStage.GET_TABLES_COMMENTS, schemaObjectsCountPlan, typeObjCountPlan,
                                        currentObjectNumber, objectType, out canceledByUser);
                                if (canceledByUser) return;

                                partTables = dbWorker.GetTablesPartitions(exportSettingsDetails.ExtractOnlyDefPart,
                                    ExportProgressDataStage.GET_TABLES_PARTS, schemaObjectsCountPlan, typeObjCountPlan,
                                    currentObjectNumber, objectType, systemViewInfo,  out canceledByUser);
                                if (canceledByUser) return;
                            }

                            if (objectType == "VIEWS")
                            {

                                viewsText = dbWorker.GetViews(ExportProgressDataStage.GET_VIEWS, schemaObjectsCountPlan, typeObjCountPlan,
                                    currentObjectNumber, objectType, out canceledByUser);
                                if (canceledByUser) return;

                                viewsComments =
                                    dbWorker.GetTableOrViewComments(dbObjectType,
                                        ExportProgressDataStage.GET_VIEWS_COMMENTS, schemaObjectsCountPlan, typeObjCountPlan,
                                        currentObjectNumber, objectType, out canceledByUser);
                                if (canceledByUser) return;
                            }

                            string objectName = string.Empty;

                            for (var i = 0; i < curentNamesList.Count; i++)
                            {
                                if (ct.IsCancellationRequested)
                                {
                                    var progressDataForTypeCancel = new ExportProgressData(ExportProgressDataLevel.CANCEL,
                                        ExportProgressDataStage.PROCESS_OBJECT_TYPE);
                                    progressDataForTypeCancel.SchemaObjCountPlan = schemaObjectsCountPlan;
                                    progressDataForTypeCancel.TypeObjCountPlan = curentNamesList.Count;
                                    progressDataForTypeCancel.TypeObjCountFact = currentTypeObjectsCounter;
                                    progressDataForTypeCancel.Current = currentObjectNumber;
                                    progressDataForTypeCancel.ObjectType = objectType;
                                    progressManager.ReportCurrentProgress(progressDataForTypeCancel);
                                    break;
                                }

                                objectName = curentNamesList[i];
                                currentObjectNumber++;
                                currentTypeObjectsCounter++;
                                string ddl = string.Empty;
                                string ddlPackageBody = string.Empty;
                                string ddlPackageHead = string.Empty;
                                //currentObjectName = objectName;

                                bool currentObjIsSchedulerJob = false;
                                bool currentObjIsDBMSJob = false;


                                try
                                {


                                    var progressDataForObject = new ExportProgressData(
                                        ExportProgressDataLevel.STAGESTARTINFO,
                                        ExportProgressDataStage.PROCESS_OBJECT);
                                    progressDataForObject.SchemaObjCountPlan = schemaObjectsCountPlan;
                                    progressDataForObject.TypeObjCountPlan = curentNamesList.Count;
                                    progressDataForObject.Current = currentObjectNumber;
                                    progressDataForObject.ObjectType = objectType;
                                    progressDataForObject.ObjectName = objectName;
                                    progressManager.ReportCurrentProgress(progressDataForObject);

                                    if (objectType == "PACKAGES")
                                    {
                                        ddlPackageHead = DDLCreator.GetObjectDdlForPackageHeader(packagesHeaders,
                                            objectName,
                                            exportSettingsDetails.AddSlashToC);
                                        ddlPackageBody = DDLCreator.GetObjectDdlForPackageBody(packagesBodies,
                                            objectName,
                                            exportSettingsDetails.AddSlashToC);
                                    }
                                    else if (objectType == "FUNCTIONS")
                                    {
                                        ddl = DDLCreator.GetObjectDdlForSourceText(functionsText, objectName,
                                            objectType,
                                            exportSettingsDetails.AddSlashToC);
                                    }
                                    else if (objectType == "PROCEDURES")
                                    {
                                        ddl = DDLCreator.GetObjectDdlForSourceText(proceduresText, objectName,
                                            objectType,
                                            exportSettingsDetails.AddSlashToC);
                                    }
                                    else if (objectType == "TRIGGERS")
                                    {
                                        ddl = DDLCreator.GetObjectDdlForSourceText(triggersText, objectName,
                                            objectType,
                                            exportSettingsDetails.AddSlashToC);
                                    }
                                    else if (objectType == "TYPES")
                                    {
                                        ddl = DDLCreator.GetObjectDdlForSourceText(typesText, objectName,
                                            objectType,
                                            exportSettingsDetails.AddSlashToC);
                                    }
                                    else if (objectType == "SYNONYMS")
                                    {
                                        ddl = DDLCreator.GetObjectDdlForSynonym(synonymsStructs, objectName,
                                            exportSettingsDetails.AddSlashToC);
                                    }
                                    else if (objectType == "SEQUENCES")
                                    {
                                        ddl = DDLCreator.GetObjectDdlForSequence(sequencesStructs, objectName,
                                            exportSettingsDetails.SetSequencesValuesTo1,
                                            exportSettingsDetails.AddSlashToC);
                                    }
                                    else if (objectType == "VIEWS")
                                    {
                                        ddl = DDLCreator.GetObjectDdlForView(viewsText, tablesAndViewsColumnStruct,
                                            viewsComments,
                                            tablesAndViewsColumnsComments, objectName,
                                            exportSettingsDetails.AddSlashToC);
                                    }
                                    else if (objectType == "TABLES")
                                    {

                                        ddl = DDLCreator.GetObjectDdlForTable(tablesStructs,
                                            tablesAndViewsColumnStruct,
                                            tablesConstraints,
                                            tablesIndexes, tablesComments, tablesAndViewsColumnsComments,
                                            partTables,
                                            objectName, userId, exportSettingsDetails.AddSlashToC);
                                    }
                                    else if (objectType == "JOBS")
                                    {
                                        ddl = DDLCreator.GetObjectDdlForSchedulerJob(schedulerJobsStructs,
                                            objectName,
                                            exportSettingsDetails.AddSlashToC);
                                        if (!string.IsNullOrWhiteSpace(ddl))
                                            currentObjIsSchedulerJob = true;
                                        else
                                        {
                                            ddl = DDLCreator.GetObjectDdlForDBMSJob(dbmsJobsStructs, objectName,
                                                exportSettingsDetails.AddSlashToC);
                                            currentObjIsDBMSJob = true;
                                        }
                                    }
                                    else if (objectType == "DBLINKS")
                                    {
                                        ddl = dbWorker.GetObjectDdl(objectType, objectName,
                                            exportSettingsDetails.SetSequencesValuesTo1,
                                            exportSettingsDetails.AddSlashToC, ExportProgressDataStage.GET_DBLINK,
                                            schemaObjectsCountPlan, typeObjCountPlan, currentObjectNumber, 
                                            out canceledByUser);
                                        if (canceledByUser) return;
                                    }
                                    else
                                    {
                                        //сюда не должны зайти, но оставим на всякий случай
                                        var objectSource = dbWorker.GetObjectSource(objectName, objectType,
                                            exportSettingsDetails.AddSlashToC,
                                            ExportProgressDataStage.GET_UNKNOWN_OBJECT_DDL, schemaObjectsCountPlan,
                                            typeObjCountPlan,
                                            currentObjectNumber, out canceledByUser);
                                        if (canceledByUser) return;
                                        ddl = DDLCreator.AddCreateOrReplace(objectSource);
                                    }

                                    string objectGrants = DDLCreator.GetObjectGrants(grants, objectName,
                                        exportSettingsDetails.OrderGrantOptionsC);
                                    if (objectType != "PACKAGES")
                                    {
                                        ddl = DDLCreator.GetDDlWithGrants(ddl, objectGrants);
                                    }
                                    else
                                    {
                                        ddlPackageHead = DDLCreator.GetDDlWithGrants(ddlPackageHead, objectGrants);
                                        ddlPackageBody = DDLCreator.GetDDlWithGrants(ddlPackageBody, objectGrants);
                                        ddl = DDLCreator.MergeHeadAndBody(ddlPackageHead, ddlPackageBody);
                                    }

                                    if (string.IsNullOrWhiteSpace(ddl))
                                        throw new Exception($"Для объекта {objectName} не удалось получить ddl");

                                    string extension = getExtensionForObjectType(objectType,
                                        !string.IsNullOrWhiteSpace(ddlPackageHead),
                                        !string.IsNullOrWhiteSpace(ddlPackageBody));
                                    var objectTypeSubdirName = objectType;
                                    if (objectType == "JOBS")
                                    {
                                        if (currentObjIsSchedulerJob)
                                            objectTypeSubdirName = "scheduler_jobs";
                                        else if (currentObjIsDBMSJob)
                                            objectTypeSubdirName = "dbms_jobs";
                                    }

                                    SaveObjectToFile(threadInfo, objectName, objectTypeSubdirName, ddl, extension);



                                    //currentObjectName = string.Empty;

                                }
                                catch (Exception ex)
                                {
                                    var progressDataForObjectErr =
                                        new ExportProgressData(ExportProgressDataLevel.ERROR,
                                            ExportProgressDataStage.PROCESS_OBJECT);
                                    progressDataForObjectErr.Error = ex.Message;
                                    progressDataForObjectErr.ErrorDetails = ex.StackTrace;
                                    progressDataForObjectErr.SchemaObjCountPlan = schemaObjectsCountPlan;
                                    progressDataForObjectErr.TypeObjCountPlan = curentNamesList.Count;
                                    progressDataForObjectErr.Current = currentObjectNumber;
                                    progressDataForObjectErr.ObjectType = objectType;
                                    progressDataForObjectErr.ObjectName = objectName;
                                    progressManager.ReportCurrentProgress(progressDataForObjectErr);

                                    currentObjectNumber--;
                                    currentTypeObjectsCounter--;
                                }

                                var progressDataForObject2 = new ExportProgressData(
                                    ExportProgressDataLevel.STAGEENDINFO, ExportProgressDataStage.PROCESS_OBJECT);
                                progressDataForObject2.SchemaObjCountPlan = schemaObjectsCountPlan;
                                progressDataForObject2.TypeObjCountPlan = curentNamesList.Count;
                                progressDataForObject2.Current = currentObjectNumber;
                                progressDataForObject2.ObjectType = objectType;
                                progressDataForObject2.ObjectName = objectName;
                                progressManager.ReportCurrentProgress(progressDataForObject2);
                            }



                        }
                        catch (Exception ex)
                        {
                            var progressDataErr = new ExportProgressData(ExportProgressDataLevel.ERROR, ExportProgressDataStage.PROCESS_OBJECT_TYPE);
                            progressDataErr.Error = ex.Message;
                            progressDataErr.ErrorDetails = ex.StackTrace;
                            progressDataErr.SchemaObjCountPlan = schemaObjectsCountPlan;
                            progressDataErr.TypeObjCountPlan = curentNamesList.Count;
                            progressDataErr.TypeObjCountFact = currentTypeObjectsCounter;
                            progressDataErr.Current = currentObjectNumber;
                            progressDataErr.ObjectType = objectType;
                            progressManager.ReportCurrentProgress(progressDataErr);
                        }

                        var progressDataForType2 = new ExportProgressData(ExportProgressDataLevel.STAGEENDINFO, ExportProgressDataStage.PROCESS_OBJECT_TYPE);
                        progressDataForType2.SchemaObjCountPlan = schemaObjectsCountPlan;
                        progressDataForType2.TypeObjCountPlan = curentNamesList.Count;
                        progressDataForType2.TypeObjCountFact = currentTypeObjectsCounter;
                        progressDataForType2.Current = currentObjectNumber;
                        progressDataForType2.ObjectType = objectType;
                        progressManager.ReportCurrentProgress(progressDataForType2);

                        schemaObjectsCountFact += currentTypeObjectsCounter;

                        if (ct.IsCancellationRequested)
                        {
                            var progressDataCancel = new ExportProgressData(ExportProgressDataLevel.CANCEL,
                                ExportProgressDataStage.PROCESS_SCHEMA);
                            progressDataCancel.SchemaObjCountPlan = schemaObjectsCountPlan;
                            progressDataCancel.SchemaObjCountFact = schemaObjectsCountFact;
                            progressManager.ReportCurrentProgress(progressDataCancel);
                            break;
                        }
                    }
                }

                if (!threadInfo.ExportSettings.WriteOnlyToMainDataFolder)
                {
                    if (progressManager.CurrentThreadErrorsCount > 0)
                        MoveFilesToErrorFolder(threadInfo, progressManager);
                    else
                    {
                        MoveFilesToMainFolder(threadInfo,progressManager);
                        //SearchAndDeleteDuplicatesInMainFolder(threadInfo);
                    }
                }

                if (threadInfo.ExportSettings.RepoSettings != null)
                {
                    if (threadInfo.ExportSettings.RepoSettings.SimpleFileRepo != null &&
                        threadInfo.ExportSettings.RepoSettings.SimpleFileRepo.CommitToRepoAfterSuccess &&
                        progressManager.CurrentThreadErrorsCount == 0)
                    {
                        CreateSimpleRepoCommit(threadInfo, progressManager);
                    }

                    if (threadInfo.ExportSettings.RepoSettings.GitLabRepo != null &&
                        threadInfo.ExportSettings.RepoSettings.GitLabRepo.CommitToRepoAfterSuccess &&
                        progressManager.CurrentThreadErrorsCount == 0)
                    {
                        //TODO копирование сформированных данным экспортом файлов из папки PathToExportDataMain в папку PathToExportDataForRepo (если задано настройками)
                        CopyFilesToGitLabRepoFolder(threadInfo);
                        //TODO создание коммита
                        CreateAndSendCommitToGitLab(threadInfo);
                    }
                }

            }
            catch (Exception ex)
            {
                var progressDataErr = new ExportProgressData(ExportProgressDataLevel.ERROR, ExportProgressDataStage.PROCESS_SCHEMA);
                progressDataErr.Error = ex.Message;
                progressDataErr.ErrorDetails = ex.StackTrace;
                progressDataErr.SchemaObjCountPlan = schemaObjectsCountPlan;
                progressDataErr.SchemaObjCountFact = schemaObjectsCountFact;
                progressManager.ReportCurrentProgress(progressDataErr);
            }
        }


        public async void StartWork(ThreadInfo threadInfo, CancellationToken ct)
        {
            var progressManager =
                new ProgressDataManager(_progressReporter, threadInfo.ProcessId, threadInfo.Connection);

            _mainProgressManager.ChildProgressManagers.Add(progressManager);

            int schemaObjectsCountPlan;
            int schemaObjectsCountFact;
            ProcessSchema(threadInfo, ct, progressManager, out schemaObjectsCountPlan, out schemaObjectsCountFact);

            threadInfo.Finished = true;

            var progressData = new ExportProgressData(ExportProgressDataLevel.STAGEENDINFO, ExportProgressDataStage.PROCESS_SCHEMA);
            progressData.SchemaObjCountPlan = schemaObjectsCountPlan;
            progressData.SchemaObjCountFact = schemaObjectsCountFact;
            progressData.Current = schemaObjectsCountFact;
            progressData.ErrorsCount = progressManager.CurrentThreadErrorsCount;
            progressManager.ReportCurrentProgress(progressData);

            if (_threadInfoList.All(c => c.Finished))
            {
                //if (_settings.LogSettings.DBLog.Enabled)
                //{

                var schemasSuccess = _mainProgressManager.ChildProgressManagers.Where(c => c.AllErrorsCount == 0)
                    .Select(c => c.Connection.UserNameAndDBIdC).ToList().MergeFormatted("", ",");
                var schemasWithErrors = _mainProgressManager.ChildProgressManagers.Where(c => c.AllErrorsCount > 0)
                    .Select(c => c.Connection.UserNameAndDBIdC).ToList().MergeFormatted("", ",");

                var progressDataProcMain = new ExportProgressData(ExportProgressDataLevel.STAGEENDINFO, ExportProgressDataStage.PROCESS_MAIN);
                progressDataProcMain.ProcessObjCountPlan = _mainProgressManager.ChildThreadsSchemaObjCountPlan;
                progressDataProcMain.ProcessObjCountFact = _mainProgressManager.ChildThreadsSchemaObjCountFact;
                progressDataProcMain.ErrorsCount = _mainProgressManager.AllErrorsCount;
                progressDataProcMain.SetTextAddInfo("SCHEMAS_SUCCESS", schemasSuccess);
                progressDataProcMain.SetTextAddInfo("SCHEMAS_ERROR", schemasWithErrors);
                _mainProgressManager.ReportCurrentProgress(progressDataProcMain);
                EndProcess(_settings.LogSettings.DBLog.DBLogPrefix, progressDataProcMain);
                //}
            }
        }

        public void StartProcess(DateTime currentDateTime, string schemasToWork/*, CancellationToken ct*/)
        {

            //if (ct.IsCancellationRequested)
            //{
            //    _mainProgressManager.ReportCurrentProgress(ExportProgressDataLevel.CANCEL,
            //        ExportProgressDataStage.UNPLANNED_EXIT, null, 0,
            //        0, null, 0, null, null);
            //    return;
            //}
            

            if (_settings.LogSettings.DBLog.Enabled)
            {
                _mainDbWorker.SaveNewProcessInDBLog(currentDateTime, _threadInfoList.Count, _settings.LogSettings.DBLog.DBLogPrefix, out _processId);
            }
            else
            {
                //TODO пробуем работать с processId в файле
            }

            _mainProgressManager.SetProcessId(_processId);

            var progressData = new ExportProgressData(ExportProgressDataLevel.STAGESTARTINFO, ExportProgressDataStage.PROCESS_MAIN);
            progressData.SetTextAddInfo("SCHEMAS_TO_WORK", schemasToWork);
            _mainProgressManager.ReportCurrentProgress(progressData);
        }

        public void EndProcess(string dbLogPrefix, ExportProgressData progressData)
        {
            if (_settings.LogSettings.DBLog.Enabled)
            {
                _mainDbWorker.UpdateProcessInDBLog(DateTime.Now, dbLogPrefix, progressData);
            }
            else
            {
                //TODO пробуем работать с processId в файле
            }

        }

        public DateTime? GetLastSuccessExportForSchema(string dbidC, string username)
        {
            return _mainDbWorker.GetLastSuccessExportForSchema(dbidC, username);
        }
    }
}
