using ServiceCheck.Core.DBStructs;
using System;
using System.Collections.Generic;
using System.Data.OracleClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace ServiceCheck.Core
{
    public class ExporterOuter
    {
        // Для управления отменой задачи
        CancellationTokenSource _cancellationTokenSource;
        // Для обновления UI из фоновой задачи
        IProgress<ExportProgressDataOuter> _progressReporter;
        public event EventHandler<ExportProgressChangedEventArgsOuter> ProgressChanged;
        OSESettings _settings;
        private string _processId;
        //private List<ThreadInfoOuter> _ThreadInfoOuterList;
        private DbWorkerOuter _mainDbWorker;
        private ProgressDataManagerOuter _mainProgressManager;
        private DateTime _startDateTime;

        public string LogDBConnectionString
        {
            get
            {
                return _mainDbWorker.ConnectionString;
            }
        }

        private void OnProgressChanged(ExportProgressChangedEventArgsOuter e)
        {
            if (ProgressChanged != null)
                ProgressChanged(this, e);
        }

        public void ReportMainProcessError(string message)
        {
            var progressDatErr =
                new ExportProgressDataOuter(ExportProgressDataLevel.ERROR,
                    ExportProgressDataStageOuter.PROCESS_MAIN);
            progressDatErr.Error = message;
            //progressDatErr.ErrorDetails = ex.StackTrace;
            _mainProgressManager.ReportCurrentProgress(progressDatErr);
        }

        public void ReportMainProcessMessage(string message)
        {
            var progressData =
                new ExportProgressDataOuter(ExportProgressDataLevel.MOMENTALEVENTINFO,
                    ExportProgressDataStageOuter.PROCESS_MAIN);
            progressData.SetTextAddInfo("MOMENTAL_INFO", message);
            _mainProgressManager.ReportCurrentProgress(progressData);
        }

        public ExporterOuter()
        {
            // Создаем объект для безопасного обновления UI
            _progressReporter = new Progress<ExportProgressDataOuter>(report =>
            {
                OnProgressChanged(new ExportProgressChangedEventArgsOuter(report));
            });
        }

        void SaveObjectToFile(ThreadInfoOuter thread, string outSchemaName, string objectName,
            string objectTypeSubdirName, string ddl, string fileExtension)
        {
            var nameWithoutExt = Common.MakeValidFileName(objectName);
            string fileName = $"{nameWithoutExt}{fileExtension}".ToLower();

            string targetFolder;
            //if (thread.ExportSettings.WriteOnlyToMainDataFolder)
            if (_settings.IsWinApp)
            {
                targetFolder = _settings.ExportSettings.PathToExportDataMain;
                //if (thread.ExportSettings.UseProcessesSubFoldersInMain)
                //    targetFolder = Path.Combine(targetFolder, thread.ProcessSubFolder);
            }
            else
                //targetFolder = thread.ExportSettings.PathToExportDataTemp;
                targetFolder = _settings.SchedulerOuterSettings.PathToExportDataTemp;
            string objectTypePath = Path.Combine(targetFolder, thread.DBSubfolder, outSchemaName, objectTypeSubdirName.ToLower());
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

        /*void MoveFilesToErrorFolder(ThreadInfoOuter thread, ProgressDataManagerOuter progressManager, string outSchemaName)
        {
            var progressData = new ExportProgressDataOuter(
                ExportProgressDataLevel.STAGESTARTINFO,
                ExportProgressDataStageOuter.MOVE_FILES_TO_ERROR_DIR);
            progressManager.ReportCurrentProgress(progressData);
            int filesCount = 0;

            try
            {
                //var sourceFolder = Path.Combine(thread.ExportSettings.PathToExportDataTemp, thread.DBSubfolder, thread.UserNameSubfolder); 
                //var destFolder = Path.Combine(thread.ExportSettings.PathToExportDataWithErrors, thread.ProcessSubFolder, thread.DBSubfolder, thread.UserNameSubfolder);
                var sourceFolder = Path.Combine(_settings.SchedulerOuterSettings.PathToExportDataTemp, thread.DBSubfolder, outSchemaName);
                var destFolder = Path.Combine(_settings.SchedulerOuterSettings.PathToExportDataWithErrors, thread.ProcessSubFolder, thread.DBSubfolder, outSchemaName);
                FilesManager.DeleteDirectory(destFolder);
                filesCount = FilesManager.MoveDirectory(sourceFolder, destFolder);
            }
            catch (Exception ex)
            {
                var progressDatErr =
                    new ExportProgressDataOuter(ExportProgressDataLevel.ERROR,
                        ExportProgressDataStageOuter.MOVE_FILES_TO_ERROR_DIR);
                progressDatErr.Error = ex.Message;
                progressDatErr.ErrorDetails = ex.StackTrace;
                progressManager.ReportCurrentProgress(progressDatErr);
            }

            var progressData2 = new ExportProgressDataOuter(
                ExportProgressDataLevel.STAGEENDINFO, ExportProgressDataStageOuter.MOVE_FILES_TO_ERROR_DIR);
            progressData2.MetaObjCountFact = filesCount;
            progressManager.ReportCurrentProgress(progressData2);

        }*/

        void MoveFilesToMainFolder(ThreadInfoOuter thread, ProgressDataManagerOuter progressManager, string outSchemaName)
        {
            var progressData = new ExportProgressDataOuter(
                ExportProgressDataLevel.STAGESTARTINFO,
                ExportProgressDataStageOuter.MOVE_FILES_TO_MAIN_DIR);
            progressData.SchemaOutName = outSchemaName;
            progressManager.ReportCurrentProgress(progressData);
            int filesCount = 0;

            try
            {
                //var sourceFolder = Path.Combine(thread.ExportSettings.PathToExportDataTemp, thread.DBSubfolder, thread.UserNameSubfolder);
                var sourceFolder = Path.Combine(_settings.SchedulerOuterSettings.PathToExportDataTemp, thread.DBSubfolder, outSchemaName);
                var destFolder = thread.ExportSettings.PathToExportDataMain;
                //if (thread.ExportSettings.UseProcessesSubFoldersInMain)
                //    destFolder = Path.Combine(destFolder, thread.ProcessSubFolder);
                destFolder = Path.Combine(destFolder, thread.DBSubfolder, outSchemaName);
                //Для этого типа выгрузки мы не можем очищать папку main, так как не знаем удален ли фактически объект, или просто отозваны на него права для наших схем
                //if (thread.ExportSettings.ClearMainFolderBeforeWriting)
                //FilesManager.DeleteDirectory(destFolder);
                filesCount = FilesManager.MoveDirectory(sourceFolder, destFolder);
            }
            catch (Exception ex)
            {
                var progressDatErr =
                    new ExportProgressDataOuter(ExportProgressDataLevel.ERROR,
                        ExportProgressDataStageOuter.MOVE_FILES_TO_MAIN_DIR);
                progressDatErr.Error = ex.Message;
                progressDatErr.ErrorDetails = ex.StackTrace;
                progressDatErr.SchemaOutName = outSchemaName;
                progressManager.ReportCurrentProgress(progressDatErr);
            }

            var progressData2 = new ExportProgressDataOuter(
                ExportProgressDataLevel.STAGEENDINFO, ExportProgressDataStageOuter.MOVE_FILES_TO_MAIN_DIR);
            progressData2.MetaObjCountFact = filesCount;
            progressData2.SchemaOutName = outSchemaName;
            progressManager.ReportCurrentProgress(progressData2);
        }

        void CreateSimpleRepoCommit(ThreadInfoOuter thread, ProgressDataManagerOuter progressManager, string outSchemaName)
        {
            var progressData = new ExportProgressDataOuter(
                ExportProgressDataLevel.STAGESTARTINFO,
                ExportProgressDataStageOuter.CREATE_SIMPLE_FILE_REPO_COMMIT);
            progressData.SchemaOutName = outSchemaName;
            progressManager.ReportCurrentProgress(progressData);
            int changesCount = 0;
            List<RepoChangeItem> repoChanges = new List<RepoChangeItem>();
            try
            {
                var sourceFolder = _settings.ExportSettings.PathToExportDataMain;
                //if (thread.ExportSettings.UseProcessesSubFoldersInMain)
                //    sourceFolder = Path.Combine(sourceFolder, thread.ProcessSubFolder);
                var targetFolder = _settings.SchedulerOuterSettings.RepoSettings.SimpleFileRepo.PathToExportDataForRepo; //thread.ExportSettings.RepoSettings.SimpleFileRepo.PathToExportDataForRepo;
                var vcsManager = new VcsManager();
                //var currentRepoName = $"{thread.DBSubfolder}\\{thread.UserNameSubfolder}";
                vcsManager.CreateCommit(sourceFolder, thread.DBSubfolder, outSchemaName, targetFolder, int.Parse(thread.ProcessId), thread.StartDateTime, _settings.SchedulerOuterSettings.RepoSettings.SimpleFileRepo.IgnoreDifferences, false, out changesCount, out repoChanges);
            }
            catch (Exception ex)
            {
                var progressDatErr =
                    new ExportProgressDataOuter(ExportProgressDataLevel.ERROR,
                        ExportProgressDataStageOuter.CREATE_SIMPLE_FILE_REPO_COMMIT);
                progressDatErr.Error = ex.Message;
                progressDatErr.ErrorDetails = ex.StackTrace;
                progressDatErr.SchemaOutName = outSchemaName;
                progressManager.ReportCurrentProgress(progressDatErr);
            }
            var progressData2 = new ExportProgressDataOuter(
                ExportProgressDataLevel.STAGEENDINFO, ExportProgressDataStageOuter.CREATE_SIMPLE_FILE_REPO_COMMIT);
            progressData2.MetaObjCountFact = changesCount;
            progressData2.SetddInfo("REPO_CHANGES", repoChanges);
            //progressData2.SetddInfo("OUTER_SCHEMA", outSchemaName);
            progressData2.SchemaOutName = outSchemaName;
            progressManager.ReportCurrentProgress(progressData2);
        }

        

        public void SetSettings(OSESettings settings)
        {
            _settings = settings;
            _mainProgressManager = new ProgressDataManagerOuter(_progressReporter);
        }

        public void SetSchedulerOuterProps()
        {
            var dbLogConn = _settings.Connections.First(c =>
                c.DBIdC.ToUpper() == _settings.SchedulerOuterSettings.DBLog.DBLogDBId.ToUpper() && c.UserName.ToUpper() ==
                _settings.SchedulerOuterSettings.DBLog.DBLogUserName.ToUpper());
            var connectionString = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)" +
                                   $"(HOST={dbLogConn.Host})(PORT={dbLogConn.Port}))" +
                                   $"(CONNECT_DATA=(SID={dbLogConn.SID})));" +
                                   $"User Id={dbLogConn.UserName};Password={dbLogConn.PasswordC};";
            _mainProgressManager.SetSchedulerProps(null, dbLogConn);
            _mainDbWorker = new DbWorkerOuter(connectionString, null, _mainProgressManager);
        }

        public /*async*/ void StartWork(ThreadInfoOuter threadInfoOuter, string connToProcessInfo, bool testMode, string grantsFolder)
        {
            if (_cancellationTokenSource != null)
                throw new Exception("Задача уже выполняется");
            _startDateTime = DateTime.Now;
            //_ThreadInfoOuterList = ThreadInfoOuterList;
            _cancellationTokenSource = new CancellationTokenSource();
            var ct = _cancellationTokenSource.Token;

            if (_mainDbWorker != null)
                _mainDbWorker.SetCancellationToken(ct);

            StartProcess(_startDateTime, connToProcessInfo /*, ct*/);
            //_ThreadInfoOuterList.ForEach(c => c.ProcessId = _processId);

            threadInfoOuter.ProcessId = _processId;

            // Здесь работаем только в синхронном режиме и только одна задача за один проход
            StartWork(threadInfoOuter, ct, testMode, grantsFolder);

        }

        public void CancelWork()
        {
            _cancellationTokenSource?.Cancel();
        }


        void ProcessSchema(ThreadInfoOuter threadInfoOuter, CancellationToken ct,
            ProgressDataManagerOuter progressManager, bool testMode, string grantsFolder, out int allObjectsCountPlan,
            out int allObjectsCountFact)
        {
            allObjectsCountPlan = 0;
            allObjectsCountFact = 0;

            int allObjectsCounter = 0;
            int currentSchemaOutObjectsCounter;
            int currentTypeSchemaOutObjectsCounter;

            bool canceledByUser;

            var tablesComments = new List<TableOrViewComment>();
            var viewsComments = new List<TableOrViewComment>();
            var tablesAndViewsColumnsComments = new List<ColumnComment>();
            var tablesStructs = new List<TableStruct>();
            var tablesAndViewsColumnStruct = new List<TableColumnStruct>();
            var tablesIndexes = new List<IndexStruct>();
            var tablesConstraints = new List<ConstraintStruct>();
            var partTables = new List<PartTables>();

            var synonymsStructs = new List<SynonymAttributes>();
            var sequencesStructs = new List<SequenceAttributes>();
            var schedulerJobsStructs = new List<SchedulerJob>();
            var dbmsJobsStructs = new List<DBMSJob>();
            var packagesHeaders = new List<DbObjectText>();
            var packagesBodies = new List<DbObjectText>();
            var packages = new List<DbPackageText>();
            var functionsText = new List<DbObjectText>();
            var proceduresText = new List<DbObjectText>();
            var triggersText = new List<DbObjectText>();
            var typesText = new List<DbObjectText>();
            var viewsText = new List<DbObjectText>();

            var exportSettingsDetailsLowPriority = threadInfoOuter.ExportSettings.ExportSettingsDetails;
            var exportSettingsDetailsHighPriority = threadInfoOuter.Connection.ExportSettingsDetails;

            var exportSettingsDetails =
                ExportSettingsDetails.GetSumExportSettingsDetails(exportSettingsDetailsLowPriority,
                    exportSettingsDetailsHighPriority);
            var settingsConnection = threadInfoOuter.Connection;
            //var objectNameMask = exportSettingsDetails.MaskForFileNames;
            //var outputFolder = ThreadInfoOuter.ExportSettings.PathToExportDataMain;
            var objectTypesToProcess = exportSettingsDetails.ObjectTypesToProcessC;

            var schemasIncludeStr = threadInfoOuter.SchemasInclude == null || !threadInfoOuter.SchemasInclude.Any()
                ? ""
                : threadInfoOuter
                    .SchemasInclude.MergeFormatted("'", ",");
            var schemasExcludeStr = threadInfoOuter.SchemasExclude == null || !threadInfoOuter.SchemasExclude.Any()
                ? ""
                : threadInfoOuter
                    .SchemasExclude.MergeFormatted("'", ",");

            string connectionString = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)" +
                                      $"(HOST={settingsConnection.Host})(PORT={settingsConnection.Port}))" +
                                      $"(CONNECT_DATA=(SID={settingsConnection.SID})));" +
                                      $"User Id={settingsConnection.UserName};Password={settingsConnection.PasswordC};";

            var progressDataForSchema = new ExportProgressDataOuter(ExportProgressDataLevel.STAGESTARTINFO,
                ExportProgressDataStageOuter.PROCESS_SCHEMA);
            progressDataForSchema.SetTextAddInfo("DBFOLDER", threadInfoOuter.DBSubfolder);
            progressDataForSchema.SetTextAddInfo("DBLINK", threadInfoOuter.DbLink);
            progressManager.ReportCurrentProgress(progressDataForSchema);

            try
            {
                using (OracleConnection connection = new OracleConnection(connectionString))
                {

                    if (!testMode)
                    {
                        connection.Open();
                        //var osb = new OracleConnectionStringBuilder(connection.ConnectionString);
                        //var userId = osb.UserID.ToUpper();

                        var dbWorker = new DbWorkerOuter(connection, threadInfoOuter.DbLink, progressManager);
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

                        DbLinkAttrigutes currentDbLink = null;
                        //получаем все dblink, чтобы понять под кем логически мы сейчас работаем
                        if (!string.IsNullOrWhiteSpace(threadInfoOuter.DbLink))
                        {
                            currentDbLink = dbWorker.GetCurDbLink(threadInfoOuter.DbLink,
                                ExportProgressDataStageOuter.GET_CURRENT_DBLINK, out canceledByUser);
                        }

                        var systemViewInfo = dbWorker.GetInfoAboutSystemViews(systemViewsToCheck,
                            ExportProgressDataStageOuter.GET_INFO_ABOUT_SYS_VIEW, out canceledByUser);
                        if (canceledByUser) return;

                        dbWorker.SetSessionTransform(exportSettingsDetails.SessionTransformC);

                        List<ObjectTypeNames> namesList = dbWorker.GetObjectsNames(objectTypesToProcess,
                            schemasIncludeStr, schemasExcludeStr,
                            ExportProgressDataStageOuter.GET_OBJECTS_NAMES, out canceledByUser);
                        if (canceledByUser) return;

                        var objListSaveSettings = threadInfoOuter.ExportSettings.ObjectsListSaveSettings;
                        if (objListSaveSettings != null && objListSaveSettings.SaveObjectsList)
                        {

                            Common.SaveObjectsListList(
                                namesList,
                                objListSaveSettings.PathToResFiles,
                                threadInfoOuter.DBSubfolder,
                                settingsConnection.UserName,
                                threadInfoOuter.DbLink,
                                threadInfoOuter.ProcessId);
                        }

                        if (threadInfoOuter.Mode != "OnlyObjectsLists")
                        {

                            allObjectsCountPlan = namesList.Sum(c => c.UniqueNames.Count);

                            var grants = dbWorker.GetAllObjectsGrants(schemasIncludeStr, schemasExcludeStr,
                                exportSettingsDetails.SkipGrantOptionsC,
                                ExportProgressDataStageOuter.GET_GRANTS, allObjectsCountPlan, out canceledByUser);
                            if (canceledByUser) return;

                            //имя пользователя, под которым сейчас логически работаем
                            //если dblink не указан, то это пользователь коннекта
                            //если dblink указан, то это пользователь dblink
                            string logicalUserName = string.IsNullOrWhiteSpace(threadInfoOuter.DbLink)
                                ? settingsConnection.UserName.ToUpper()
                                : currentDbLink.UserName.ToUpper();

                            GrantsOuterManager.SaveGrantsForCurrentSchemaAndForPublic(grants, grantsFolder,
                                threadInfoOuter.DBSubfolder, logicalUserName);

                            var allGrants = GrantsOuterManager.GetGrants(grantsFolder, threadInfoOuter.DBSubfolder);


                            if (objectTypesToProcess.Contains("TABLES") || objectTypesToProcess.Contains("VIEWS"))
                            {
                                tablesAndViewsColumnStruct = dbWorker.GetTablesAndViewsColumnsStruct(
                                    ExportProgressDataStageOuter.GET_COLUMNS, allObjectsCountPlan, schemasIncludeStr,
                                    schemasExcludeStr, systemViewInfo,
                                    out canceledByUser);
                                if (canceledByUser) return;

                                tablesAndViewsColumnsComments = dbWorker.GetTablesAndViewsColumnComments(
                                    ExportProgressDataStageOuter.GET_COLUMNS_COMMENTS, allObjectsCountPlan,
                                    schemasIncludeStr, schemasExcludeStr,
                                    out canceledByUser);
                                if (canceledByUser) return;
                            }


                            foreach (var objectType in objectTypesToProcess)
                            {

                                var currentNamesList = namesList.Where(c => c.ObjectType == objectType)
                                    .SelectMany(c => c.UniqueNames).ToList();
                                var typeObjCountFact = 0;

                                //int currentTypeObjectsCounter = 0;
                                var typeObjCountPlan = currentNamesList.Count;

                                if (typeObjCountPlan > 0)
                                {

                                    try
                                    {
                                        //currentObjectTypes = objectType;
                                        string dbObjectType = Common.GetObjectTypeName(objectType);

                                        var progressDataForType = new ExportProgressDataOuter(
                                            ExportProgressDataLevel.STAGESTARTINFO,
                                            ExportProgressDataStageOuter.PROCESS_OBJECT_TYPE);
                                        progressDataForType.AllObjCountPlan = allObjectsCountPlan;
                                        progressDataForType.TypeObjCountPlan = typeObjCountPlan;
                                        progressDataForType.ObjectType = objectType;
                                        progressManager.ReportCurrentProgress(progressDataForType);


                                        if (objectType == "SYNONYMS")
                                        {
                                            synonymsStructs = dbWorker.GetSynonyms(
                                                ExportProgressDataStageOuter.GET_SYNONYMS,
                                                allObjectsCountPlan, typeObjCountPlan, objectType,
                                                schemasIncludeStr, schemasExcludeStr,
                                                out canceledByUser);
                                            typeObjCountFact = synonymsStructs.Count;
                                            if (canceledByUser) return;
                                        }

                                        if (objectType == "SEQUENCES")
                                        {
                                            sequencesStructs = dbWorker.GetSequences(
                                                ExportProgressDataStageOuter.GET_SEQUENCES,
                                                allObjectsCountPlan, typeObjCountPlan, objectType,
                                                schemasIncludeStr, schemasExcludeStr,
                                                out canceledByUser);
                                            typeObjCountFact = sequencesStructs.Count;
                                            if (canceledByUser) return;
                                        }

                                        if (objectType == "JOBS")
                                        {
                                            schedulerJobsStructs = dbWorker.GetSchedulerJobs(
                                                ExportProgressDataStageOuter.GET_SCHEDULER_JOBS, allObjectsCountPlan,
                                                typeObjCountPlan,
                                                objectType, schemasIncludeStr, schemasExcludeStr,
                                                out canceledByUser);
                                            typeObjCountFact = schedulerJobsStructs.Count;
                                            if (canceledByUser) return;
                                            dbmsJobsStructs = dbWorker.GetDBMSJobs(
                                                ExportProgressDataStageOuter.GET_DMBS_JOBS,
                                                allObjectsCountPlan, typeObjCountPlan, objectType,
                                                schemasIncludeStr, schemasExcludeStr,
                                                out canceledByUser);
                                            typeObjCountFact += dbmsJobsStructs.Count;
                                            if (canceledByUser) return;
                                        }

                                        if (objectType == "PACKAGES")
                                        {
                                            packagesHeaders = dbWorker.GetObjectsSourceByType("PACKAGE",
                                                /*currentOutSchemaName,*/
                                                ExportProgressDataStageOuter.GET_PACKAGES_HEADERS, allObjectsCountPlan,
                                                typeObjCountPlan,
                                                objectType, schemasIncludeStr, schemasExcludeStr,
                                                out canceledByUser);
                                            if (canceledByUser) return;

                                            packagesBodies =
                                                dbWorker.GetObjectsSourceByType(
                                                    "PACKAGE BODY", /*currentOutSchemaName,*/
                                                    ExportProgressDataStageOuter.GET_PACKAGES_BODIES,
                                                    allObjectsCountPlan,
                                                    typeObjCountPlan,
                                                    objectType, schemasIncludeStr, schemasExcludeStr,
                                                    out canceledByUser);
                                            foreach (var packagesHeader in packagesHeaders)
                                            {
                                                var currentPackage = packages.FirstOrDefault(c =>
                                                    c.Owner == packagesHeader.Owner && c.Name == packagesHeader.Name);
                                                if (currentPackage == null)
                                                {
                                                    currentPackage = new DbPackageText
                                                    {
                                                        Owner = packagesHeader.Owner, Name = packagesHeader.Name,
                                                        Header = packagesHeader.Text
                                                    };
                                                    packages.Add(currentPackage);
                                                }
                                            }

                                            foreach (var packagesBody in packagesBodies)
                                            {
                                                var currentPackage = packages.FirstOrDefault(c =>
                                                    c.Owner == packagesBody.Owner && c.Name == packagesBody.Name);
                                                if (currentPackage == null)
                                                {
                                                    currentPackage = new DbPackageText
                                                        {Owner = packagesBody.Owner, Name = packagesBody.Name};
                                                    packages.Add(currentPackage);
                                                }

                                                currentPackage.Body = packagesBody.Text;
                                            }

                                            typeObjCountFact = packages.Count;
                                            if (canceledByUser) return;
                                        }

                                        if (objectType == "FUNCTIONS")
                                        {
                                            functionsText = dbWorker.GetObjectsSourceByType(
                                                Common.GetObjectTypeName(objectType),
                                                /*currentOutSchemaName,*/ ExportProgressDataStageOuter.GET_FUNCTIONS,
                                                allObjectsCountPlan,
                                                typeObjCountPlan,
                                                objectType, schemasIncludeStr, schemasExcludeStr,
                                                out canceledByUser);
                                            typeObjCountFact = functionsText.Count;
                                            if (canceledByUser) return;
                                        }

                                        if (objectType == "PROCEDURES")
                                        {
                                            proceduresText = dbWorker.GetObjectsSourceByType(
                                                Common.GetObjectTypeName(objectType),
                                                /*currentOutSchemaName,*/ ExportProgressDataStageOuter.GET_PROCEDURES,
                                                allObjectsCountPlan,
                                                typeObjCountPlan,
                                                objectType, schemasIncludeStr, schemasExcludeStr,
                                                out canceledByUser);
                                            typeObjCountFact = proceduresText.Count;
                                            if (canceledByUser) return;
                                        }

                                        if (objectType == "TRIGGERS")
                                        {
                                            triggersText = dbWorker.GetObjectsSourceByType(
                                                Common.GetObjectTypeName(objectType),
                                                /*currentOutSchemaName,*/ ExportProgressDataStageOuter.GET_TRIGGERS,
                                                allObjectsCountPlan,
                                                typeObjCountPlan,
                                                objectType, schemasIncludeStr, schemasExcludeStr,
                                                out canceledByUser);
                                            typeObjCountFact = triggersText.Count;
                                            if (canceledByUser) return;
                                        }

                                        if (objectType == "TYPES")
                                        {
                                            typesText = dbWorker.GetObjectsSourceByType(
                                                Common.GetObjectTypeName(objectType),
                                                /*currentOutSchemaName,*/ ExportProgressDataStageOuter.GET_TYPES,
                                                allObjectsCountPlan,
                                                typeObjCountPlan,
                                                objectType, schemasIncludeStr, schemasExcludeStr,
                                                out canceledByUser);
                                            typeObjCountFact = typesText.Count;
                                            if (canceledByUser) return;
                                        }

                                        if (objectType == "TABLES")
                                        {

                                            tablesConstraints = dbWorker.GetTablesConstraints( /*userId,*/
                                                ExportProgressDataStageOuter.GET_TABLE_CONSTRAINTS, allObjectsCountPlan,
                                                typeObjCountPlan,
                                                objectType, /*schemasIncludeStr, schemasExcludeStr,*/
                                                out canceledByUser);
                                            if (canceledByUser) return;

                                            tablesStructs = dbWorker.GetTablesStruct(
                                                ExportProgressDataStageOuter.GET_TABLES_STRUCTS,
                                                allObjectsCountPlan, typeObjCountPlan, objectType,
                                                schemasIncludeStr, schemasExcludeStr,
                                                out canceledByUser);
                                            typeObjCountFact = tablesStructs.Count;
                                            if (canceledByUser) return;

                                            tablesIndexes = dbWorker.GetTablesIndexes(
                                                ExportProgressDataStageOuter.GET_TABLES_INDEXES,
                                                allObjectsCountPlan, typeObjCountPlan, objectType,
                                                schemasIncludeStr, schemasExcludeStr,
                                                out canceledByUser);
                                            if (canceledByUser) return;

                                            tablesComments =
                                                dbWorker.GetTableOrViewComments(dbObjectType,
                                                    ExportProgressDataStageOuter.GET_TABLES_COMMENTS,
                                                    allObjectsCountPlan,
                                                    typeObjCountPlan,
                                                    objectType, schemasIncludeStr, schemasExcludeStr,
                                                    out canceledByUser);
                                            if (canceledByUser) return;

                                            partTables = dbWorker.GetTablesPartitions(
                                                exportSettingsDetails.GetPartitionMode,
                                                ExportProgressDataStageOuter.GET_TABLES_PARTS, allObjectsCountPlan,
                                                typeObjCountPlan,
                                                objectType, systemViewInfo, schemasIncludeStr,
                                                schemasExcludeStr, out canceledByUser);
                                            if (canceledByUser) return;
                                        }

                                        if (objectType == "VIEWS")
                                        {

                                            viewsText = dbWorker.GetViews(ExportProgressDataStageOuter.GET_VIEWS,
                                                allObjectsCountPlan, typeObjCountPlan,
                                                objectType, schemasIncludeStr, schemasExcludeStr,
                                                out canceledByUser);
                                            if (canceledByUser) return;
                                            typeObjCountFact = viewsText.Count;

                                            viewsComments =
                                                dbWorker.GetTableOrViewComments(dbObjectType,
                                                    ExportProgressDataStageOuter.GET_VIEWS_COMMENTS,
                                                    allObjectsCountPlan,
                                                    typeObjCountPlan,
                                                    objectType, schemasIncludeStr, schemasExcludeStr,
                                                    out canceledByUser);
                                            if (canceledByUser) return;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        var progressDataErr = new ExportProgressDataOuter(ExportProgressDataLevel.ERROR,
                                            ExportProgressDataStageOuter.PROCESS_OBJECT_TYPE);
                                        progressDataErr.Error = ex.Message;
                                        progressDataErr.ErrorDetails = ex.StackTrace;
                                        progressDataErr.AllObjCountPlan = allObjectsCountPlan;
                                        progressDataErr.TypeObjCountPlan = currentNamesList.Count;
                                        progressDataErr.ObjectType = objectType;
                                        progressManager.ReportCurrentProgress(progressDataErr);
                                    }

                                    var progressDataForType2 = new ExportProgressDataOuter(
                                        ExportProgressDataLevel.STAGEENDINFO,
                                        ExportProgressDataStageOuter.PROCESS_OBJECT_TYPE);
                                    progressDataForType2.AllObjCountPlan = allObjectsCountPlan;
                                    progressDataForType2.TypeObjCountPlan = currentNamesList.Count;
                                    progressDataForType2.TypeObjCountFact = typeObjCountFact;
                                    progressDataForType2.ObjectType = objectType;
                                    progressManager.ReportCurrentProgress(progressDataForType2);

                                }
                            }


                            if (_settings.IsSchedulerOuter || _settings.WinAppSettings.ClearMainFolderBeforeWriting)
                            {
                                var destFolder = _settings.IsWinApp
                                    ? _settings.ExportSettings.PathToExportDataMain
                                    : _settings.SchedulerOuterSettings.PathToExportDataTemp;
                                destFolder = Path.Combine(destFolder, threadInfoOuter.DBSubfolder);
                                FilesManager.DeleteDirectory(destFolder);
                            }

                            foreach (var schemaCollection in namesList.GroupBy(c => c.SchemaName))
                            {

                                var schemaOutObjectsPlan = schemaCollection.Sum(c => c.UniqueNames.Count);
                                var currentOutSchemaName = schemaCollection.Key;
                                currentSchemaOutObjectsCounter = 0;

                                var progressDataForOutSchema = new ExportProgressDataOuter(
                                    ExportProgressDataLevel.STAGESTARTINFO,
                                    ExportProgressDataStageOuter.PROCESS_OUT_SCHEMA);
                                progressDataForOutSchema.SchemaOutObjCountPlan = schemaOutObjectsPlan;
                                if (allObjectsCounter == 0)
                                    progressDataForOutSchema.Current = null;
                                else
                                    progressDataForOutSchema.Current = allObjectsCounter;
                                progressDataForOutSchema.SchemaOutName = currentOutSchemaName;
                                progressManager.ReportCurrentProgress(progressDataForOutSchema);


                                foreach (var objectType in objectTypesToProcess)
                                {
                                    currentTypeSchemaOutObjectsCounter = 0;

                                    var currentNamesList = schemaCollection.Where(c => c.ObjectType == objectType)
                                        .SelectMany(c => c.UniqueNames).ToList();


                                    //int currentTypeObjectsCounter = 0;
                                    var currentTypeSchemaOutObjectsPlan = currentNamesList.Count;


                                    for (var i = 0; i < currentNamesList.Count; i++)
                                    {

                                        var objectName = currentNamesList[i];

                                        allObjectsCounter++;
                                        currentSchemaOutObjectsCounter++;
                                        currentTypeSchemaOutObjectsCounter++;

                                        string ddl = string.Empty;
                                        string ddlPackageBody = string.Empty;
                                        string ddlPackageHead = string.Empty;
                                        //currentObjectName = objectName;

                                        bool currentObjIsSchedulerJob = false;
                                        bool currentObjIsDBMSJob = false;

                                        try
                                        {

                                            var progressDataForObject = new ExportProgressDataOuter(
                                                ExportProgressDataLevel.STAGESTARTINFO,
                                                ExportProgressDataStageOuter.PROCESS_OBJECT);
                                            progressDataForObject.AllObjCountPlan = allObjectsCountPlan;
                                            progressDataForObject.SchemaOutObjCountPlan = schemaOutObjectsPlan;
                                            progressDataForObject.TypeObjCountPlan = currentNamesList.Count;
                                            progressDataForObject.Current = allObjectsCounter;
                                            progressDataForObject.ObjectType = objectType;
                                            progressDataForObject.ObjectName = objectName;
                                            progressDataForObject.SchemaOutName = currentOutSchemaName;
                                            progressManager.ReportCurrentProgress(progressDataForObject);

                                            if (objectType == "PACKAGES")
                                            {
                                                ddlPackageHead = DDLCreator.GetObjectDdlForPackageHeader(
                                                    packagesHeaders, currentOutSchemaName,
                                                    objectName,
                                                    exportSettingsDetails.AddSlashToC);
                                                ddlPackageBody = DDLCreator.GetObjectDdlForPackageBody(packagesBodies,
                                                    currentOutSchemaName,
                                                    objectName,
                                                    exportSettingsDetails.AddSlashToC);
                                            }
                                            else if (objectType == "FUNCTIONS")
                                            {
                                                ddl = DDLCreator.GetObjectDdlForSourceText(functionsText,
                                                    currentOutSchemaName, objectName,
                                                    objectType,
                                                    exportSettingsDetails.AddSlashToC);
                                            }
                                            else if (objectType == "PROCEDURES")
                                            {
                                                ddl = DDLCreator.GetObjectDdlForSourceText(proceduresText,
                                                    currentOutSchemaName, objectName,
                                                    objectType,
                                                    exportSettingsDetails.AddSlashToC);
                                            }
                                            else if (objectType == "TRIGGERS")
                                            {
                                                ddl = DDLCreator.GetObjectDdlForSourceText(triggersText,
                                                    currentOutSchemaName, objectName,
                                                    objectType,
                                                    exportSettingsDetails.AddSlashToC);
                                            }
                                            else if (objectType == "TYPES")
                                            {
                                                ddl = DDLCreator.GetObjectDdlForSourceText(typesText,
                                                    currentOutSchemaName, objectName,
                                                    objectType,
                                                    exportSettingsDetails.AddSlashToC);
                                            }
                                            else if (objectType == "SYNONYMS")
                                            {
                                                ddl = DDLCreator.GetObjectDdlForSynonym(synonymsStructs,
                                                    currentOutSchemaName, objectName,
                                                    exportSettingsDetails.AddSlashToC);
                                            }
                                            else if (objectType == "SEQUENCES")
                                            {
                                                ddl = DDLCreator.GetObjectDdlForSequence(sequencesStructs,
                                                    currentOutSchemaName, objectName,
                                                    exportSettingsDetails.SetSequencesValuesTo1,
                                                    exportSettingsDetails.AddSlashToC);
                                            }
                                            else if (objectType == "VIEWS")
                                            {
                                                ddl = DDLCreator.GetObjectDdlForView(viewsText,
                                                    tablesAndViewsColumnStruct,
                                                    viewsComments,
                                                    tablesAndViewsColumnsComments, currentOutSchemaName, objectName,
                                                    exportSettingsDetails.AddSlashToC);
                                            }
                                            else if (objectType == "TABLES")
                                            {

                                                ddl = DDLCreator.GetObjectDdlForTable(tablesStructs,
                                                    tablesAndViewsColumnStruct,
                                                    tablesConstraints,
                                                    tablesIndexes, tablesComments, tablesAndViewsColumnsComments,
                                                    partTables,
                                                    objectName, currentOutSchemaName, exportSettingsDetails.AddSlashToC,
                                                    true);
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
                                                    exportSettingsDetails.AddSlashToC,
                                                    ExportProgressDataStageOuter.GET_DBLINK,
                                                    allObjectsCountPlan, currentTypeSchemaOutObjectsPlan,
                                                    allObjectsCounter,
                                                    out canceledByUser);
                                                if (canceledByUser) return;
                                            }
                                            else
                                            {
                                                //сюда не должны зайти, но оставим на всякий случай
                                                var objectSource = dbWorker.GetObjectSource(objectName, objectType,
                                                    exportSettingsDetails.AddSlashToC,
                                                    ExportProgressDataStageOuter.GET_UNKNOWN_OBJECT_DDL,
                                                    allObjectsCountPlan,
                                                    currentTypeSchemaOutObjectsPlan,
                                                    allObjectsCounter, out canceledByUser);
                                                if (canceledByUser) return;
                                                ddl = DDLCreator.AddCreateOrReplace(objectSource);
                                            }

                                            string objectGrants = DDLCreator.GetObjectGrants(allGrants,
                                                currentOutSchemaName, objectName,
                                                exportSettingsDetails.OrderGrantOptionsC);
                                            if (objectType != "PACKAGES")
                                            {
                                                ddl = DDLCreator.GetDDlWithGrants(ddl, objectGrants);
                                            }
                                            else
                                            {
                                                ddlPackageHead =
                                                    DDLCreator.GetDDlWithGrants(ddlPackageHead, objectGrants);
                                                ddlPackageBody =
                                                    DDLCreator.GetDDlWithGrants(ddlPackageBody, objectGrants);
                                                ddl = DDLCreator.MergeHeadAndBody(ddlPackageHead, ddlPackageBody);
                                            }

                                            if (string.IsNullOrWhiteSpace(ddl))
                                            {
                                                var objType = Common.GetObjectTypeName(objectType);
                                                //throw new Exception($"Для объекта {objectName} не удалось получить ddl");
                                                var message =
                                                    $"Предупреждение!!! Для {objType} {objectName} не удалось получить ddl!";
                                                var progressWarning = new ExportProgressDataOuter(
                                                    ExportProgressDataLevel.WARNING,
                                                    ExportProgressDataStageOuter.PROCESS_OBJECT_TYPE);
                                                progressWarning.SetTextAddInfo("WARNING_INFO", message);
                                                progressWarning.SchemaOutName = currentOutSchemaName;
                                                progressManager.ReportCurrentProgress(progressWarning);

                                                allObjectsCounter--;
                                                currentSchemaOutObjectsCounter--;
                                                currentTypeSchemaOutObjectsCounter--;
                                            }
                                            else
                                            {
                                                string extension = Common.GetExtensionForObjectType(objectType,
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

                                                SaveObjectToFile(threadInfoOuter, currentOutSchemaName, objectName,
                                                    objectTypeSubdirName, ddl,
                                                    extension);
                                            }

                                            //currentObjectName = string.Empty;

                                        }
                                        catch (Exception ex)
                                        {
                                            var progressDataForObjectErr =
                                                new ExportProgressDataOuter(ExportProgressDataLevel.ERROR,
                                                    ExportProgressDataStageOuter.PROCESS_OBJECT);
                                            progressDataForObjectErr.Error = ex.Message;
                                            progressDataForObjectErr.ErrorDetails = ex.StackTrace;
                                            progressDataForObjectErr.AllObjCountPlan = allObjectsCountPlan;
                                            progressDataForObjectErr.SchemaOutObjCountPlan = schemaOutObjectsPlan;
                                            progressDataForObjectErr.TypeObjCountPlan = currentNamesList.Count;
                                            progressDataForObjectErr.Current = allObjectsCounter;
                                            progressDataForObjectErr.ObjectType = objectType;
                                            progressDataForObjectErr.ObjectName = objectName;
                                            progressDataForObjectErr.SchemaOutName = currentOutSchemaName;
                                            progressManager.ReportCurrentProgress(progressDataForObjectErr);

                                            allObjectsCounter--;
                                            currentSchemaOutObjectsCounter--;
                                            currentTypeSchemaOutObjectsCounter--;
                                        }

                                        var progressDataForObject2 = new ExportProgressDataOuter(
                                            ExportProgressDataLevel.STAGEENDINFO,
                                            ExportProgressDataStageOuter.PROCESS_OBJECT);
                                        progressDataForObject2.AllObjCountPlan = allObjectsCountPlan;
                                        progressDataForObject2.SchemaOutObjCountPlan = schemaOutObjectsPlan;
                                        progressDataForObject2.TypeObjCountPlan = currentNamesList.Count;
                                        progressDataForObject2.Current = allObjectsCounter;
                                        progressDataForObject2.ObjectType = objectType;
                                        progressDataForObject2.ObjectName = objectName;
                                        progressDataForObject2.SchemaOutName = currentOutSchemaName;
                                        progressManager.ReportCurrentProgress(progressDataForObject2);
                                    }


                                }





                                if (ct.IsCancellationRequested)
                                {
                                    var progressDataCancel = new ExportProgressDataOuter(ExportProgressDataLevel.CANCEL,
                                        ExportProgressDataStageOuter.PROCESS_SCHEMA);
                                    progressDataCancel.AllObjCountPlan = allObjectsCountPlan;
                                    progressDataCancel.AllObjCountFact = allObjectsCounter;
                                    progressManager.ReportCurrentProgress(progressDataCancel);
                                    break;
                                }

                                if (_settings.IsSchedulerOuter)
                                {
                                    if (progressManager.CurrentThreadErrorsCount > 0)
                                    {
                                        //TODO переносить в err только файлы с ошибками
                                        //MoveFilesToErrorFolder(threadInfoOuter, progressManager, currentOutSchemaName);
                                    }

                                    //else
                                    MoveFilesToMainFolder(threadInfoOuter, progressManager, currentOutSchemaName);
                                }

                                if (_settings.IsSchedulerOuter && _settings.SchedulerOuterSettings.RepoSettings != null)
                                {
                                    if (_settings.SchedulerOuterSettings.RepoSettings.SimpleFileRepo != null &&
                                        _settings.SchedulerOuterSettings.RepoSettings.SimpleFileRepo
                                            .CommitToRepoAfterSuccess /* &&
                                    progressManager.CurrentThreadErrorsCount == 0*/)
                                    {
                                        CreateSimpleRepoCommit(threadInfoOuter, progressManager, currentOutSchemaName);
                                    }
                                }

                                var progressData = new ExportProgressDataOuter(ExportProgressDataLevel.STAGEENDINFO,
                                    ExportProgressDataStageOuter.PROCESS_OUT_SCHEMA);
                                progressData.AllObjCountPlan = allObjectsCountPlan;
                                progressData.AllObjCountFact = allObjectsCounter;
                                progressData.Current = allObjectsCounter;
                                progressData.ErrorsCount =
                                    progressManager.CurrentThreadErrorsCountBySchemaOutName(currentOutSchemaName);
                                progressData.WarningsCount =
                                    progressManager.CurrentThreadWarningsCountBySchemaOutName(currentOutSchemaName);
                                progressData.SchemaOutObjCountPlan = schemaOutObjectsPlan;
                                progressData.SchemaOutObjCountFact = currentSchemaOutObjectsCounter;
                                progressData.SchemaOutName = currentOutSchemaName;
                                progressManager.ReportCurrentProgress(progressData);
                            }

                        }

                    }
                }
            }
            catch (Exception ex)
            {
                var progressDataErr = new ExportProgressDataOuter(ExportProgressDataLevel.ERROR,
                    ExportProgressDataStageOuter.PROCESS_SCHEMA);
                progressDataErr.Error = ex.Message;
                progressDataErr.ErrorDetails = ex.StackTrace;
                progressDataErr.AllObjCountPlan = allObjectsCountPlan;
                progressDataErr.AllObjCountFact = allObjectsCounter;
                progressManager.ReportCurrentProgress(progressDataErr);
            }

            allObjectsCountFact = allObjectsCounter;
        }


        public /*async*/ void StartWork(ThreadInfoOuter threadInfoOuter, CancellationToken ct, bool testMode, string grantsFolder)
        {
            var progressManager = new ProgressDataManagerOuter(_progressReporter);
            progressManager.SetSchedulerProps(threadInfoOuter.ProcessId, threadInfoOuter.Connection);
            progressManager.SetCurrentThreadProps(threadInfoOuter.DbLink, threadInfoOuter.DBSubfolder);

            _mainProgressManager.ChildProgressManagers.Add(progressManager);

            int schemaObjectsCountPlan;
            int schemaObjectsCountFact;
            ProcessSchema(threadInfoOuter, ct, progressManager, testMode, grantsFolder, out schemaObjectsCountPlan, out schemaObjectsCountFact);

            //ThreadInfoOuter.Finished = true;

            var progressData = new ExportProgressDataOuter(ExportProgressDataLevel.STAGEENDINFO,
                ExportProgressDataStageOuter.PROCESS_SCHEMA);
            progressData.AllObjCountPlan = schemaObjectsCountPlan;
            progressData.AllObjCountFact = schemaObjectsCountFact;
            progressData.Current = schemaObjectsCountFact;
            progressData.ErrorsCount = progressManager.CurrentThreadErrorsCount;
            progressData.WarningsCount = progressManager.CurrentThreadWarningsCount;
            progressData.SetTextAddInfo("DBFOLDER", threadInfoOuter.DBSubfolder);
            progressData.SetTextAddInfo("DBLINK", threadInfoOuter.DbLink);
            progressManager.ReportCurrentProgress(progressData);


            var schemasSuccess = _mainProgressManager.ChildProgressManagers.Where(c => c.AllErrorsCount + c.AllWarningsCount == 0)
                .Select(c => c.Connection.UserNameAndDBIdC).ToList().MergeFormatted("", ",");
            var schemasWithErrors = _mainProgressManager.ChildProgressManagers.Where(c => c.AllErrorsCount > 0)
                .Select(c => c.Connection.UserNameAndDBIdC).ToList().MergeFormatted("", ",");
            var schemasWithWarnings = _mainProgressManager.ChildProgressManagers.Where(c => c.AllWarningsCount > 0)
                .Select(c => c.Connection.UserNameAndDBIdC).ToList().MergeFormatted("", ",");

            var progressDataProcMain = new ExportProgressDataOuter(ExportProgressDataLevel.STAGEENDINFO,
                ExportProgressDataStageOuter.PROCESS_MAIN);
            progressDataProcMain.ProcessObjCountPlan = _mainProgressManager.ChildThreadsSchemaObjCountPlan;
            progressDataProcMain.ProcessObjCountFact = _mainProgressManager.ChildThreadsSchemaObjCountFact;
            progressDataProcMain.ErrorsCount = _mainProgressManager.AllErrorsCount;
            progressDataProcMain.WarningsCount = _mainProgressManager.AllWarningsCount;
            progressDataProcMain.SetTextAddInfo("SCHEMAS_SUCCESS", schemasSuccess);
            progressDataProcMain.SetTextAddInfo("SCHEMAS_ERROR", schemasWithErrors);
            progressDataProcMain.SetTextAddInfo("SCHEMAS_WARNING", schemasWithWarnings);
            _mainProgressManager.ReportCurrentProgress(progressDataProcMain);
            if (_settings.IsSchedulerOuter)
                EndProcess(_settings.SchedulerOuterSettings.DBLog.DBLogPrefix, progressDataProcMain);

        }

        public void StartProcess(DateTime currentDateTime, string schemasToWork/*, CancellationToken ct*/)
        {

            //if (ct.IsCancellationRequested)
            //{
            //    _mainProgressManager.ReportCurrentProgress(ExportProgressDataLevel.CANCEL,
            //        ExportProgressDataStageOuter.UNPLANNED_EXIT, null, 0,
            //        0, null, 0, null, null);
            //    return;
            //}
            

            if (_settings.IsSchedulerOuter)
            {
                _mainDbWorker.SaveNewProcessInDBLog(currentDateTime, _settings.SchedulerOuterSettings.DBLog.DBLogPrefix, out _processId);
            }

            _mainProgressManager.SetProcessId(_processId);

            var progressData = new ExportProgressDataOuter(ExportProgressDataLevel.STAGESTARTINFO, ExportProgressDataStageOuter.PROCESS_MAIN);
            progressData.SetTextAddInfo("SCHEMAS_TO_WORK", schemasToWork);
            _mainProgressManager.ReportCurrentProgress(progressData);
        }

        public void EndProcess(string dbLogPrefix, ExportProgressDataOuter progressData)
        {
            _mainDbWorker.UpdateProcessInDBLog(DateTime.Now, dbLogPrefix, progressData);
        }


        public List<SchemaWorkAggrFullStatOuter> GetAggrFullStat(List<ConnectionOuterToProcess> scheduledConnections, int getStatForLastDays, string prefix)
        {
            //TODO
            var plainStat = _mainDbWorker.GetStat(getStatForLastDays, prefix);
            var appWorkStat = _mainDbWorker.GetAppWorkStat(getStatForLastDays, prefix);
            var commitStat = _mainDbWorker.GetCommitStat(getStatForLastDays, prefix);
            return SchemaWorkAggrFullStatOuter.GetAggrFullStat(plainStat, appWorkStat, commitStat, scheduledConnections, getStatForLastDays);
            //return new List<SchemaWorkAggrFullStatOuter>();
        }

        static PrognozBySchemaOuter getStatForSchema(List<SchemaWorkAggrFullStatOuter> statInfo, string dbid, string userName, string dblink,
            int minSuccessResultsForPrognoz, int? intervalForSearch)
        {
            var schemaStat = statInfo.Where(c =>
                (userName is null || c.UserName.ToUpper() == userName.ToUpper()) &&
                (dblink is null || c.DbLink.ToUpper() == dblink.ToUpper()) &&
                (dbid is null || c.DBId.ToUpper() == dbid.ToUpper()) &&
                ((intervalForSearch is null && c.SuccessLaunchesCount >= minSuccessResultsForPrognoz) ||
                 (intervalForSearch == SchemaWorkAggrFullStatOuter.Interval7 && c.SuccessLaunchesCount7 >= minSuccessResultsForPrognoz) ||
                 (intervalForSearch == SchemaWorkAggrFullStatOuter.Interval30 && c.SuccessLaunchesCount30 >= minSuccessResultsForPrognoz) ||
                 (intervalForSearch == SchemaWorkAggrFullStatOuter.Interval90 && c.SuccessLaunchesCount90 >= minSuccessResultsForPrognoz))).ToList();
            if (schemaStat.Any())
            {
                var res = new PrognozBySchemaOuter();
                if (intervalForSearch == SchemaWorkAggrFullStatOuter.Interval7)
                {
                    res.OjectsCount = schemaStat.Average(c => c.AvgSuccessLaunchAllObjectsFactCount7 ?? 0);
                    res.DurationsInMinutes = schemaStat.Average(c => c.AvgSuccessLaunchDurationInMinutes7 ?? 0);
                }
                else if (intervalForSearch == SchemaWorkAggrFullStatOuter.Interval30)
                {
                    res.OjectsCount = schemaStat.Average(c => c.AvgSuccessLaunchAllObjectsFactCount30 ?? 0);
                    res.DurationsInMinutes = schemaStat.Average(c => c.AvgSuccessLaunchDurationInMinutes30 ?? 0);
                }
                else if (intervalForSearch == SchemaWorkAggrFullStatOuter.Interval90)
                {
                    res.OjectsCount = schemaStat.Average(c => c.AvgSuccessLaunchAllObjectsFactCount90 ?? 0);
                    res.DurationsInMinutes = schemaStat.Average(c => c.AvgSuccessLaunchDurationInMinutes90 ?? 0);
                }
                else
                {
                    res.OjectsCount = schemaStat.Average(c => c.AvgSuccessLaunchAllObjectsFactCount ?? 0);
                    res.DurationsInMinutes = schemaStat.Average(c => c.AvgSuccessLaunchDurationInMinutes ?? 0);
                }

                return res;
            }
            return null;
        }

        public static PrognozBySchemaOuter SelectConnectionToProcess(List<SchemaWorkAggrFullStatOuter> statInfo, int minSuccessResultsForPrognoz)
        {
            PrognozBySchemaOuter res = null;

            var connToProcess = new List<SchemaWorkAggrFullStatOuter>();
            foreach (var stat in statInfo)
            {
                var connToDo = true;
                if (!stat.IsScheduled)
                {
                    connToDo = false;
                }
                else if (stat.OneTimePerHoursPlan!=null && stat.OneTimePerHoursPlan > 0)
                {
                    if (stat.LastSuccessLaunchFactTime != null)
                    {
                        var durationFromLastSuccess = DateTime.Now - stat.LastSuccessLaunchFactTime.Value;
                        if (TimeSpan.FromHours(stat.OneTimePerHoursPlan.Value) > durationFromLastSuccess)
                            connToDo = false;
                    }
                }
                if (connToDo)
                    connToProcess.Add(stat);
            }

            var statItem = connToProcess.FirstOrDefault();
            if (statItem != null)
            {
                PrognozBySchemaOuter newItem;

                //пытаемся найти подходящую статистику за 7, 30, 90 или максимальный доступный интервал
                //сначала для схемы, затем для БД, затем любую
                newItem = getStatForSchema(statInfo, statItem.DBId, statItem.UserName, statItem.DbLink,
                    minSuccessResultsForPrognoz, SchemaWorkAggrFullStatOuter.Interval7);
                if (newItem == null)
                    newItem = getStatForSchema(statInfo, statItem.DBId, statItem.UserName,statItem.DbLink,
                        minSuccessResultsForPrognoz, SchemaWorkAggrFullStatOuter.Interval30);
                if (newItem == null)
                    newItem = getStatForSchema(statInfo, statItem.DBId, statItem.UserName,statItem.DbLink,
                        minSuccessResultsForPrognoz, SchemaWorkAggrFullStatOuter.Interval90);
                if (newItem == null)
                    newItem = getStatForSchema(statInfo, statItem.DBId, statItem.UserName,statItem.DbLink,
                        minSuccessResultsForPrognoz, null);

                newItem = getStatForSchema(statInfo, statItem.DBId, statItem.UserName, null,
                    minSuccessResultsForPrognoz, SchemaWorkAggrFullStatOuter.Interval7);
                if (newItem == null)
                    newItem = getStatForSchema(statInfo, statItem.DBId, statItem.UserName,null,
                        minSuccessResultsForPrognoz, SchemaWorkAggrFullStatOuter.Interval30);
                if (newItem == null)
                    newItem = getStatForSchema(statInfo, statItem.DBId, statItem.UserName,null,
                        minSuccessResultsForPrognoz, SchemaWorkAggrFullStatOuter.Interval90);
                if (newItem == null)
                    newItem = getStatForSchema(statInfo, statItem.DBId, statItem.UserName,null,
                        minSuccessResultsForPrognoz, null);

                if (newItem == null)
                    newItem = getStatForSchema(statInfo, statItem.DBId, null, null,
                        minSuccessResultsForPrognoz, SchemaWorkAggrFullStatOuter.Interval7);
                if (newItem == null)
                    newItem = getStatForSchema(statInfo, statItem.DBId, null, null,
                        minSuccessResultsForPrognoz, SchemaWorkAggrFullStatOuter.Interval30);
                if (newItem == null)
                    newItem = getStatForSchema(statInfo, statItem.DBId, null, null,
                        minSuccessResultsForPrognoz, SchemaWorkAggrFullStatOuter.Interval90);
                if (newItem == null)
                    newItem = getStatForSchema(statInfo, statItem.DBId, null, null,
                        minSuccessResultsForPrognoz, null);

                if (newItem == null)
                    newItem = getStatForSchema(statInfo, null, null, null,
                        minSuccessResultsForPrognoz, SchemaWorkAggrFullStatOuter.Interval7);
                if (newItem == null)
                    newItem = getStatForSchema(statInfo, null, null, null,
                        minSuccessResultsForPrognoz, SchemaWorkAggrFullStatOuter.Interval30);
                if (newItem == null)
                    newItem = getStatForSchema(statInfo, null, null, null,
                        minSuccessResultsForPrognoz, SchemaWorkAggrFullStatOuter.Interval90);
                if (newItem == null)
                    newItem = getStatForSchema(statInfo, null, null, null,
                        minSuccessResultsForPrognoz, null);

                //если что-то нашли в статистике
                if (newItem != null)
                    res = newItem;
                else
                    res = new PrognozBySchemaOuter();

                res.DbId = statItem.DBId.ToUpper();
                res.UserName = statItem.UserName.ToUpper();
                res.DbLink = statItem.DbLink.ToUpper();
                //res.DbFolder = statItem.DbFolder.ToUpper();
            }
            return res;
        }
    }
}