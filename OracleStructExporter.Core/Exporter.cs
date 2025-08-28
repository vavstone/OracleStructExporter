using OracleStructExporter.Core.DBStructs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OracleClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
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

        private void OnProgressChanged(ExportProgressChangedEventArgs e)
        {
            if (ProgressChanged != null)
                ProgressChanged(this, e);
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
            var mainFolderPath = thread.ExportSettings.WriteOnlyToMainDataFolder
                ? thread.ExportSettings.PathToExportDataMain
                : thread.ExportSettings.PathToExportDataTemp;
            var processSubfolder = thread.ExportSettings.UseProcessesSubFolders
                ? $"{thread.StartDateTime.ToString("yyyy-MM-dd")}_{thread.ProcessId}"
                : string.Empty;
            var dbSubfolder = thread.Connection.DBIdCForFileSystem.ToUpper();
            var userNameSubfolder = thread.Connection.UserName.ToUpper();
            string objectTypePath = Path.Combine(mainFolderPath, processSubfolder, dbSubfolder, userNameSubfolder, objectTypeSubdirName.ToLower());
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

        static void MoveFilesToErrorFolder(ThreadInfo thread)
        {
            //TODO
        }

        static void MoveFilesToMainFolder(ThreadInfo thread)
        {
            //TODO
        }

        static void SearchAndDeleteDuplicatesInMainFolder(ThreadInfo thread)
        {
            //TODO
            //здесь необходимо руководствоваться флагом ClearDuplicatesInMainFolder на уровне общих настроек и перекрывающих флагов на уровне настроек Connection, а также при сравнении папок исключать из списка сравниваемых файлы из блока FilesToExcludeFromCheckingOnDoubles
        }

        static void CopyFilesToRepoFolder(ThreadInfo thread)
        {
            //TODO
        }

        static void CreateAndSendCommitToGit(ThreadInfo thread)
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

        public async void StartWork(OSESettings settings, List<ThreadInfo> threadInfoList)
        {
            // Если задача уже выполняется
            if (_cancellationTokenSource != null)
                throw new Exception("Задача уже выполняется");
            _startDateTime = DateTime.Now;
            _settings = settings;
            _threadInfoList = threadInfoList;


            


            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                var ct = _cancellationTokenSource.Token;

                var dbLogConn = _settings.Connections.First(c =>
                    c.DBIdC.ToUpper() == _settings.LogSettings.DBLog.DBLogDBId.ToUpper() && c.UserName.ToUpper() ==
                    _settings.LogSettings.DBLog.DBLogUserName.ToUpper());
                var connectionString = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)" +
                                          $"(HOST={dbLogConn.Host})(PORT={dbLogConn.Port}))" +
                                          $"(CONNECT_DATA=(SID={dbLogConn.SID})));" +
                                          $"User Id={dbLogConn.UserName};Password={dbLogConn.PasswordC};";
                //using (OracleConnection connection = new OracleConnection(connectionString))
                //{
                //    connection.Open();
                    _mainProgressManager = new ProgressDataManager(_progressReporter, null, dbLogConn);
                    _mainDbWorker = new DbWorker(connectionString, _mainProgressManager, null, ct);

                    StartProcess(_startDateTime, ct);
                    //_mainDbWorker.SaveNewProcessInDBLog(_startDateTime, threadInfoList.Count,
                    //    _settings.LogSettings.DBLog.DBLogPrefix, out _processId);
                //}

                _mainProgressManager.SetProcessId(_processId);
                threadInfoList.ForEach(c => c.ProcessId = _processId);
                foreach (var threadInfo in threadInfoList)
                {
                    Task.Run(() => StartWork(threadInfo, ct), ct);
                }
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        public void CancelWork()
        {
            _cancellationTokenSource?.Cancel();
        }


        public async void StartWork(ThreadInfo threadInfo, CancellationToken ct)
        {

            var totalObjectsToProcess = 0;
            var currentObjectNumber = 0;
            var currentObjectName = string.Empty;
            var currentObjectTypes = string.Empty;
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

            var progressManager =
                new ProgressDataManager(_progressReporter, threadInfo.ProcessId, threadInfo.Connection);
            var exportSettingsDetails = threadInfo.ExportSettings.ExportSettingsDetails;
            var settingsConnection = threadInfo.Connection;
            var objectNameMask = exportSettingsDetails.MaskForFileNames?.Include;
            var outputFolder = threadInfo.ExportSettings.PathToExportDataMain;
            var objectTypesToProcess = threadInfo.ExportSettings.ExportSettingsDetails.ObjectTypesToProcessC;

            string connectionString = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)" +
                                      $"(HOST={settingsConnection.Host})(PORT={settingsConnection.Port}))" +
                                      $"(CONNECT_DATA=(SID={settingsConnection.SID})));" +
                                      $"User Id={settingsConnection.UserName};Password={settingsConnection.PasswordC};";

            var currentSchemaDescr =
                $"{settingsConnection.UserName}@{settingsConnection.Host}:{settingsConnection.Port}/{settingsConnection.SID}";

            if (ct.IsCancellationRequested)
            {
                progressManager.ReportCurrentProgress(ExportProgressDataLevel.CANCEL,
                    ExportProgressDataStage.UNPLANNED_EXIT, null, 0,
                    0,  null, 0, null, null);
                return;
            }

            progressManager.ReportCurrentProgress(ExportProgressDataLevel.STAGESTARTINFO,
                ExportProgressDataStage.PROCESS_SCHEMA, currentObjectName, currentObjectNumber,
                totalObjectsToProcess,  currentSchemaDescr, 0, null, null);


            try
            {
                using (OracleConnection connection = new OracleConnection(connectionString))
                {

                    connection.Open();
                    var osb = new OracleConnectionStringBuilder(connection.ConnectionString);
                    var userId = osb.UserID.ToUpper();

                    var dbWorker = new DbWorker(connection, progressManager, objectNameMask, ct);

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
                        ExportProgressDataStage.GET_INFO_ABOUT_SYS_VIEW, totalObjectsToProcess, out canceledByUser);
                    if (canceledByUser) return;

                    dbWorker.SetSessionTransform(exportSettingsDetails.SessionTransformC);



                    List<ObjectTypeNames> namesList = dbWorker.GetObjectsNames(objectTypesToProcess,
                        ExportProgressDataStage.GET_OBJECTS_NAMES, out canceledByUser);
                    if (canceledByUser) return;

                    totalObjectsToProcess = namesList.Sum(c => c.ObjectNames.Count);



                    var grants = dbWorker.GetAllObjectsGrants(userId, exportSettingsDetails.SkipGrantOptionsC,
                        ExportProgressDataStage.GET_GRANTS, totalObjectsToProcess, out canceledByUser);
                    if (canceledByUser) return;

                    if (objectTypesToProcess.Contains("TABLES") || objectTypesToProcess.Contains("VIEWS"))
                    {


                        tablesAndViewsColumnStruct = dbWorker.GetTablesAndViewsColumnsStruct(
                            ExportProgressDataStage.GET_COLUMNS, totalObjectsToProcess, systemViewInfo,
                            out canceledByUser);
                        if (canceledByUser) return;

                        tablesAndViewsColumnsComments = dbWorker.GetTablesAndViewsColumnComments(
                            ExportProgressDataStage.GET_COLUMNS_COMMENTS, totalObjectsToProcess, out canceledByUser);
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


                    foreach (var objectType in objectTypesToProcess)
                    {
                        try
                        {
                            currentObjectTypes = objectType;
                            string dbObjectType = DbWorker.GetObjectTypeName(objectType);

                            progressManager.ReportCurrentProgress(ExportProgressDataLevel.STAGESTARTINFO,
                                ExportProgressDataStage.PROCESS_OBJECT_TYPE, currentObjectName, currentObjectNumber,
                                totalObjectsToProcess,  currentObjectTypes, 0, null, null);

                            if (objectType == "SYNONYMS")
                            {
                                synonymsStructs = dbWorker.GetSynonyms(ExportProgressDataStage.GET_SYNONYMS,
                                    totalObjectsToProcess, currentObjectNumber, out canceledByUser);
                                if (canceledByUser) return;
                            }

                            if (objectType == "SEQUENCES")
                            {
                                sequencesStructs = dbWorker.GetSequences(ExportProgressDataStage.GET_SEQUENCES,
                                    totalObjectsToProcess, currentObjectNumber, out canceledByUser);
                                if (canceledByUser) return;
                            }

                            if (objectType == "JOBS")
                            {
                                schedulerJobsStructs = dbWorker.GetSchedulerJobs(
                                    ExportProgressDataStage.GET_SCHEDULER_JOBS, totalObjectsToProcess,
                                    currentObjectNumber, out canceledByUser);
                                if (canceledByUser) return;
                                dbmsJobsStructs = dbWorker.GetDBMSJobs(ExportProgressDataStage.GET_DMBS_JOBS,
                                    totalObjectsToProcess, currentObjectNumber, out canceledByUser);
                                if (canceledByUser) return;
                            }

                            if (objectType == "PACKAGES")
                            {
                                packagesHeaders = dbWorker.GetObjectsSourceByType("PACKAGE", userId,
                                    ExportProgressDataStage.GET_PACKAGES_HEADERS, totalObjectsToProcess,
                                    currentObjectNumber, out canceledByUser);
                                if (canceledByUser) return;

                                packagesBodies =
                                    dbWorker.GetObjectsSourceByType("PACKAGE BODY", userId,
                                        ExportProgressDataStage.GET_PACKAGES_BODIES, totalObjectsToProcess,
                                        currentObjectNumber, out canceledByUser);
                                if (canceledByUser) return;
                            }

                            if (objectType == "FUNCTIONS")
                            {
                                functionsText = dbWorker.GetObjectsSourceByType(DbWorker.GetObjectTypeName(objectType),
                                    userId, ExportProgressDataStage.GET_FUNCTIONS, totalObjectsToProcess,
                                    currentObjectNumber, out canceledByUser);
                                if (canceledByUser) return;
                            }

                            if (objectType == "PROCEDURES")
                            {
                                proceduresText = dbWorker.GetObjectsSourceByType(DbWorker.GetObjectTypeName(objectType),
                                    userId, ExportProgressDataStage.GET_PROCEDURES, totalObjectsToProcess,
                                    currentObjectNumber, out canceledByUser);
                                if (canceledByUser) return;
                            }

                            if (objectType == "TRIGGERS")
                            {
                                triggersText = dbWorker.GetObjectsSourceByType(DbWorker.GetObjectTypeName(objectType),
                                    userId, ExportProgressDataStage.GET_TRIGGERS, totalObjectsToProcess,
                                    currentObjectNumber, out canceledByUser);
                                if (canceledByUser) return;
                            }

                            if (objectType == "TYPES")
                            {
                                typesText = dbWorker.GetObjectsSourceByType(DbWorker.GetObjectTypeName(objectType),
                                    userId, ExportProgressDataStage.GET_TYPES, totalObjectsToProcess,
                                    currentObjectNumber, out canceledByUser);
                                if (canceledByUser) return;
                            }

                            if (objectType == "TABLES")
                            {

                                tablesConstraints = dbWorker.GetTablesConstraints(userId,
                                    ExportProgressDataStage.GET_TABLE_CONSTRAINTS, totalObjectsToProcess,
                                    currentObjectNumber, out canceledByUser);
                                if (canceledByUser) return;

                                tablesStructs = dbWorker.GetTablesStruct(ExportProgressDataStage.GET_TABLES_STRUCTS,
                                    totalObjectsToProcess, currentObjectNumber, out canceledByUser);
                                if (canceledByUser) return;

                                tablesIndexes = dbWorker.GetTablesIndexes(ExportProgressDataStage.GET_TABLES_INDEXES,
                                    totalObjectsToProcess, currentObjectNumber, out canceledByUser);
                                if (canceledByUser) return;

                                tablesComments =
                                    dbWorker.GetTableOrViewComments(dbObjectType,
                                        ExportProgressDataStage.GET_TABLES_COMMENTS, totalObjectsToProcess,
                                        currentObjectNumber, out canceledByUser);
                                if (canceledByUser) return;

                                partTables = dbWorker.GetTablesPartitions(exportSettingsDetails.ExtractOnlyDefPart,
                                    ExportProgressDataStage.GET_TABLES_PARTS, totalObjectsToProcess,
                                    currentObjectNumber, out canceledByUser);
                                if (canceledByUser) return;
                            }

                            if (objectType == "VIEWS")
                            {

                                viewsText = dbWorker.GetViews(ExportProgressDataStage.GET_VIEWS, totalObjectsToProcess,
                                    currentObjectNumber, out canceledByUser);
                                if (canceledByUser) return;

                                viewsComments =
                                    dbWorker.GetTableOrViewComments(dbObjectType,
                                        ExportProgressDataStage.GET_VIEWS_COMMENTS, totalObjectsToProcess,
                                        currentObjectNumber, out canceledByUser);
                                if (canceledByUser) return;
                            }

                            var currentType = namesList.FirstOrDefault(c => c.ObjectType == objectType);
                            var curentNamesList = currentType.ObjectNames;
                            int currentTypeObjectsCounter = 0;
                            for (var i = 0; i < curentNamesList.Count; i++)
                            {

                                var objectName = curentNamesList[i];
                                currentObjectNumber++;
                                currentTypeObjectsCounter++;
                                string ddl = string.Empty;
                                string ddlPackageBody = string.Empty;
                                string ddlPackageHead = string.Empty;
                                currentObjectName = objectName;

                                bool currentObjIsSchedulerJob = false;
                                bool currentObjIsDBMSJob = false;

                                try
                                {
                                    if (ct.IsCancellationRequested)
                                    {
                                        progressManager.ReportCurrentProgress(ExportProgressDataLevel.CANCEL,
                                            ExportProgressDataStage.UNPLANNED_EXIT, currentObjectName,
                                            currentObjectNumber,
                                            totalObjectsToProcess,  null, 0, null, null);
                                        return;
                                    }

                                    progressManager.ReportCurrentProgress(ExportProgressDataLevel.STAGESTARTINFO,
                                        ExportProgressDataStage.PROCESS_OBJECT, currentObjectName, currentObjectNumber,
                                        totalObjectsToProcess, null, 0, null, null);
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
                                        ddl = DDLCreator.GetObjectDdlForSourceText(triggersText, objectName, objectType,
                                            exportSettingsDetails.AddSlashToC);
                                    }
                                    else if (objectType == "TYPES")
                                    {
                                        ddl = DDLCreator.GetObjectDdlForSourceText(typesText, objectName, objectType,
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

                                        ddl = DDLCreator.GetObjectDdlForTable(tablesStructs, tablesAndViewsColumnStruct,
                                            tablesConstraints,
                                            tablesIndexes, tablesComments, tablesAndViewsColumnsComments, partTables,
                                            objectName, userId, exportSettingsDetails.AddSlashToC);
                                    }
                                    else if (objectType == "JOBS")
                                    {
                                        ddl = DDLCreator.GetObjectDdlForSchedulerJob(schedulerJobsStructs, objectName,
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
                                            totalObjectsToProcess, currentObjectNumber, out canceledByUser);
                                        if (canceledByUser) return;
                                    }
                                    else
                                    {
                                        //сюда не должны зайти, но оставим на всякий случай
                                        var objectSource = dbWorker.GetObjectSource(objectName, objectType,
                                            exportSettingsDetails.AddSlashToC,
                                            ExportProgressDataStage.GET_UNKNOWN_OBJECT_DDL, totalObjectsToProcess,
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

                                    progressManager.ReportCurrentProgress(ExportProgressDataLevel.STAGEENDINFO,
                                        ExportProgressDataStage.PROCESS_OBJECT, currentObjectName, currentObjectNumber,
                                        totalObjectsToProcess,  null, 0, null, null);

                                    currentObjectName = string.Empty;

                                }
                                catch (Exception ex)
                                {
                                    progressManager.ReportCurrentProgress(ExportProgressDataLevel.ERROR,
                                        ExportProgressDataStage.UNPLANNED_EXIT, currentObjectName, currentObjectNumber,
                                        totalObjectsToProcess,  null, 0, ex.Message, ex.StackTrace);
                                    currentObjectNumber--;
                                    currentTypeObjectsCounter--;
                                }
                            }

                            progressManager.ReportCurrentProgress(ExportProgressDataLevel.STAGEENDINFO,
                                ExportProgressDataStage.PROCESS_OBJECT_TYPE, currentObjectName, currentObjectNumber,
                                totalObjectsToProcess,  currentObjectTypes, currentTypeObjectsCounter, null,
                                null);
                        }
                        catch (Exception ex)
                        {
                            progressManager.ReportCurrentProgress(ExportProgressDataLevel.ERROR,
                                ExportProgressDataStage.UNPLANNED_EXIT, currentObjectName, currentObjectNumber,
                                totalObjectsToProcess,  null, 0, ex.Message, ex.StackTrace);
                        }
                    }
                }

                if (!threadInfo.ExportSettings.WriteOnlyToMainDataFolder)
                {
                    //TODO перемещение сформированных данным экспортом файлов из папки PathToExportDataTemp в папку PathToExportDataMain или PathToExportDataTemp (если задано настройками)
                    if (progressManager.ErrorsCount > 0)
                        MoveFilesToErrorFolder(threadInfo);
                    else
                    {
                        MoveFilesToMainFolder(threadInfo);
                        SearchAndDeleteDuplicatesInMainFolder(threadInfo);
                    }
                }

                if (threadInfo.ExportSettings.RepoSettings != null &&
                    threadInfo.ExportSettings.RepoSettings.CommitToRepoAfterSuccess)
                {
                    if (progressManager.ErrorsCount == 0)
                    {
                        //TODO копирование сформированных данным экспортом файлов из папки PathToExportDataMain в папку PathToExportDataForRepo (если задано настройками)
                        CopyFilesToRepoFolder(threadInfo);
                        //TODO создание и отправка коммита в гит
                        CreateAndSendCommitToGit(threadInfo);
                    }
                }



            }
            catch (Exception ex)
            {
                progressManager.ReportCurrentProgress(ExportProgressDataLevel.ERROR,
                    ExportProgressDataStage.UNPLANNED_EXIT, currentObjectName, currentObjectNumber,
                    totalObjectsToProcess, null, 0, ex.Message, ex.StackTrace);
            }




            threadInfo.Finished = true;
            
            progressManager.ReportCurrentProgress(ExportProgressDataLevel.STAGEENDINFO,
                ExportProgressDataStage.PROCESS_SCHEMA, currentObjectName, currentObjectNumber,
                totalObjectsToProcess,  currentSchemaDescr, currentObjectNumber, null, null);

            if (_threadInfoList.All(c => c.Finished))
            {
                //if (_settings.LogSettings.DBLog.Enabled)
                //{
                EndProcess(_settings.LogSettings.DBLog.DBLogPrefix, _processId);
                //}
            }
        }

        public void StartProcess(DateTime currentDateTime, CancellationToken ct)
        {

            if (ct.IsCancellationRequested)
            {
                _mainProgressManager.ReportCurrentProgress(ExportProgressDataLevel.CANCEL,
                    ExportProgressDataStage.UNPLANNED_EXIT, null, 0,
                    0, null, 0, null, null);
                return;
            }
            _mainProgressManager.ReportCurrentProgress(ExportProgressDataLevel.STAGESTARTINFO,
                ExportProgressDataStage.PROCESS_MAIN, null, 0,
                0, null, 0, null, null);
            if (_settings.LogSettings.DBLog.Enabled)
            {
                _mainDbWorker.SaveNewProcessInDBLog(currentDateTime, _threadInfoList.Count, _settings.LogSettings.DBLog.DBLogPrefix, out _processId);
            }
            else
            {
                //пробуем работать с processId в файле
            }
            
        }

        public void EndProcess(string dbLogPrefix, string processId)
        {
            if (_settings.LogSettings.DBLog.Enabled)
            {
                _mainDbWorker.UpdateProcessInDBLog(DateTime.Now, dbLogPrefix, processId);
            }
            else
            {
                //пробуем работать с processId в файле
            }


            _mainProgressManager.ReportCurrentProgress(ExportProgressDataLevel.STAGEENDINFO,
                ExportProgressDataStage.PROCESS_MAIN, null, 0,
                0, null, 0, null, null);
        }
    }
}
