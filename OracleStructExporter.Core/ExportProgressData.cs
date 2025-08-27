using System;

namespace OracleStructExporter.Core
{

    public class ExportProgressData
    {
        public string ObjectName { get; set; }
        public int Current { get; set; }
        public int TotalObjects { get; set; }

        public string Error { get; set; }
        public string ErrorDetails { get; set; }
        public string TextAddInfo { get; set; }
        public int ObjectNumAddInfo { get; set; }

        public string ProcessId { get; set; }
        
        public Connection CurrentConnection { get; set; }

        public int AllProcessErrorsCount { get; internal set; }


        public string Message
        {
            get
            {
                var objectAddStr = string.IsNullOrWhiteSpace(ObjectName) ? "" : $" при обработке {ObjectName}";
                if (Level == ExportProgressDataLevel.ERROR) return $"Ошибка{objectAddStr}! {Error}";
                if (Level == ExportProgressDataLevel.CANCEL) return "Операция отменена пользователем!";

                if (Stage == ExportProgressDataStage.PROCESS_SCHEMA)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Выгрузка объектов схемы {TextAddInfo}...";
                    var errorsAddStr = "";
                    if (AllProcessErrorsCount > 0)
                        errorsAddStr = $". Ошибок: {AllProcessErrorsCount}";
                    return $"Объекты схемы {TextAddInfo} ({ObjectNumAddInfo} шт.) выгружены{errorsAddStr}{DurationString}";
                }
                if (Stage == ExportProgressDataStage.PROCESS_OBJECT_TYPE)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Выгрузка объектов типа {TextAddInfo}...";
                    return $"Объекты типа {TextAddInfo} ({ObjectNumAddInfo} шт.) выгружены{DurationString}";
                }
                if (Stage == ExportProgressDataStage.GET_INFO_ABOUT_SYS_VIEW)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return "Получение информации о системных представлениях...";
                    return $"Информация о системных представлениях ({ObjectNumAddInfo} шт.) получена. {TextAddInfo}{DurationString}";
                }

