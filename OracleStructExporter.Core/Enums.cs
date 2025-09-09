namespace OracleStructExporter.Core
{
    public enum RepoOperation
    {
        ADD = 0,
        UPD = 1,
        DEL = 2
    }

    public enum OracleObjectType
    {
        UNKNOWN = 0,
        DBLINK = 1,
        DBMS_JOB = 2,
        FUNCTION = 3,
        PACKAGE = 4,
        PROCEDURE = 5,
        SCHEDULER_JOB = 6,
        SEQUENCE = 7,
        SYNONYM = 8,
        TABLE = 9,
        TRIGGER = 10,
        TYPE = 11,
        VIEW = 12
    }
}
