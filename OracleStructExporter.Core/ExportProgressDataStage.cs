namespace OracleStructExporter.Core
{
    public enum ExportProgressDataStage
    {
        /// <summary>
        /// Не использовать при создании события, техническое значение
        /// </summary>
        NONE = 0,

        PROCESS_SCHEMA = 1,
        PROCESS_OBJECT_TYPE = 2,
        PROCESS_OBJECT = 3,

        GET_GRANTS = 4,
        GET_COLUMNS = 5,
        GET_COLUMNS_COMMENTS = 6,
        GET_SYNONYMS = 7,
        GET_SEQUENCES = 8,
        GET_PACKAGES_HEADERS = 9,
        GET_PACKAGES_BODIES = 10,
        GET_FUNCTIONS = 11,
        GET_PROCEDURES = 12,
        GET_TRIGGERS = 13,
        GET_TYPES = 14,
        GET_TABLE_CONSTRAINTS = 15,
        GET_TABLES_STRUCTS = 16,
        GET_TABLES_INDEXES = 17,
        GET_TABLES_COMMENTS = 18,
        GET_TABLES_PARTS = 19,
        GET_VIEWS = 20,
        GET_VIEWS_COMMENTS = 21,
        
        GET_OBJECTS_NAMES = 22,

        GET_INFO_ABOUT_SYS_VIEW = 23,
        GET_SCHEDULER_JOBS = 24,
        GET_DMBS_JOBS = 25,
        GET_DBLINK = 26,
        GET_UNKNOWN_OBJECT_DDL = 27,
        UNPLANNED_EXIT = 28,

        MOVE_FILES_TO_ERROR_DIR = 29,
        MOVE_FILES_TO_MAIN_DIR = 30,
        SEARCH_AND_DELETE_DUPLICATES_IN_MAIN_DIR = 31,
        COPY_FILES_TO_REPO_DIR = 32,
        CREATE_AND_SEND_COMMIT_TO_GIT = 33


    }
}