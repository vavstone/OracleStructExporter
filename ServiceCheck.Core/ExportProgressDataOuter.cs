using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceCheck.Core
{
    public class ExportProgressDataOuter
    {
        public string ProcessId { get; set; }
        public Connection CurrentConnection { get; set; }

        public string ObjectName { get; set; }
        public string ObjectType { get; set; }
        
        
        //public int TotalObjects { get; set; }
        public int? ProcessObjCountPlan { get; set; }
        public int? SchemaObjCountPlan { get; set; }
        public int? TypeObjCountPlan { get; set; }
        public int? Current { get; set; }
        public int? ProcessObjCountFact { get; set; }
        public int? SchemaObjCountFact { get; set; }
        public int? TypeObjCountFact { get; set; }
        public int? MetaObjCountFact { get; set; }
        public int? ErrorsCount { get; set; }

        public List<RepoChangeItem> RepoChangesPlainList
        {
            get
            {
                return GetAddInfo<List<RepoChangeItem>>("REPO_CHANGES");
            }
        }

        public List<RepoChangeDbSchemaGroupInfo> RepoChanges
        {
            get
            {
                var res = new List<RepoChangeDbSchemaGroupInfo>();
                var items = RepoChangesPlainList;
                if (items != null && items.Any(c=>!c.MaskWorked))
                {
                    foreach (var dbIdGroup in items.Where(c=>!c.MaskWorked).GroupBy(c => c.DBId))
                    {
                        var dbId = dbIdGroup.Key;
                        foreach (var dbUserNameGroup in dbIdGroup.GroupBy(c => c.UserName))
                        {
                            var resItem = new RepoChangeDbSchemaGroupInfo
                            {
                                DBId = dbId,
                                UserName = dbUserNameGroup.Key
                            };
                            res.Add(resItem);
                            foreach (var dbCommitGroup in dbUserNameGroup.GroupBy((c =>
                                         new Tuple<int, DateTime, bool>(c.ProcessId, c.CommitCommonDate, c.IsInitial))))
                            {
                                var commitItem = new RepoChangeCommitGroupInfo
                                {
                                    ProcessId = dbCommitGroup.Key.Item1,
                                    CommitCommonDate = dbCommitGroup.Key.Item2,
                                    IsInitial = dbCommitGroup.Key.Item3
                                };
                                resItem.CommitsList.Add(commitItem);

                                foreach (var objAndOperGroup in dbCommitGroup.GroupBy(c =>
                                             new Tuple<OracleObjectType, RepoOperation>(c.ObjectType, c.Operation)))
                                {
                                    var objItem = new RepoChangeObjAndOperGroupInfo
                                    {
                                        ObjectType = objAndOperGroup.Key.Item1,
                                        Operation = objAndOperGroup.Key.Item2,
                                        ChangesCount = objAndOperGroup.Count(),
                                        FilesSize = objAndOperGroup.Sum(c => c.FileSize),
                                        FirstModificationTime = objAndOperGroup.Min(c=>c.CommitCurFileTime),
                                        LastModificationTime = objAndOperGroup.Max(c=>c.CommitCurFileTime)
                                    };
                                    commitItem.OperationsList.Add(objItem);
                                }
                            }
                        }
                    }

                }

                return res;
            }
        }

        //public int ObjectNumAddInfo { get; set; }
        //public int AllProcessErrorsCount { get; internal set; }

        public string Error { get; set; }
        public string ErrorDetails { get; set; }
        //public string TextAddInfo { get; set; }
        public Dictionary<string,string> textAddInfo { get; set; } = new Dictionary<string,string>();
        public Dictionary<string, object> addInfo { get; set; } = new Dictionary<string, object>();

        public void SetTextAddInfo(string key, string value)
        {
            textAddInfo[key]  = value;
        }

        public string GetTextAddInfo(string key)
        {
            if (key == null)
            {
                if (!textAddInfo.Any())
                    return textAddInfo[textAddInfo.Keys.First()];
                return null;
            }
            if (textAddInfo.ContainsKey(key))
                return textAddInfo[key];
            return null;
        }

        public void SetddInfo(string key, object value)
        {
            addInfo[key] = value;
        }

        public T GetAddInfo<T>(string key)
        {
            if (key == null)
            {
                foreach (var k in addInfo.Keys)
                {
                    if (addInfo[k] is T)
                        return (T)addInfo[k];
                }
            }
            if (addInfo.ContainsKey(key))
                return (T)addInfo[key];
            return default;
        }




        public string Message
        {
            get
            {
                var objectAddStr = string.IsNullOrWhiteSpace(ObjectName) ? "" : $" при обработке {ObjectName}";
                if (Level == ExportProgressDataLevel.ERROR) return $"Ошибка{objectAddStr}! {Error}";
                if (Level == ExportProgressDataLevel.CANCEL) return "Операция отменена пользователем!";
                if (Level == ExportProgressDataLevel.MOMENTALEVENTINFO) return GetTextAddInfo("MOMENTAL_INFO");


                if (Stage == ExportProgressDataStageOuter.PROCESS_MAIN)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Запуск работы по:{Environment.NewLine}{GetTextAddInfo("SCHEMAS_TO_WORK")}";
                    var errorsAddStr = "";
                    if (ErrorsCount > 0)
                        errorsAddStr = $"{Environment.NewLine}Ошибок: {ErrorsCount}";
                    var schemasAddStr = "";
                    var schemasSuccess = GetTextAddInfo("SCHEMAS_SUCCESS");
                    var schemasError = GetTextAddInfo("SCHEMAS_ERROR");
                    if (!string.IsNullOrEmpty(schemasSuccess))
                        schemasAddStr += $"{Environment.NewLine}Успешно: {schemasSuccess}.";
                    if (!string.IsNullOrEmpty(schemasError))
                    {
                        //if (!string.IsNullOrWhiteSpace(schemasAddStr))
                        //    schemasAddStr += " ";
                        schemasAddStr += $"{Environment.NewLine}Ошибки: {schemasError}.";
                    }

                    return $"Завершение работы.{schemasAddStr}{Environment.NewLine}Объекты схем ({ProcessObjCountFact} из {ProcessObjCountPlan}) выгружены{DurationString}.{errorsAddStr}";
                }

                if (Stage == ExportProgressDataStageOuter.PROCESS_SCHEMA)
                {
                    var connectAddStr = $"{CurrentConnection.UserName}@{CurrentConnection.DBIdC}";
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Выгрузка объектов схемы {connectAddStr}...";
                    var errorsAddStr = "";
                    if (ErrorsCount > 0)
                        errorsAddStr = $". Ошибок: {ErrorsCount}";
                    return $"Объекты схемы {connectAddStr} ({SchemaObjCountFact} из {SchemaObjCountPlan}) выгружены{errorsAddStr}{DurationString}";
                }
                if (Stage == ExportProgressDataStageOuter.PROCESS_OBJECT_TYPE)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Выгрузка объектов типа {ObjectType}...";
                    return $"Объекты типа {ObjectType} ({TypeObjCountFact} из {TypeObjCountPlan}) выгружены{DurationString}";
                }
                if (Stage == ExportProgressDataStageOuter.GET_INFO_ABOUT_SYS_VIEW)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return "Получение информации о системных представлениях...";
                    return $"Информация о системных представлениях ({MetaObjCountFact} шт.) получена. {GetTextAddInfo("SYSTEM_VIEW_INFO")}{DurationString}";
                }

                if (Stage == ExportProgressDataStageOuter.GET_OBJECTS_NAMES)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о списке объектов схемы...";
                    return $"Информация о списке объектов схемы ({MetaObjCountFact} шт.) получена {DurationString}";
                }
                if (Stage == ExportProgressDataStageOuter.GET_GRANTS)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о грантах...";
                    return $"Информация о грантах ({MetaObjCountFact} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStageOuter.GET_COLUMNS)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о столбцах...";
                    return $"Информация о столбцах ({MetaObjCountFact} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStageOuter.GET_COLUMNS_COMMENTS)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о комментариях на столбцы...";
                    return $"Информация о комментариях на столбцы ({MetaObjCountFact} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStageOuter.GET_SYNONYMS)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о синонимах...";
                    return $"Информация о синонимах ({MetaObjCountFact} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStageOuter.GET_SEQUENCES)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о sequences...";
                    return $"Информация о sequences ({MetaObjCountFact} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStageOuter.GET_SCHEDULER_JOBS)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о scheduler_jobs...";
                    return $"Информация о scheduler_jobs ({MetaObjCountFact} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStageOuter.GET_DMBS_JOBS)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о dbms_jobs...";
                    return $"Информация о dbms_jobs ({MetaObjCountFact} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStageOuter.GET_PACKAGES_HEADERS)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о заголовках пакетов...";
                    return $"Информация о заголовках пакетов ({MetaObjCountFact} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStageOuter.GET_PACKAGES_BODIES)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о телах пакетов...";
                    return $"Информация о телах пакетов ({MetaObjCountFact} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStageOuter.GET_FUNCTIONS)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о функциях...";
                    return $"Информация о функциях ({MetaObjCountFact} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStageOuter.GET_PROCEDURES)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о процедурах...";
                    return $"Информация о процедурах ({MetaObjCountFact} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStageOuter.GET_TRIGGERS)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о триггерах...";
                    return $"Информация о триггерах ({MetaObjCountFact} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStageOuter.GET_TYPES)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о типах...";
                    return $"Информация о типах ({MetaObjCountFact} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStageOuter.GET_TABLE_CONSTRAINTS)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о ключах таблиц...";
                    return $"Информация о ключах таблиц ({MetaObjCountFact} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStageOuter.GET_TABLES_STRUCTS)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о таблицах...";
                    return $"Информация о таблицах ({MetaObjCountFact} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStageOuter.GET_TABLES_INDEXES)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации об индексах таблиц...";
                    return $"Информация об индексах таблиц ({MetaObjCountFact} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStageOuter.GET_TABLES_COMMENTS)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о комментариях на таблицы...";
                    return $"Информация о комментариях на таблицы ({MetaObjCountFact} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStageOuter.GET_TABLES_PARTS)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о партициях таблиц...";
                    return $"Информация о партициях таблиц ({MetaObjCountFact} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStageOuter.GET_VIEWS)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о представлениях...";
                    return $"Информация о представлениях ({MetaObjCountFact} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStageOuter.GET_VIEWS_COMMENTS)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о комментариях на представления...";
                    return $"Информация о комментариях на представления ({MetaObjCountFact} шт.) получена{DurationString}";
                }

                if (Stage == ExportProgressDataStageOuter.PROCESS_OBJECT)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Выгрузка {ObjectName}...";
                    return $"{ObjectName} выгружен{DurationString}";
                }
                if (Stage == ExportProgressDataStageOuter.GET_DBLINK)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение текста dblink {ObjectName}...";
                    return $"Текст dblink {ObjectName} получен{DurationString}";
                }
                if (Stage == ExportProgressDataStageOuter.GET_UNKNOWN_OBJECT_DDL)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение текста объекта {ObjectName}...";
                    return $"Текст объекта {ObjectName} получен{DurationString}";
                }

                if (Stage == ExportProgressDataStageOuter.MOVE_FILES_TO_ERROR_DIR)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Перемещение файлов в папку err...";
                    return $"Файлы в err ({MetaObjCountFact} шт.) перемещены{DurationString}";
                }
                if (Stage == ExportProgressDataStageOuter.MOVE_FILES_TO_MAIN_DIR)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Перемещение файлов в папку main...";
                    return $"Файлы в main ({MetaObjCountFact} шт.) перемещены{DurationString}";
                }
                //if (Stage == ExportProgressDataStage.SEARCH_AND_DELETE_DUPLICATES_IN_MAIN_DIR)
                //{
                //    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                //        return $"Поиск и удаление дубликатов в main...";
                //    if (MetaObjCountFact > 0)
                //        return $"Дубликаты ({MetaObjCountFact} шт.) в папке {TextAddInfo} удалены{DurationString}";
                //    return $"Дубликаты не найдены{DurationString}";
                //}
                if (Stage == ExportProgressDataStageOuter.CREATE_SIMPLE_FILE_REPO_COMMIT)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Создание коммита в локальной файловой СКВ...";
                    return $"Коммит в локальной файловой СКВ создан ({MetaObjCountFact} изменений) {DurationString}";
                }
                //if (Stage == ExportProgressDataStage.CREATE_AND_SEND_GITLAB_COMMIT)
                //{
                //    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                //        return $"Создание и отправка commit в git...";
                //    return $"Commit создан и отправлен в git{DurationString}";
                //}

                return GetTextAddInfo(null);
            }
        }

        public bool ThreadFinished
        {
            get
            {
                return (Stage == ExportProgressDataStageOuter.PROCESS_SCHEMA && Level == ExportProgressDataLevel.STAGEENDINFO) || Level == ExportProgressDataLevel.CANCEL;
            }
        }

        public bool ProcessFinished
        {
            get
            {
                return (Stage == ExportProgressDataStageOuter.PROCESS_MAIN && Level == ExportProgressDataLevel.STAGEENDINFO) || Level == ExportProgressDataLevel.CANCEL;
            }
        }

        public bool IsProgressFromMainProcess
        {
            get
            {
                return (Stage == ExportProgressDataStageOuter.PROCESS_MAIN);
            } 
        
        }

        public bool IsEndOfSimpleRepoCreating
        {
            get
            {
                return (Stage == ExportProgressDataStageOuter.CREATE_SIMPLE_FILE_REPO_COMMIT && Level == ExportProgressDataLevel.STAGEENDINFO);
            }

        }

        public DateTime EventTime { get; set; }
        public string EventId { get; set; }

        public ExportProgressDataLevel Level { get; set; }
        public ExportProgressDataStageOuter Stage { get; set; }


        public ExportProgressDataOuter StartStageProgressData { get; set; }


        public TimeSpan Duration
        {
            get
            {
                if (Level != ExportProgressDataLevel.STAGEENDINFO || StartStageProgressData == null) return TimeSpan.Zero;
                return EventTime - StartStageProgressData.EventTime;
            }
        }

        public string DurationString
        {
            get
            {
                if (Level != ExportProgressDataLevel.STAGEENDINFO || StartStageProgressData == null) return string.Empty;
                return $" ({Duration.ToStringFormat(true)})";
            }
        }

        public ExportProgressDataOuter()
        {
            EventTime = DateTime.Now;
            EventId = Guid.NewGuid().ToString();
        }

        public ExportProgressDataOuter(ExportProgressDataLevel level, ExportProgressDataStageOuter stage) : this()
        {
            Level = level;
            Stage = stage;
            if (level == ExportProgressDataLevel.ERROR)
                ErrorsCount = 1;
        }
    }
}
