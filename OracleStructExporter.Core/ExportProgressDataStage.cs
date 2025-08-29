namespace OracleStructExporter.Core
{
    public enum ExportProgressDataStage
    {
        /// <summary>
        /// Не использовать при создании события, техническое значение
        /// </summary>
        NONE = 0,
        PROCESS_MAIN = 1,
        /*START:
        ProcessObjCountPlan = 0;
        SchemaObjCountPlan = 0;
        TypeObjCountPlan = 0;
        Current = 0;
        END:
        ProcessObjCountPlan = X;
        SchemaObjCountPlan = 0;
        TypeObjCountPlan = 0;
        ProcessObjCountFact = X;
        SchemaObjCountFact = 0;
        TypeObjCountFact = 0;
        MetaObjCountFact = 0;
        ErrorsCount = X;
        Current = 0;*/

        PROCESS_SCHEMA = 2,
        /*START:
        ProcessObjCountPlan = 0;
        SchemaObjCountPlan = 0;
        TypeObjCountPlan = 0;
        Current = 0;
        END:
        ProcessObjCountPlan = 0;
        SchemaObjCountPlan = X;
        TypeObjCountPlan = 0;
        ProcessObjCountFact = 0;
        SchemaObjCountFact = X;
        TypeObjCountFact = 0;
        MetaObjCountFact = 0;
        ErrorsCount = X;
        Current = X;*/

        PROCESS_OBJECT_TYPE = 3,
        /*START:
        ProcessObjCountPlan = 0;
        SchemaObjCountPlan = X;
        TypeObjCountPlan = X;
        Current = X;
        END:
        ProcessObjCountPlan = 0;
        SchemaObjCountPlan = X;
        TypeObjCountPlan = X;
        ProcessObjCountFact = 0;
        SchemaObjCountFact = 0;
        TypeObjCountFact = X;
        MetaObjCountFact = 0;
        ErrorsCount = X;
        Current = X;*/

        PROCESS_OBJECT = 4,
        /*START:
        ProcessObjCountPlan = 0;
        SchemaObjCountPlan = X;
        TypeObjCountPlan = X;
        Current = X;
        END:
        ProcessObjCountPlan = 0;
        SchemaObjCountPlan = X;
        TypeObjCountPlan = X;
        ProcessObjCountFact = 0;
        SchemaObjCountFact = 0;
        TypeObjCountFact = 0;
        MetaObjCountFact = 0;
        ErrorsCount = X;
        Current = X;*/

        GET_GRANTS = 5,
        GET_COLUMNS = 6,
        GET_COLUMNS_COMMENTS = 7,
        /*START:
        ProcessObjCountPlan = 0;
        SchemaObjCountPlan = X;
        TypeObjCountPlan = 0;
        Current = 0;
        END:
        ProcessObjCountPlan = 0;
        SchemaObjCountPlan = X;
        TypeObjCountPlan = 0;
        ProcessObjCountFact = 0;
        SchemaObjCountFact = 0;
        TypeObjCountFact = 0;
        MetaObjCountFact = X;
        ErrorsCount = X;
        Current = 0;*/

        GET_SYNONYMS = 8,
        GET_SEQUENCES = 9,
        GET_PACKAGES_HEADERS = 10,
        GET_PACKAGES_BODIES = 11,
        GET_FUNCTIONS = 12,
        GET_PROCEDURES = 13,
        GET_TRIGGERS = 14,
        GET_TYPES = 15,
        GET_TABLE_CONSTRAINTS = 16,
        GET_TABLES_STRUCTS = 17,
        GET_TABLES_INDEXES = 18,
        GET_TABLES_COMMENTS = 19,
        GET_TABLES_PARTS = 20,
        GET_VIEWS = 21,
        GET_VIEWS_COMMENTS = 22,
        GET_SCHEDULER_JOBS = 25,
        GET_DMBS_JOBS = 26,
        /*START:
        ProcessObjCountPlan = 0;
        SchemaObjCountPlan = X;
        TypeObjCountPlan = X;
        Current = X;
        END:
        ProcessObjCountPlan = 0;
        SchemaObjCountPlan = X;
        TypeObjCountPlan = X;
        ProcessObjCountFact = 0;
        SchemaObjCountFact = 0;
        TypeObjCountFact = 0;
        MetaObjCountFact = X;
        ErrorsCount = X;
        Current = X;*/

        GET_OBJECTS_NAMES = 23,
        /*START:
        ProcessObjCountPlan = 0;
        SchemaObjCountPlan = 0;
        TypeObjCountPlan = 0;
        Current = 0;
        END:
        ProcessObjCountPlan = 0;
        SchemaObjCountPlan = X;
        TypeObjCountPlan = 0;
        ProcessObjCountFact = 0;
        SchemaObjCountFact = 0;
        TypeObjCountFact = 0;
        MetaObjCountFact = X;
        ErrorsCount = X;
        Current = 0;*/


        GET_INFO_ABOUT_SYS_VIEW = 24,
        /*START:
        ProcessObjCountPlan = 0;
        SchemaObjCountPlan = 0;
        TypeObjCountPlan = 0;
        Current = 0;
        END:
        ProcessObjCountPlan = 0;
        SchemaObjCountPlan = 0;
        TypeObjCountPlan = 0;
        ProcessObjCountFact = 0;
        SchemaObjCountFact = 0;
        TypeObjCountFact = 0;
        MetaObjCountFact = X;
        ErrorsCount = X;
        Current = 0;*/



        GET_DBLINK = 27,
        GET_UNKNOWN_OBJECT_DDL = 28,
        /*START:
        ProcessObjCountPlan = 0;
        SchemaObjCountPlan = X;
        TypeObjCountPlan = X;
        Current = X;
        END:
        ProcessObjCountPlan = 0;
        SchemaObjCountPlan = X;
        TypeObjCountPlan = X;
        ProcessObjCountFact = 0;
        SchemaObjCountFact = 0;
        TypeObjCountFact = 0;
        MetaObjCountFact = X;
        ErrorsCount = X;
        Current = X;*/



        //UNPLANNED_EXIT = 29,
        
        MOVE_FILES_TO_ERROR_DIR = 29,
        MOVE_FILES_TO_MAIN_DIR = 30,
        SEARCH_AND_DELETE_DUPLICATES_IN_MAIN_DIR = 31,
        COPY_FILES_TO_REPO_DIR = 32,
        CREATE_AND_SEND_COMMIT_TO_GIT = 33
        /*START:
        ProcessObjCountPlan = 0;
        SchemaObjCountPlan = X;
        TypeObjCountPlan = 0;
        Current = X;
        END:
        ProcessObjCountPlan = 0;
        SchemaObjCountPlan = X;
        TypeObjCountPlan = 0;
        ProcessObjCountFact = 0;
        SchemaObjCountFact = X;
        TypeObjCountFact = 0;
        MetaObjCountFact = X;
        ErrorsCount = X;
        Current = X;*/
    }
}