                if (Stage == ExportProgressDataStage.GET_OBJECTS_NAMES)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о списке объектов схемы...";
                    return $"Информация о списке объектов схемы ({ObjectNumAddInfo} шт.) получена {DurationString}";
                }
                if (Stage == ExportProgressDataStage.GET_GRANTS)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о грантах...";
                    return $"Информация о грантах ({ObjectNumAddInfo} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStage.GET_COLUMNS)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о столбцах...";
                    return $"Информация о столбцах ({ObjectNumAddInfo} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStage.GET_COLUMNS_COMMENTS)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о комментариях на столбцы...";
                    return $"Информация о комментариях на столбцы ({ObjectNumAddInfo} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStage.GET_SYNONYMS)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о синонимах...";
                    return $"Информация о синонимах ({ObjectNumAddInfo} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStage.GET_SEQUENCES)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о sequences...";
                    return $"Информация о sequences ({ObjectNumAddInfo} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStage.GET_SCHEDULER_JOBS)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о scheduler_jobs...";
                    return $"Информация о scheduler_jobs ({ObjectNumAddInfo} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStage.GET_DMBS_JOBS)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о dbms_jobs...";
                    return $"Информация о dbms_jobs ({ObjectNumAddInfo} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStage.GET_PACKAGES_HEADERS)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о заголовках пакетов...";
                    return $"Информация о заголовках пакетов ({ObjectNumAddInfo} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStage.GET_PACKAGES_BODIES)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о телах пакетов...";
                    return $"Информация о телах пакетов ({ObjectNumAddInfo} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStage.GET_FUNCTIONS)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о функциях...";
                    return $"Информация о функциях ({ObjectNumAddInfo} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStage.GET_PROCEDURES)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о процедурах...";
                    return $"Информация о процедурах ({ObjectNumAddInfo} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStage.GET_TRIGGERS)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о триггерах...";
                    return $"Информация о триггерах ({ObjectNumAddInfo} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStage.GET_TYPES)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о типах...";
                    return $"Информация о типах ({ObjectNumAddInfo} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStage.GET_TABLE_CONSTRAINTS)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о ключах таблиц...";
                    return $"Информация о ключах таблиц ({ObjectNumAddInfo} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStage.GET_TABLES_STRUCTS)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о таблицах...";
                    return $"Информация о таблицах ({ObjectNumAddInfo} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStage.GET_TABLES_INDEXES)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации об индексах таблиц...";
                    return $"Информация об индексах таблиц ({ObjectNumAddInfo} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStage.GET_TABLES_COMMENTS)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о комментариях на таблицы...";
                    return $"Информация о комментариях на таблицы ({ObjectNumAddInfo} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStage.GET_TABLES_PARTS)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о партициях таблиц...";
                    return $"Информация о партициях таблиц ({ObjectNumAddInfo} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStage.GET_VIEWS)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о представлениях...";
                    return $"Информация о представлениях ({ObjectNumAddInfo} шт.) получена{DurationString}";
                }
                if (Stage == ExportProgressDataStage.GET_VIEWS_COMMENTS)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение информации о комментариях на представления...";
                    return $"Информация о комментариях на представления ({ObjectNumAddInfo} шт.) получена{DurationString}";
                }

                if (Stage == ExportProgressDataStage.PROCESS_OBJECT)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Выгрузка {ObjectName}...";
                    return $"{ObjectName} выгружен{DurationString}";
                }
                if (Stage == ExportProgressDataStage.GET_DBLINK)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение текста dblink {ObjectName}...";
                    return $"Текст dblink {ObjectName} получен{DurationString}";
                }
                if (Stage == ExportProgressDataStage.GET_UNKNOWN_OBJECT_DDL)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Получение текста объекта {ObjectName}...";
                    return $"Текст объекта {ObjectName} получен{DurationString}";
                }

                if (Stage == ExportProgressDataStage.MOVE_FILES_TO_ERROR_DIR)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Перемещение файлов папку err...";
                    return $"Файлы в err ({ObjectNumAddInfo} шт.) перемещены{DurationString}";
                }
                if (Stage == ExportProgressDataStage.MOVE_FILES_TO_MAIN_DIR)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Перемещение файлов папку main...";
                    return $"Файлы в main ({ObjectNumAddInfo} шт.) перемещены{DurationString}";
                }
                if (Stage == ExportProgressDataStage.SEARCH_AND_DELETE_DUPLICATES_IN_MAIN_DIR)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Поиск и удаление дубликатов в main...";
                    if (ObjectNumAddInfo>0)
                        return $"Дубликаты ({ObjectNumAddInfo} шт.) в папке {TextAddInfo} удалены{DurationString}";
                    return $"Дубликаты не найдены{DurationString}";
                }
                if (Stage == ExportProgressDataStage.COPY_FILES_TO_REPO_DIR)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Копирование файлов папку repo...";
                    return $"Файлы в repo ({ObjectNumAddInfo} шт.) скопированы{DurationString}";
                }
                if (Stage == ExportProgressDataStage.CREATE_AND_SEND_COMMIT_TO_GIT)
                {
                    if (Level == ExportProgressDataLevel.STAGESTARTINFO)
                        return $"Создание и отправка commit в git...";
                    return $"Commit создан и отправлен в git{DurationString}";
                }

                return TextAddInfo;
            }
        }
    
        public bool ProcessFinished { get; set; }

        public DateTime EventTime { get; set; }
        public string EventId { get; set; }

        public ExportProgressDataLevel Level { get; set; }
        public ExportProgressDataStage Stage { get; set; }


        public ExportProgressData StartStageProgressData { get; set; }


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
                return $" ({FormatTimeSpan(Duration, true)})";
            }
        }

        static string FormatTimeSpan(TimeSpan val, bool alwaysShowMilliseconds)
        {
            if (val.TotalMilliseconds < 1000)
            {
                return $"{val.Milliseconds} мсек";
            }

            if (val.TotalMinutes < 1)
            {
                return alwaysShowMilliseconds
                    ? $"{val.Seconds} сек {val.Milliseconds} мсек"
                    : $"{val.Seconds} сек";
            }

            if (val.TotalHours < 1)
            {
                return alwaysShowMilliseconds
                    ? $"{val.Minutes} мин {val.Seconds} сек {val.Milliseconds} мсек"
                    : $"{val.Minutes} мин {val.Seconds} сек";
            }

            int totalHours = (int)val.TotalHours;
            return alwaysShowMilliseconds
                ? $"{totalHours} час {val.Minutes} мин {val.Seconds} сек {val.Milliseconds} мсек"
                : $"{totalHours} час {val.Minutes} мин {val.Seconds} сек";
        }



        public ExportProgressData()
        {
            EventTime = DateTime.Now;
            EventId = Guid.NewGuid().ToString();
        }

        public ExportProgressData(ExportProgressDataLevel level, ExportProgressDataStage stage, string objectName, int current, int totalObjects, bool processFinished, string textAddInfo, int objectNumAddInfo, string processId, Connection currentConnection, string error, string errorDetails) : this()
        {
            Level = level;
            Stage = stage;
            ObjectName = objectName;
            Current = current;
            TotalObjects = totalObjects;
            ProcessFinished = processFinished;
            TextAddInfo = textAddInfo;
            ObjectNumAddInfo = objectNumAddInfo;
            Error = error;
            ErrorDetails = errorDetails;
            ProcessId = processId;
            CurrentConnection = currentConnection;
            //DBId = dbId;
            //UserName = userName;
        }
    }
}
