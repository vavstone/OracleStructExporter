namespace OracleStructExporter.Core
{
    public enum ExportProgressDataStage
    {
        /// <summary>
        /// Не использовать при создании события, техническое значение
        /// </summary>
        NONE = 0,
        PROCESS_MAIN = 1,
        PROCESS_SCHEMA = 2,
        PROCESS_OBJECT_TYPE = 3,
        PROCESS_OBJECT = 4,

        GET_GRANTS = 5,
        GET_COLUMNS = 6,
        GET_COLUMNS_COMMENTS = 7,
        GET_SYNONYMS = 8,
        GET_SEQUENCES = 9,
        GET_PACKAGES_HEADERS = 10,
        GET_PACKAGES_BODIES = 11,
        GET_FUNCTIONS = 12,
        GET_PROCEDURES = 12,
        GET_TRIGGERS = 13,
        GET_TYPES = 14,
        GET_TABLE_CONSTRAINTS = 16,
        GET_TABLES_STRUCTS = 17,
        GET_TABLES_INDEXES = 18,
        GET_TABLES_COMMENTS = 19,
        GET_TABLES_PARTS = 20,
        GET_VIEWS = 21,
        GET_VIEWS_COMMENTS = 22,
        
        GET_OBJECTS_NAMES = 23,

        GET_INFO_ABOUT_SYS_VIEW = 24,
        GET_SCHEDULER_JOBS = 25,
        GET_DMBS_JOBS = 26,
        GET_DBLINK = 27,
        GET_UNKNOWN_OBJECT_DDL = 28,
        UNPLANNED_EXIT = 29,

        MOVE_FILES_TO_ERROR_DIR = 30,
        MOVE_FILES_TO_MAIN_DIR = 31,
        SEARCH_AND_DELETE_DUPLICATES_IN_MAIN_DIR = 32,
        COPY_FILES_TO_REPO_DIR = 33,
        CREATE_AND_SEND_COMMIT_TO_GIT = 34

    }
}