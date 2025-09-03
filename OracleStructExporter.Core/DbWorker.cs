using OracleStructExporter.Core.DBStructs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OracleClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace OracleStructExporter.Core
{
    public class DbWorker
    {
        private OracleConnection _connection;
        public string ConnectionString { get; private set; }
        private ProgressDataManager _progressDataManager;
        private CancellationToken _cancellationToken;
        private string _objectNameMask;


        public DbWorker(OracleConnection connection, ProgressDataManager progressDataManager, string objectNameMask/*, CancellationToken cancellationToken*/)
        {
            _connection = connection;
            _progressDataManager = progressDataManager;
            //_cancellationToken = cancellationToken;
            _objectNameMask = objectNameMask;
        }

        public DbWorker(string connectionString, ProgressDataManager progressDataManager, string objectNameMask/*, CancellationToken cancellationToken*/)
        {
            ConnectionString = connectionString;
            _progressDataManager = progressDataManager;
            //_cancellationToken = cancellationToken;
            _objectNameMask = objectNameMask;
        }

        public void SetCancellationToken(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }


        public List<ObjectTypeNames> GetObjectsNames(List<string> objectTypesList, ExportProgressDataStage stage, out bool canceledByUser)
        {
            
            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressData(ExportProgressDataLevel.CANCEL, stage);
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            List<ObjectTypeNames> res = new List<ObjectTypeNames>();
            
            var progressData = new ExportProgressData(ExportProgressDataLevel.STAGESTARTINFO, stage);
            _progressDataManager.ReportCurrentProgress(progressData);
            
            var counter = 0;
            foreach (var objectType in objectTypesList)
            {
                try
                {
                    var items = new List<string>();
                    string objectQuery = GetObjectQuery(objectType, _objectNameMask);
                    using (OracleCommand cmd = new OracleCommand(objectQuery, _connection))
                    {
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                items.Add(reader[0].ToString());
                                counter++;
                            }
                        }
                    }
                    res.Add(new ObjectTypeNames { ObjectType = objectType, ObjectNames = items});
                }
                catch (Exception e)
                {
                    var progressDataErr = new ExportProgressData(ExportProgressDataLevel.ERROR, stage);
                    progressDataErr.Error = e.Message;
                    progressDataErr.ErrorDetails = e.StackTrace;
                    _progressDataManager.ReportCurrentProgress(progressDataErr);
                }
            }

            var progressData2 = new ExportProgressData(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.SchemaObjCountPlan = counter;
            progressData2.MetaObjCountFact = counter;
            _progressDataManager.ReportCurrentProgress(progressData2);

            return res;
        }

        // Сопоставление типов объектов с их представлениями в БД
        static readonly Dictionary<string, string> objectTypeMapping = new Dictionary<string, string>
        {
            {"FUNCTIONS", "FUNCTION"},
            {"PACKAGES", "PACKAGE"},
            {"PROCEDURES", "PROCEDURE"},
            {"SEQUENCES", "SEQUENCE"},
            {"SYNONYMS", "SYNONYM"},
            {"TABLES", "TABLE"},
            {"TRIGGERS", "TRIGGER"},
            {"TYPES", "TYPE"},
            {"VIEWS", "VIEW"},
            {"JOBS", "JOB"},
            {"DBLINKS", "DB_LINK"}
        };

        public static string GetObjectTypeName(string objectTypeCommonName)
        {
            return objectTypeMapping[objectTypeCommonName];
        }

        public string GetObjectSource(string objectName, string objectType, List<string> addSlashTo, ExportProgressDataStage stage, int schemaObjCountPlan, int typeObjCountPlan, int current,out bool canceledByUser)
        {
            string dbObjectType = GetObjectTypeName(objectType);
            const string sourceQuery = @"
                SELECT text 
                FROM user_source 
                WHERE name = :objectName 
                    AND type = :objectType 
                ORDER BY line";
            StringBuilder sourceCode = new StringBuilder();

            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressData(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.SchemaObjCountPlan = schemaObjCountPlan;
                progressDataCancel.TypeObjCountPlan = typeObjCountPlan;
                progressDataCancel.Current = current;
                progressDataCancel.ObjectName = objectName;
                progressDataCancel.ObjectType = objectType;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressData(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.SchemaObjCountPlan = schemaObjCountPlan;
            progressData.TypeObjCountPlan = typeObjCountPlan;
            progressData.Current = current;
            progressData.ObjectName = objectName;
            progressData.ObjectType = objectType;
            _progressDataManager.ReportCurrentProgress(progressData);

            try
            {
                using (OracleCommand cmd = new OracleCommand(sourceQuery, _connection))
                {
                    cmd.Parameters.Add("objectName", OracleType.VarChar).Value = objectName;
                    cmd.Parameters.Add("objectType", OracleType.VarChar).Value = dbObjectType;

                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            sourceCode.Append(reader["text"]);
                        }
                    }
                }

                if (addSlashTo.Contains(objectType))
                {
                    sourceCode.Append("/");
                }
            }
            catch (Exception e)
            {
                var progressDataErr = new ExportProgressData(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.SchemaObjCountPlan = schemaObjCountPlan;
                progressDataErr.TypeObjCountPlan = typeObjCountPlan;
                progressDataErr.Current = current;
                progressDataErr.ObjectName = objectName;
                progressDataErr.ObjectType = objectType;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }

            var progressData2 = new ExportProgressData(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = 1;
            progressData2.SchemaObjCountPlan = schemaObjCountPlan;
            progressData2.TypeObjCountPlan = typeObjCountPlan;
            progressData2.Current = current;
            progressData2.ObjectName = objectName;
            progressData2.ObjectType = objectType;
            _progressDataManager.ReportCurrentProgress(progressData2);


            return sourceCode.ToString();
        }

        static string GetAddObjectNameMaskWhere(string fieldName, string _objectNameMask, bool firstInWhereBlock)
        {
            if (string.IsNullOrWhiteSpace(_objectNameMask)) return "";
            var res = "";
            var ar = _objectNameMask.Split(';');
            if (ar.Any() && !string.IsNullOrWhiteSpace(ar[0]))
            {
                if (firstInWhereBlock)
                    res += " WHERE (";
                else
                    res += " AND (";

                foreach (var maskItem in ar.Where(c => !string.IsNullOrWhiteSpace(c)))
                    res += $"{fieldName} like '{maskItem}' OR ";

                res = res.Substring(0, res.Length - 4);
                res += ") ";
            }
            return res;
        }

        public static string GetObjectQuery(string objectType, string _objectNameMask)
        {
            switch (objectType)
            {
                case "FUNCTIONS":
                case "PROCEDURES":
                case "TRIGGERS":
                case "TYPES":
                case "VIEWS":
                    return "SELECT object_name FROM user_objects " +
                           $"WHERE object_type = '{GetObjectTypeName(objectType)}' " + GetAddObjectNameMaskWhere("object_name", _objectNameMask, false) +
                           "ORDER BY object_name";

                case "PACKAGES":
                    return "SELECT distinct (object_name) FROM user_objects " +
                           "WHERE object_type in ('PACKAGE','PACKAGE BODY') " + GetAddObjectNameMaskWhere("object_name", _objectNameMask, false) +
                           "ORDER BY object_name";

                case "SEQUENCES":
                    return "SELECT sequence_name AS object_name FROM user_sequences " + GetAddObjectNameMaskWhere("sequence_name", _objectNameMask, true) +
                           "ORDER BY sequence_name";

                case "SYNONYMS":
                    return "SELECT synonym_name AS object_name FROM user_synonyms " + GetAddObjectNameMaskWhere("synonym_name", _objectNameMask, true) +
                           "ORDER BY synonym_name";

                case "TABLES":
                    return "SELECT table_name AS object_name FROM user_tables " + GetAddObjectNameMaskWhere("table_name", _objectNameMask, true) +
                           "ORDER BY table_name";

                case "JOBS":
                    return $"SELECT job_name AS object_name FROM user_scheduler_jobs{GetAddObjectNameMaskWhere("job_name", _objectNameMask, true)} union all select to_char(job) from user_jobs";

                case "DBLINKS":
                    return "SELECT db_link AS object_name FROM user_db_links " + GetAddObjectNameMaskWhere("db_link", _objectNameMask, true) +
                           "ORDER BY db_link";

                default:
                    return "SELECT object_name FROM user_objects " +
                           $"WHERE object_type = '{GetObjectTypeName(objectType)}' " + GetAddObjectNameMaskWhere("object_name", _objectNameMask, false) +
                           "ORDER BY object_name";
            }
        }

        public List<SynonymAttributes> GetSynonyms(ExportProgressDataStage stage, int schemaObjCountPlan, int typeObjCountPlan, int current, string objectTypeMulti, out bool canceledByUser)
        {
            var res = new List<SynonymAttributes>();

            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;

                var progressDataCancel = new ExportProgressData(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.SchemaObjCountPlan = schemaObjCountPlan;
                progressDataCancel.TypeObjCountPlan = typeObjCountPlan;
                progressDataCancel.Current = current;
                progressDataCancel.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);

                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressData(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.SchemaObjCountPlan = schemaObjCountPlan;
            progressData.TypeObjCountPlan = typeObjCountPlan;
            progressData.Current = current;
            progressData.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData);
            try
            {
                string ddlQuery = "SELECT synonym_name, table_owner, table_name, db_link  FROM user_synonyms" + GetAddObjectNameMaskWhere("synonym_name", _objectNameMask, true);
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new SynonymAttributes();
                            item.Name = reader["synonym_name"].ToString();
                            item.TargetSchema = reader["table_owner"].ToString();
                            item.TargetObjectName = reader["table_name"].ToString();
                            item.DBLink = reader["db_link"].ToString();
                            res.Add(item);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var progressDataErr = new ExportProgressData(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.SchemaObjCountPlan = schemaObjCountPlan;
                progressDataErr.TypeObjCountPlan = typeObjCountPlan;
                progressDataErr.Current = current;
                progressDataErr.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }

            var progressData2 = new ExportProgressData(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = res.Count;
            progressData2.SchemaObjCountPlan = schemaObjCountPlan;
            progressData2.TypeObjCountPlan = typeObjCountPlan;
            progressData2.Current = current;
            progressData2.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData2);

            return res;
        }

        public List<SequenceAttributes> GetSequences(ExportProgressDataStage stage, int schemaObjCountPlan, int typeObjCountPlan, int current, string objectTypeMulti, out bool canceledByUser)
        {
            var res = new List<SequenceAttributes>();

            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressData(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.SchemaObjCountPlan = schemaObjCountPlan;
                progressDataCancel.TypeObjCountPlan = typeObjCountPlan;
                progressDataCancel.Current = current;
                progressDataCancel.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressData(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.SchemaObjCountPlan = schemaObjCountPlan;
            progressData.TypeObjCountPlan = typeObjCountPlan;
            progressData.Current = current;
            progressData.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData);

            try
            {
                string ddlQuery = "SELECT sequence_name, min_value, max_value, increment_by, cycle_flag, order_flag, cache_size, last_number FROM user_sequences" + GetAddObjectNameMaskWhere("sequence_name", _objectNameMask, true);
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new SequenceAttributes();
                            item.SequenceName = reader["sequence_name"].ToString();
                            if (!reader.IsDBNull(reader.GetOrdinal("min_value")))
                                item.MinValue = reader.GetDouble(reader.GetOrdinal("min_value"));
                            if (!reader.IsDBNull(reader.GetOrdinal("max_value")))
                            {
                                item.MaxValue = reader.GetDecimal(reader.GetOrdinal("max_value"));

                            }

                            item.IncrementBy = reader.GetInt32(reader.GetOrdinal("increment_by"));
                            item.CycleFlag = reader["cycle_flag"].ToString();
                            item.OrderFlag = reader["order_flag"].ToString();
                            item.CacheSize = reader.GetInt32(reader.GetOrdinal("cache_size"));
                            item.LastNumber = reader.GetDouble(reader.GetOrdinal("last_number"));
                            res.Add(item);
                        }
                    }
                }

            }
            catch (Exception e)
            {
                var progressDataErr = new ExportProgressData(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.SchemaObjCountPlan = schemaObjCountPlan;
                progressDataErr.TypeObjCountPlan = typeObjCountPlan;
                progressDataErr.Current = current;
                progressDataErr.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }

            var progressData2 = new ExportProgressData(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = res.Count;
            progressData2.SchemaObjCountPlan = schemaObjCountPlan;
            progressData2.TypeObjCountPlan = typeObjCountPlan;
            progressData2.Current = current;
            progressData2.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData2);

            return res;
        }

        public List<SchedulerJob> GetSchedulerJobs(ExportProgressDataStage stage, int schemaObjCountPlan, int typeObjCountPlan, int current, string objectTypeMulti, out bool canceledByUser)
        {
            var res = new List<SchedulerJob>();

            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressData(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.SchemaObjCountPlan = schemaObjCountPlan;
                progressDataCancel.TypeObjCountPlan = typeObjCountPlan;
                progressDataCancel.Current = current;
                progressDataCancel.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressData(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.SchemaObjCountPlan = schemaObjCountPlan;
            progressData.TypeObjCountPlan = typeObjCountPlan;
            progressData.Current = current;
            progressData.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData);

            try
            {
                string ddlQuery = "SELECT job_name, job_type, job_action, start_date, repeat_interval, end_date, job_class, enabled, auto_drop, comments, number_of_arguments FROM user_scheduler_jobs" + GetAddObjectNameMaskWhere("job_name", _objectNameMask, true);
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new SchedulerJob();
                            item.JobName = reader["job_name"].ToString();
                            item.JobType = reader["job_type"].ToString();
                            item.JobAction = reader["job_action"].ToString();
                            if (!reader.IsDBNull(reader.GetOrdinal("start_date")))
                                item.StartDate = reader.GetDateTime(reader.GetOrdinal("start_date"));
                            if (!reader.IsDBNull(reader.GetOrdinal("end_date")))
                                item.EndDate = reader.GetDateTime(reader.GetOrdinal("end_date"));
                            item.RepeatInterval = reader["repeat_interval"].ToString();
                            item.JobClass = reader["job_class"].ToString();
                            item.Enabled = reader["enabled"].ToString();
                            item.AutoDrop = reader["auto_drop"].ToString();
                            item.Comments = reader["comments"].ToString();
                            if (!reader.IsDBNull(reader.GetOrdinal("number_of_arguments")))
                                item.NumberOfArguments = reader.GetInt32(reader.GetOrdinal("number_of_arguments"));
                            res.Add(item);
                        }
                    }
                }

                ddlQuery = $"select job_name, argument_name, argument_position, value from user_scheduler_job_args{GetAddObjectNameMaskWhere("job_name", _objectNameMask, true)} order by job_name, argument_position";
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new SchedulerJobArgument();
                            item.JobName = reader["job_name"].ToString();
                            item.ArgumentName = reader["argument_name"].ToString();
                            item.ArgumentPosition = reader.GetInt32(reader.GetOrdinal("argument_position"));
                            item.Value = reader["value"].ToString();

                            var job = res.FirstOrDefault(c => c.JobName == item.JobName);
                            if (job != null)
                                job.ArgumentList.Add(item);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var progressDataErr = new ExportProgressData(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.SchemaObjCountPlan = schemaObjCountPlan;
                progressDataErr.TypeObjCountPlan = typeObjCountPlan;
                progressDataErr.Current = current;
                progressDataErr.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }

            var progressData2 = new ExportProgressData(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = res.Count;
            progressData2.SchemaObjCountPlan = schemaObjCountPlan;
            progressData2.TypeObjCountPlan = typeObjCountPlan;
            progressData2.Current = current;
            progressData2.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData2);

            return res;
        }

        public List<DBMSJob> GetDBMSJobs(ExportProgressDataStage stage, int schemaObjCountPlan, int typeObjCountPlan, int current, string objectTypeMulti, out bool canceledByUser)
        {
            var res = new List<DBMSJob>();

            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressData(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.SchemaObjCountPlan = schemaObjCountPlan;
                progressDataCancel.TypeObjCountPlan = typeObjCountPlan;
                progressDataCancel.Current = current;
                progressDataCancel.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);

                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressData(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.SchemaObjCountPlan = schemaObjCountPlan;
            progressData.TypeObjCountPlan = typeObjCountPlan;
            progressData.Current = current;
            progressData.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData);
            try
            {
                string ddlQuery = "select job, what, next_date, next_sec, interval from user_jobs";
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new DBMSJob();
                            item.Job = reader.GetInt32(reader.GetOrdinal("job"));
                            item.What = reader["what"].ToString();
                            item.Interval = reader["interval"].ToString();
                            item.NextDate = reader.GetDateTime(reader.GetOrdinal("next_date"));
                            item.NextSec = reader["next_sec"].ToString();
                            res.Add(item);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var progressDataErr = new ExportProgressData(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.SchemaObjCountPlan = schemaObjCountPlan;
                progressDataErr.TypeObjCountPlan = typeObjCountPlan;
                progressDataErr.Current = current;
                progressDataErr.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }

            var progressData2 = new ExportProgressData(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = res.Count;
            progressData2.SchemaObjCountPlan = schemaObjCountPlan;
            progressData2.TypeObjCountPlan = typeObjCountPlan;
            progressData2.Current = current;
            progressData2.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData2);

            return res;
        }

        public Dictionary<string,string> GetViews(ExportProgressDataStage stage, int schemaObjCountPlan, int typeObjCountPlan, int current, string objectTypeMulti, out bool canceledByUser)
        {
            var res = new Dictionary<string, string>();

            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressData(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.SchemaObjCountPlan = schemaObjCountPlan;
                progressDataCancel.TypeObjCountPlan = typeObjCountPlan;
                progressDataCancel.Current = current;
                progressDataCancel.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressData(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.SchemaObjCountPlan = schemaObjCountPlan;
            progressData.TypeObjCountPlan = typeObjCountPlan;
            progressData.Current = current;
            progressData.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData);

            try
            {
                string ddlQuery = "SELECT view_name, text  FROM user_views" + GetAddObjectNameMaskWhere("view_name", _objectNameMask, true);
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var name = reader["view_name"].ToString();
                            var text = reader["text"].ToString();
                            res[name] = text.TrimEnd();
                        }
                    }
                }
            }
            catch (Exception e)
            {

                var progressDataErr = new ExportProgressData(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.SchemaObjCountPlan = schemaObjCountPlan;
                progressDataErr.TypeObjCountPlan = typeObjCountPlan;
                progressDataErr.Current = current;
                progressDataErr.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }

            var progressData2 = new ExportProgressData(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = res.Count;
            progressData2.SchemaObjCountPlan = schemaObjCountPlan;
            progressData2.TypeObjCountPlan = typeObjCountPlan;
            progressData2.Current = current;
            progressData2.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData2);

            return res;
        }

        public List<ColumnComment> GetTablesAndViewsColumnComments(ExportProgressDataStage stage, int schemaObjCountPlan, out bool canceledByUser)
        {
            var res = new List<ColumnComment>();

            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressData(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.SchemaObjCountPlan = schemaObjCountPlan;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressData(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.SchemaObjCountPlan = schemaObjCountPlan;
            _progressDataManager.ReportCurrentProgress(progressData);

            try
            {
                string ddlQuery = @"SELECT table_name, column_name, comments  FROM USER_COL_COMMENTS" + GetAddObjectNameMaskWhere("table_name", _objectNameMask, true);
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {

                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new ColumnComment();
                            item.TableName = reader["table_name"].ToString();
                            item.ColumnName = reader["column_name"].ToString();
                            item.Comments = reader["comments"].ToString();
                            res.Add(item);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var progressDataErr = new ExportProgressData(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.SchemaObjCountPlan = schemaObjCountPlan;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }

            var progressData2 = new ExportProgressData(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = res.Count;
            progressData2.SchemaObjCountPlan = schemaObjCountPlan;
            _progressDataManager.ReportCurrentProgress(progressData2);

            return res;
        }

        public List<TableOrViewComment> GetTableOrViewComments(string objectType, ExportProgressDataStage stage, int schemaObjCountPlan, int typeObjCountPlan, int current, string objectTypeMulti, out bool canceledByUser)
        {
            var res = new List<TableOrViewComment>();

            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressData(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.SchemaObjCountPlan = schemaObjCountPlan;
                progressDataCancel.TypeObjCountPlan = typeObjCountPlan;
                progressDataCancel.Current = current;
                progressDataCancel.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressData(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.SchemaObjCountPlan = schemaObjCountPlan;
            progressData.TypeObjCountPlan = typeObjCountPlan;
            progressData.Current = current;
            progressData.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData);
            try
            {
                string ddlQuery = @"SELECT table_name, comments  FROM USER_TAB_COMMENTS WHERE table_type=:objectType" + GetAddObjectNameMaskWhere("table_name", _objectNameMask, false);
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    cmd.Parameters.Add("objectType", OracleType.VarChar).Value = objectType;
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new TableOrViewComment();
                            item.TableName = reader["table_name"].ToString();
                            item.Comments = reader["comments"].ToString();
                            res.Add(item);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var progressDataErr = new ExportProgressData(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.SchemaObjCountPlan = schemaObjCountPlan;
                progressDataErr.TypeObjCountPlan = typeObjCountPlan;
                progressDataErr.Current = current;
                progressDataErr.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }

            var progressData2 = new ExportProgressData(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = res.Count;
            progressData2.SchemaObjCountPlan = schemaObjCountPlan;
            progressData2.TypeObjCountPlan = typeObjCountPlan;
            progressData2.Current = current;
            progressData2.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData2);


            return res;
        }

        public List<TableStruct> GetTablesStruct (ExportProgressDataStage stage, int schemaObjCountPlan, int typeObjCountPlan, int current, string objectTypeMulti, out bool canceledByUser)
        {
            var res = new List<TableStruct>();

            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressData(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.SchemaObjCountPlan = schemaObjCountPlan;
                progressDataCancel.TypeObjCountPlan = typeObjCountPlan;
                progressDataCancel.Current = current;
                progressDataCancel.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressData(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.SchemaObjCountPlan = schemaObjCountPlan;
            progressData.TypeObjCountPlan = typeObjCountPlan;
            progressData.Current = current;
            progressData.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData);

            try
            {
                string ddlQuery = @"SELECT table_name, partitioned, temporary, duration, compression, iot_type, logging, dependencies FROM USER_TABLES" + GetAddObjectNameMaskWhere("table_name", _objectNameMask, true);
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new TableStruct();
                            item.TableName = reader["table_name"].ToString();
                            item.Partitioned = reader["partitioned"].ToString();
                            item.Temporary = reader["temporary"].ToString();
                            item.Duration = reader["duration"].ToString();
                            item.Compression = reader["compression"].ToString();
                            item.IOTType = reader["iot_type"].ToString();
                            item.Logging = reader["logging"].ToString();
                            item.Dependencies = reader["dependencies"].ToString();
                            res.Add(item);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var progressDataErr = new ExportProgressData(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.SchemaObjCountPlan = schemaObjCountPlan;
                progressDataErr.TypeObjCountPlan = typeObjCountPlan;
                progressDataErr.Current = current;
                progressDataErr.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }
            var progressData2 = new ExportProgressData(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = res.Count;
            progressData2.SchemaObjCountPlan = schemaObjCountPlan;
            progressData2.TypeObjCountPlan = typeObjCountPlan;
            progressData2.Current = current;
            progressData2.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData2);
            return res;
        }

        public List<TableColumnStruct> GetTablesAndViewsColumnsStruct(ExportProgressDataStage stage, int schemaObjCountPlan, Dictionary<string, List<string>> systemViewInfo, out bool canceledByUser)
        {
            var res = new List<TableColumnStruct>();

            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressData(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.SchemaObjCountPlan = schemaObjCountPlan;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressData(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.SchemaObjCountPlan = schemaObjCountPlan;
            _progressDataManager.ReportCurrentProgress(progressData);

            try
            {
                var existsIdentityColumnsView = systemViewInfo["USER_TAB_IDENTITY_COLS"] != null;
                var existsDefaultOnNullColumn = systemViewInfo["USER_TAB_COLS"].Any(c => c == "DEFAULT_ON_NULL");
                var addDefalutOnNullSelect = existsDefaultOnNullColumn ? ", default_on_null" : "";

                string ddlQuery = $@"SELECT table_name, column_name, data_type, data_type_owner, data_length, char_length,  char_col_decl_length, data_precision, data_scale, nullable, column_id, data_default, hidden_column, virtual_column, char_used{addDefalutOnNullSelect} FROM USER_TAB_COLS" + GetAddObjectNameMaskWhere("table_name", _objectNameMask, true);
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new TableColumnStruct();
                            item.TableName = reader["table_name"].ToString();
                            item.ColumnName = reader["column_name"].ToString();
                            item.DataType = reader["data_type"].ToString();
                            item.DataTypeOwner = reader["data_type_owner"].ToString();
                            item.DataLength = reader.GetInt32(reader.GetOrdinal("data_length"));
                            if (!reader.IsDBNull(reader.GetOrdinal("char_length")))
                                item.CharLength = reader.GetInt32(reader.GetOrdinal("char_length"));
                            if (!reader.IsDBNull(reader.GetOrdinal("char_col_decl_length")))
                                item.CharColDeclLength = reader.GetInt32(reader.GetOrdinal("char_col_decl_length"));
                            if (!reader.IsDBNull(reader.GetOrdinal("data_precision")))
                                item.DataPrecision = reader.GetInt32(reader.GetOrdinal("data_precision"));
                            if (!reader.IsDBNull(reader.GetOrdinal("data_scale")))
                                item.DataScale = reader.GetInt32(reader.GetOrdinal("data_scale"));
                            item.Nullable = reader["nullable"].ToString();
                            if (!reader.IsDBNull(reader.GetOrdinal("column_id")))
                                item.ColumnId = reader.GetInt32(reader.GetOrdinal("column_id"));
                            if (!reader.IsDBNull(reader.GetOrdinal("data_default")))
                                item.DataDefault = reader["data_default"].ToString().Trim();
                            item.HiddenColumn = reader["hidden_column"].ToString();
                            item.VirtualColumn = reader["virtual_column"].ToString();
                            item.CharUsed = reader["char_used"].ToString();
                            if (existsDefaultOnNullColumn)
                                item.DefaultOnNull = reader["default_on_null"].ToString();
                            res.Add(item);
                        }
                    }
                }

                if (existsIdentityColumnsView)
                {
                    ddlQuery = @"SELECT table_name, column_name, generation_type, identity_options FROM user_tab_identity_cols" + GetAddObjectNameMaskWhere("table_name", _objectNameMask, true);
                    using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                    {
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var item = new TableIdentityColumnStruct();
                                item.TableName = reader["table_name"].ToString();
                                item.ColumnName = reader["column_name"].ToString();
                                item.GenerationType = reader["generation_type"].ToString();
                                item.IdentityOptions = reader["identity_options"].ToString();
                                var column = res.FirstOrDefault(c =>
                                    c.TableName == item.TableName && c.ColumnName == item.ColumnName);
                                if (column != null)
                                    column.IdentityColumnStruct = item;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var progressDataErr = new ExportProgressData(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.SchemaObjCountPlan = schemaObjCountPlan;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }

            var progressData2 = new ExportProgressData(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = res.Count;
            progressData2.SchemaObjCountPlan = schemaObjCountPlan;
            _progressDataManager.ReportCurrentProgress(progressData2);

            return res;
        }

        public List<PartTables> GetTablesPartitions(bool ExtractOnlyDefParts, ExportProgressDataStage stage, int schemaObjCountPlan, int typeObjCountPlan, int current, string objectTypeMulti, out bool canceledByUser)
        {
            var res = new List<PartTables>();

            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressData(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.SchemaObjCountPlan = schemaObjCountPlan;
                progressDataCancel.TypeObjCountPlan = typeObjCountPlan;
                progressDataCancel.Current = current;
                progressDataCancel.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressData(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.SchemaObjCountPlan = schemaObjCountPlan;
            progressData.TypeObjCountPlan = typeObjCountPlan;
            progressData.Current = current;
            progressData.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData);

            try
            {
                //USER_PART_TABLES
                string ddlQuery = @"SELECT table_name, partitioning_type, subpartitioning_type, partition_count, def_subpartition_count, partitioning_key_count, subpartitioning_key_count, def_tablespace_name, interval FROM USER_PART_TABLES" + GetAddObjectNameMaskWhere("table_name", _objectNameMask, true);
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new PartTables();
                            item.TableName = reader["table_name"].ToString();
                            item.PartitioningType = reader["partitioning_type"].ToString();
                            item.SubPartitioningType = reader["subpartitioning_type"].ToString();
                            if (!reader.IsDBNull(reader.GetOrdinal("partition_count")))
                                item.PartitionCount = reader.GetInt32(reader.GetOrdinal("partition_count"));
                            if (!reader.IsDBNull(reader.GetOrdinal("def_subpartition_count")))
                                item.DefSubPartitionCount = reader.GetInt32(reader.GetOrdinal("def_subpartition_count"));
                            if (!reader.IsDBNull(reader.GetOrdinal("partitioning_key_count")))
                                item.PartitioningKeyCount = reader.GetInt32(reader.GetOrdinal("partitioning_key_count"));
                            if (!reader.IsDBNull(reader.GetOrdinal("subpartitioning_key_count")))
                                item.SubPartitioningKeyCount = reader.GetInt32(reader.GetOrdinal("subpartitioning_key_count"));
                            item.DefTableSpaceName = reader["def_tablespace_name"].ToString();
                            item.Interval = reader["interval"].ToString();
                            res.Add(item);
                        }
                    }
                }

                //USER_PART_KEY_COLUMNS
                ddlQuery = @"SELECT name, column_name, column_position FROM USER_PART_KEY_COLUMNS where object_type='TABLE'" + GetAddObjectNameMaskWhere("name", _objectNameMask, false);
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new PartOrSubPartKeyColumns();
                            item.TableName = reader["name"].ToString();
                            item.ColumnName = reader["column_name"].ToString();
                            if (!reader.IsDBNull(reader.GetOrdinal("column_position")))
                                item.ColumnPosition = reader.GetInt32(reader.GetOrdinal("column_position"));
                            var table = res.FirstOrDefault(c => c.TableName == item.TableName);
                            if (table != null)
                                table.PartKeyColumns.Add(item);
                        }
                    }
                }

                //USER_SUBPART_KEY_COLUMNS
                ddlQuery = @"SELECT name, column_name, column_position FROM USER_SUBPART_KEY_COLUMNS where object_type='TABLE'" + GetAddObjectNameMaskWhere("name", _objectNameMask, false);
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new PartOrSubPartKeyColumns();
                            item.TableName = reader["name"].ToString();
                            item.ColumnName = reader["column_name"].ToString();
                            if (!reader.IsDBNull(reader.GetOrdinal("column_position")))
                                item.ColumnPosition = reader.GetInt32(reader.GetOrdinal("column_position"));
                            var table = res.FirstOrDefault(c => c.TableName == item.TableName);
                            if (table != null)
                                table.SubPartKeyColumns.Add(item);
                        }
                    }
                }

                //USER_TAB_PARTITIONS
                var addRestrStr = ExtractOnlyDefParts ? "partition_position=1" : "1=1";
                ddlQuery = @"SELECT table_name, partition_name, subpartition_count, high_value, partition_position, tablespace_name FROM USER_TAB_PARTITIONS WHERE " + addRestrStr + GetAddObjectNameMaskWhere("table_name", _objectNameMask, false);
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new TabPartitions();
                            item.TableName = reader["table_name"].ToString();
                            item.PartitionName = reader["partition_name"].ToString();
                            if (!reader.IsDBNull(reader.GetOrdinal("subpartition_count")))
                                item.SubPartitionCount = reader.GetInt32(reader.GetOrdinal("subpartition_count"));
                            item.HighValue = reader["high_value"].ToString();
                            if (!reader.IsDBNull(reader.GetOrdinal("partition_position")))
                                item.PartitionPosition = reader.GetInt32(reader.GetOrdinal("partition_position"));
                            item.TableSpaceName = reader["tablespace_name"].ToString();
                            var table = res.FirstOrDefault(c => c.TableName == item.TableName);
                            if (table != null)
                                table.Partitions.Add(item);
                        }
                    }
                }

                //USER_TAB_SUBPARTITIONS
                addRestrStr = ExtractOnlyDefParts ? "subpartition_position=1" : "1=1";
                ddlQuery = @"SELECT table_name, partition_name, subpartition_name, high_value, subpartition_position, tablespace_name FROM USER_TAB_SUBPARTITIONS WHERE " + addRestrStr + GetAddObjectNameMaskWhere("table_name", _objectNameMask, false);
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new TabSubPartitions();
                            item.TableName = reader["table_name"].ToString();
                            item.PartitionName = reader["partition_name"].ToString();
                            item.SubPartitionName = reader["subpartition_name"].ToString();
                            item.HighValue = reader["high_value"].ToString();
                            if (!reader.IsDBNull(reader.GetOrdinal("subpartition_position")))
                                item.SubPartitionPosition = reader.GetInt32(reader.GetOrdinal("subpartition_position"));
                            item.TableSpaceName = reader["tablespace_name"].ToString();
                            var table = res.FirstOrDefault(c => c.TableName == item.TableName);
                            if (table != null)
                            {
                                var partition = table.Partitions.FirstOrDefault(c => c.PartitionName == item.PartitionName);
                                if (partition != null)
                                    partition.SubPartitions.Add(item);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var progressDataErr = new ExportProgressData(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.SchemaObjCountPlan = schemaObjCountPlan;
                progressDataErr.TypeObjCountPlan = typeObjCountPlan;
                progressDataErr.Current = current;
                progressDataErr.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }

            var progressData2 = new ExportProgressData(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = res.Count;
            progressData2.SchemaObjCountPlan = schemaObjCountPlan;
            progressData2.TypeObjCountPlan = typeObjCountPlan;
            progressData2.Current = current;
            progressData2.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData2);

            return res;
        }

        public Dictionary<string,List<string>> GetInfoAboutSystemViews(List<string> viewNames, ExportProgressDataStage stage, out bool canceledByUser)
        {
            var res = new Dictionary<string, List<string>>();
            var infoForProgress = new StringBuilder();
            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressData(ExportProgressDataLevel.CANCEL, stage);
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressData(ExportProgressDataLevel.STAGESTARTINFO, stage);
            _progressDataManager.ReportCurrentProgress(progressData);

            try
            {
                if (viewNames.Any())
                {
                    var tmpAr = new List<Tuple<string,string>>();
                    var names = viewNames.MergeFormatted("'", ",");
                    string ddlQuery = $@"SELECT table_name, column_name FROM all_tab_cols WHERE owner = 'SYS' AND table_name in ({names})";
                    using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                    {
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                tmpAr.Add(new Tuple<string, string>(reader["table_name"].ToString(),
                                    reader["column_name"].ToString()));
                            }
                        }
                    }

                    for (int i = 0; i < viewNames.Count; i++)
                    {
                        var view = viewNames[i];
                        infoForProgress.Append($"{view}: ");
                        var cols = tmpAr.Where(c => c.Item1 == view);
                        if (cols.Any())
                        {
                            res[view] = cols.Select(c => c.Item2).ToList();
                            infoForProgress.Append("да");
                        }
                        else
                        {
                            res[view] = null;
                            infoForProgress.Append("нет");
                        }

                        if (i < viewNames.Count - 1)
                            infoForProgress.Append(", ");
                    }
                }
                
            }
            catch (Exception e)
            {
                var progressDataErr = new ExportProgressData(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }


            var progressData2 = new ExportProgressData(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = res.Count;
            progressData2.TextAddInfo = infoForProgress.ToString();
            _progressDataManager.ReportCurrentProgress(progressData2);

            return res;
        }

        public List<IndexStruct> GetTablesIndexes(ExportProgressDataStage stage, int schemaObjCountPlan, int typeObjCountPlan, int current, string objectTypeMulti, out bool canceledByUser)
        {
            var res = new List<IndexStruct>();

            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressData(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.SchemaObjCountPlan = schemaObjCountPlan;
                progressDataCancel.TypeObjCountPlan = typeObjCountPlan;
                progressDataCancel.Current = current;
                progressDataCancel.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressData(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.SchemaObjCountPlan = schemaObjCountPlan;
            progressData.TypeObjCountPlan = typeObjCountPlan;
            progressData.Current = current;
            progressData.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData);

            try
            {
                string ddlQuery = @"SELECT i.table_name, i.index_name, i.index_type, i.uniqueness, i.compression, i.prefix_length, i.logging, p.locality 
            from USER_INDEXES i 
            LEFT JOIN USER_PART_INDEXES p ON i.TABLE_NAME=p.table_name and i.INDEX_NAME = p.INDEX_NAME
            WHERE i.TABLE_TYPE='TABLE'" + GetAddObjectNameMaskWhere("i.table_name", _objectNameMask, false);
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new IndexStruct();
                            item.TableName = reader["table_name"].ToString();
                            item.IndexName = reader["index_name"].ToString();
                            item.IndexType = reader["index_type"].ToString();
                            item.Uniqueness = reader["uniqueness"].ToString();
                            item.Compression = reader["compression"].ToString();
                            if (!reader.IsDBNull(reader.GetOrdinal("prefix_length")))
                                item.PrefixLength = reader.GetInt32(reader.GetOrdinal("prefix_length"));
                            item.Logging = reader["logging"].ToString();
                            item.Locality = reader["locality"].ToString();
                            res.Add(item);
                        }
                    }
                }
                ddlQuery = @"select table_name, index_name, column_name, column_position, descend  from USER_IND_COLUMNS" + GetAddObjectNameMaskWhere("table_name", _objectNameMask, true);
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new IndexColumnStruct();
                            item.TableName = reader["table_name"].ToString();
                            item.IndexName = reader["index_name"].ToString();
                            item.ColumnName = reader["column_name"].ToString();
                            if (!reader.IsDBNull(reader.GetOrdinal("column_position")))
                                item.ColumnPosition = reader.GetInt32(reader.GetOrdinal("column_position"));
                            item.Descend = reader["descend"].ToString();
                            var index = res.FirstOrDefault(c => c.TableName.ToUpper() == item.TableName.ToUpper() && c.IndexName.ToUpper() == item.IndexName.ToUpper());
                            if (index != null)
                                index.IndexColumnStructs.Add(item);
                        }
                    }
                }
                ddlQuery = @"select table_name, index_name, column_expression, column_position from USER_IND_EXPRESSIONS" + GetAddObjectNameMaskWhere("table_name", _objectNameMask, true) + " order by column_position";
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var tableName = reader["table_name"].ToString().ToUpper();
                            var indexName = reader["index_name"].ToString().ToUpper();
                            int? columnPosition = null;
                            if (!reader.IsDBNull(reader.GetOrdinal("column_position")))
                                columnPosition = reader.GetInt32(reader.GetOrdinal("column_position"));
                            if (columnPosition != null)
                            {
                                var index = res.FirstOrDefault(c =>
                                    c.TableName.ToUpper() == tableName && c.IndexName.ToUpper() == indexName);
                                if (index != null)
                                {
                                    var colIndex =
                                        index.IndexColumnStructs.FirstOrDefault(c => c.ColumnPosition == columnPosition);
                                    if (colIndex != null)
                                        colIndex.Expression = reader["column_expression"].ToString().Replace("\"", "");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var progressDataErr = new ExportProgressData(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.SchemaObjCountPlan = schemaObjCountPlan;
                progressDataErr.TypeObjCountPlan = typeObjCountPlan;
                progressDataErr.Current = current;
                progressDataErr.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }

            var progressData2 = new ExportProgressData(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = res.Count;
            progressData2.SchemaObjCountPlan = schemaObjCountPlan;
            progressData2.TypeObjCountPlan = typeObjCountPlan;
            progressData2.Current = current;
            progressData2.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData2);

            return res;
        }

        public List<ConstraintStruct> GetTablesConstraints(string schemaName, ExportProgressDataStage stage, int schemaObjCountPlan, int typeObjCountPlan, int current, string objectTypeMulti, out bool canceledByUser)
        {
            var res = new List<ConstraintStruct>();

            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressData(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.SchemaObjCountPlan = schemaObjCountPlan;
                progressDataCancel.TypeObjCountPlan = typeObjCountPlan;
                progressDataCancel.Current = current;
                progressDataCancel.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressData(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.SchemaObjCountPlan = schemaObjCountPlan;
            progressData.TypeObjCountPlan = typeObjCountPlan;
            progressData.Current = current;
            progressData.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData);

            try
            {
                string ddlQuery = @"select owner, table_name, constraint_name, constraint_type, status, validated, search_condition, generated, r_owner, r_constraint_name, delete_rule from USER_CONSTRAINTS" + GetAddObjectNameMaskWhere("table_name", _objectNameMask, true);
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    //cmd.Parameters.Add("schemaName", OracleType.VarChar).Value = schemaName.ToUpper();
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new ConstraintStruct();
                            item.Owner = reader["owner"].ToString();
                            item.TableName = reader["table_name"].ToString();
                            item.ConstraintName = reader["constraint_name"].ToString();
                            item.ConstraintType = reader["constraint_type"].ToString();
                            item.Status = reader["status"].ToString();
                            item.Validated = reader["validated"].ToString();
                            item.Generated = reader["generated"].ToString();
                            item.ROwner = reader["r_owner"].ToString();
                            item.RConstraintName = reader["r_constraint_name"].ToString();
                            item.DeleteRule = reader["delete_rule"].ToString();
                            item.SearchCondition = reader["search_condition"].ToString();
                            res.Add(item);
                        }
                    }
                }
                ddlQuery = @"select table_name, constraint_name, column_name, position from USER_CONS_COLUMNS WHERE owner=:schemaName" + GetAddObjectNameMaskWhere("table_name", _objectNameMask, false);
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    cmd.Parameters.Add("schemaName", OracleType.VarChar).Value = schemaName.ToUpper();
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new ConstraintColumnStruct();
                            item.TableName = reader["table_name"].ToString();
                            item.ConstraintName = reader["constraint_name"].ToString();
                            item.ColumnName = reader["column_name"].ToString();
                            if (!reader.IsDBNull(reader.GetOrdinal("position")))
                                item.Position = reader.GetInt32(reader.GetOrdinal("position"));
                            var constraint = res.FirstOrDefault(c => c.TableName.ToUpper() == item.TableName.ToUpper() && c.ConstraintName.ToUpper() == item.ConstraintName.ToUpper());
                            if (constraint != null)
                                constraint.ConstraintColumnStructs.Add(item);
                        }
                    }
                }

                //внешние ключи
                var outerConstraints = new List<ConstraintStruct>();
                ddlQuery = $@"select owner, table_name, constraint_name, constraint_type, status, validated, generated, r_owner, r_constraint_name, delete_rule from ALL_CONSTRAINTS
                            where (owner,constraint_name) in 
                            (select r_owner, r_constraint_name from USER_CONSTRAINTS{GetAddObjectNameMaskWhere("table_name", _objectNameMask, true)})";
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    //cmd.Parameters.Add("schemaName", OracleType.VarChar).Value = schemaName.ToUpper();
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new ConstraintStruct();
                            item.Owner = reader["owner"].ToString();
                            item.TableName = reader["table_name"].ToString();
                            item.ConstraintName = reader["constraint_name"].ToString();
                            item.ConstraintType = reader["constraint_type"].ToString();
                            item.Status = reader["status"].ToString();
                            item.Validated = reader["validated"].ToString();
                            item.Generated = reader["generated"].ToString();
                            item.ROwner = reader["r_owner"].ToString();
                            item.RConstraintName = reader["r_constraint_name"].ToString();
                            item.DeleteRule = reader["delete_rule"].ToString();
                            outerConstraints.Add(item);
                        }
                    }
                }
                ddlQuery = $@"select table_name, constraint_name, column_name, position from ALL_CONS_COLUMNS
                            where (owner,table_name,constraint_name) in 
                            (select owner, table_name, constraint_name from ALL_CONSTRAINTS
                            where (owner,constraint_name) in 
                            (select r_owner, r_constraint_name from USER_CONSTRAINTS{GetAddObjectNameMaskWhere("table_name", _objectNameMask, true)}))";
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    //cmd.Parameters.Add("schemaName", OracleType.VarChar).Value = schemaName.ToUpper();
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new ConstraintColumnStruct();
                            item.TableName = reader["table_name"].ToString();
                            item.ConstraintName = reader["constraint_name"].ToString();
                            item.ColumnName = reader["column_name"].ToString();
                            if (!reader.IsDBNull(reader.GetOrdinal("position")))
                                item.Position = reader.GetInt32(reader.GetOrdinal("position"));
                            var constraint = outerConstraints.FirstOrDefault(c => c.TableName.ToUpper() == item.TableName.ToUpper() && c.ConstraintName.ToUpper() == item.ConstraintName.ToUpper());
                            if (constraint != null)
                                constraint.ConstraintColumnStructs.Add(item);
                        }
                    }
                }

                foreach (var constraint in res.Where(c=>!string.IsNullOrWhiteSpace(c.RConstraintName)))
                {
                    //var outerConstraint = res.FirstOrDefault(c =>
                    //    c.ConstraintName.ToUpper() == constraint.RConstraintName.ToUpper() && (string.IsNullOrWhiteSpace(constraint.ROwner) || c.Owner.ToUpper() == constraint.ROwner.ToUpper()));
                    //if (outerConstraint == null)
                    //{
                        var outerConstraint = outerConstraints.FirstOrDefault(c =>
                            c.ConstraintName.ToUpper() == constraint.RConstraintName.ToUpper() && (string.IsNullOrWhiteSpace(constraint.ROwner) || c.Owner.ToUpper() == constraint.ROwner.ToUpper()));
                    //}

                    constraint.ReferenceConstraint = outerConstraint;
                }
                
            }
            catch (Exception e)
            {
                var progressDataErr = new ExportProgressData(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.SchemaObjCountPlan = schemaObjCountPlan;
                progressDataErr.TypeObjCountPlan = typeObjCountPlan;
                progressDataErr.Current = current;
                progressDataErr.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }

            var progressData2 = new ExportProgressData(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = res.Count;
            progressData2.SchemaObjCountPlan = schemaObjCountPlan;
            progressData2.TypeObjCountPlan = typeObjCountPlan;
            progressData2.Current = current;
            progressData2.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData2);

            return res;
        }

        public Dictionary<string, string> GetObjectsSourceByType(string objectType, string schemaName,ExportProgressDataStage stage, int schemaObjCountPlan, int typeObjCountPlan, int current, string objectTypeMulti, out bool canceledByUser)
        {
            var res = new Dictionary<string, string>();

            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressData(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.SchemaObjCountPlan = schemaObjCountPlan;
                progressDataCancel.TypeObjCountPlan = typeObjCountPlan;
                progressDataCancel.Current = current;
                progressDataCancel.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressData(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.SchemaObjCountPlan = schemaObjCountPlan;
            progressData.TypeObjCountPlan = typeObjCountPlan;
            progressData.Current = current;
            progressData.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData);

            try
            {
                var sourceQuery = string.Format(@"SELECT name, text, line
                FROM user_source 
                WHERE type = :objectType {0} 
                ORDER BY name, line", GetAddObjectNameMaskWhere("name", _objectNameMask, false));

                StringBuilder sourceCode = new StringBuilder();

                using (OracleCommand cmd = new OracleCommand(sourceQuery, _connection))
                {
                    cmd.Parameters.Add("objectType", OracleType.VarChar).Value = objectType;

                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        var curObjName = "";
                        while (reader.Read())
                        {
                            if (!string.IsNullOrWhiteSpace(curObjName) && curObjName != reader["name"].ToString())
                            {
                                res[curObjName] = sourceCode.ToString();
                                sourceCode.Clear();
                                curObjName = reader["name"].ToString();
                            }
                            else if (string.IsNullOrWhiteSpace(curObjName))
                                curObjName = reader["name"].ToString();

                            var text = reader["text"].ToString();
                            if (!reader.IsDBNull(reader.GetOrdinal("line")))
                            {
                                var line = reader.GetInt32(reader.GetOrdinal("line"));
                                if (line == 1)
                                {
                                    //иногда в первой строке встречается имя схемы, например:
                                    //TRIGGER MCA_FLEX.NEWDOCUM
                                    //или
                                    //TRIGGER "MCA_FLEX".tiVIS_ASKTS4_CONTROLS BEFORE INSERT ON VIS_ASKTS4_CONTROLS
                                    if (text.Contains(schemaName))
                                    {
                                        text = text.Replace($" {schemaName}.", " ").Replace($@" ""{schemaName}"".", " ");
                                    }
                                }
                            }

                            sourceCode.Append(text);
                        }

                        if (!string.IsNullOrWhiteSpace(curObjName))
                            res[curObjName] = sourceCode.ToString();

                    }
                }
            }
            catch (Exception e)
            {
                var progressDataErr = new ExportProgressData(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.SchemaObjCountPlan = schemaObjCountPlan;
                progressDataErr.TypeObjCountPlan = typeObjCountPlan;
                progressDataErr.Current = current;
                progressDataErr.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }
            var progressData2 = new ExportProgressData(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = res.Count;
            progressData2.SchemaObjCountPlan = schemaObjCountPlan;
            progressData2.TypeObjCountPlan = typeObjCountPlan;
            progressData2.Current = current;
            progressData2.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData2);
            return res;
        }

        public List<GrantAttributes> GetAllObjectsGrants(string schemaName, List<string> skipGrantOptions, ExportProgressDataStage stage, int schemaObjCountPlan, out bool canceledByUser)
        {
            var res = new List<GrantAttributes>();

            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressData(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.SchemaObjCountPlan = schemaObjCountPlan;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressData(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.SchemaObjCountPlan = schemaObjCountPlan;
            _progressDataManager.ReportCurrentProgress(progressData);

            try
            {
                string grantQuery = @"SELECT table_name, grantee, privilege, grantable, hierarchy
            FROM user_tab_privs 
            WHERE owner = :schemaName " + GetAddObjectNameMaskWhere("table_name", _objectNameMask, false);// +
                //"ORDER BY table_name, privilege";
                using (OracleCommand cmd = new OracleCommand(grantQuery, _connection))
                {
                    cmd.Parameters.Add("schemaName", OracleType.VarChar).Value = schemaName.ToUpper();
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!skipGrantOptions.Contains(reader["privilege"].ToString().ToUpper()))
                            {
                                var item = new GrantAttributes();
                                item.ObjectName = reader["table_name"].ToString();
                                item.Grantee = reader["grantee"].ToString();
                                item.Privilege = reader["privilege"].ToString();
                                item.Grantable = reader["grantable"].ToString();
                                item.Hierarchy = reader["hierarchy"].ToString();
                                res.Add(item);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var progressDataErr = new ExportProgressData(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.SchemaObjCountPlan = schemaObjCountPlan;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }

            var progressData2 = new ExportProgressData(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = res.Count;
            progressData2.SchemaObjCountPlan = schemaObjCountPlan;
            _progressDataManager.ReportCurrentProgress(progressData2);

            return res;
        }

        public string GetObjectDdl(string objectTypeMulti, string objectName, bool setSequencesValuesTo1, List<string> addSlashTo, ExportProgressDataStage stage, int schemaObjCountPlan, int typeObjCountPlan, int current, out bool canceledByUser)
        {
            var ddl = "";
            string ddlQuery = @"
                SELECT dbms_metadata.get_ddl(:objectType, :objectName) AS ddl 
                FROM dual";

            string dbObjectType = GetObjectTypeName(objectTypeMulti);

            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressData(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.SchemaObjCountPlan = schemaObjCountPlan;
                progressDataCancel.TypeObjCountPlan = typeObjCountPlan;
                progressDataCancel.Current = current;
                progressDataCancel.ObjectType = objectTypeMulti;
                progressDataCancel.ObjectName = objectName;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressData(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.SchemaObjCountPlan = schemaObjCountPlan;
            progressData.TypeObjCountPlan = typeObjCountPlan;
            progressData.Current = current;
            progressData.ObjectType = objectTypeMulti;
            progressData.ObjectName = objectName;
            _progressDataManager.ReportCurrentProgress(progressData);

            try
            {
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    cmd.Parameters.Add("objectType", OracleType.VarChar).Value =
                        dbObjectType == "JOB" ? "PROCOBJ" : dbObjectType;
                    cmd.Parameters.Add("objectName", OracleType.VarChar).Value = objectName;

                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            ddl = reader["ddl"].ToString();
                        }
                    }
                }

                if (dbObjectType == "SEQUENCE")
                {
                    ddl = ddl.Replace("\"", "");
                }

                if (dbObjectType == "SEQUENCE")
                {
                    if (setSequencesValuesTo1)
                        ddl = ResetStartSequenceValue(ddl);
                }

                if (addSlashTo.Contains(objectTypeMulti))
                    ddl += Environment.NewLine + "/";
            }
            catch (Exception e)
            {
                var progressDataErr = new ExportProgressData(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.SchemaObjCountPlan = schemaObjCountPlan;
                progressDataErr.TypeObjCountPlan = typeObjCountPlan;
                progressDataErr.Current = current;
                progressDataErr.ObjectType = objectTypeMulti;
                progressDataErr.ObjectName = objectName;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }

            var progressData2 = new ExportProgressData(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = 1;
            progressData2.SchemaObjCountPlan = schemaObjCountPlan;
            progressData2.TypeObjCountPlan = typeObjCountPlan;
            progressData2.Current = current;
            progressData2.ObjectType = objectTypeMulti;
            progressData2.ObjectName = objectName;
            _progressDataManager.ReportCurrentProgress(progressData2);

            return ddl;
        }

        public static string ResetStartSequenceValue(string sql)
        {
            string pattern = @"(START\s+WITH\s+)(\d+)";

            string result = Regex.Replace(
                sql,
                pattern,
                match => match.Groups[1].Value + "1",
                RegexOptions.IgnoreCase
            );
            return result;
        }

        public void SetSessionTransform(Dictionary<string,string> sessionTransform)
        {
            try
            {
                if (sessionTransform.Any())
                {
                    var query = "BEGIN ";
                    foreach (var key in sessionTransform.Keys)
                    {
                        query +=
                            $"DBMS_METADATA.set_transform_param (DBMS_METADATA.session_transform, '{key}', {sessionTransform[key]}); " /*+Environment.NewLine*/
                            ;
                    }

                    query += "END;";


                    using (OracleCommand cmd = new OracleCommand(query, _connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                var progressData = new ExportProgressData(ExportProgressDataLevel.ERROR, ExportProgressDataStage.PROCESS_SCHEMA);
                progressData.Error = ex.Message;
                progressData.ErrorDetails = ex.StackTrace;
                _progressDataManager.ReportCurrentProgress(progressData);
            }
            
        }

        public void SaveNewProcessInDBLog(DateTime currentDateTime, int threadsCount, string prefix, out string id)
        {
            id = "";
            try
            {
                var query =
                    $"insert into {prefix}PROCESS (id, connections_to_process, start_time) values ({prefix}PROCESS_seq.Nextval, {threadsCount},:start_time) returning id into :new_id";

                using (OracleConnection connection = new OracleConnection(ConnectionString))
                {
                    connection.Open();
                    using (OracleCommand cmd = new OracleCommand(query, connection))
                    {
                        var new_id = new OracleParameter("new_id", OracleType.Int32);
                        new_id.Direction = ParameterDirection.ReturnValue;
                        cmd.Parameters.Add(new_id);
                        cmd.Parameters.Add("start_time", OracleType.DateTime).Value = currentDateTime;
                        cmd.ExecuteNonQuery();
                        id = new_id.Value.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                var progressDataErr = new ExportProgressData(ExportProgressDataLevel.ERROR, ExportProgressDataStage.PROCESS_MAIN);
                progressDataErr.Error = ex.Message;
                progressDataErr.ErrorDetails = ex.StackTrace;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }
        }

        public void UpdateProcessInDBLog(DateTime currentDateTime, string prefix, ExportProgressData progressData)
        {
            try
            {
                var query =
                    $"update {prefix}PROCESS set end_time=:end_time, processobjcountplan=:processobjcountplan, processobjcountfact=:processobjcountfact, errorscount=:errorscount where id=:id";
                using (OracleConnection connection = new OracleConnection(ConnectionString))
                {
                    connection.Open();
                    using (OracleCommand cmd = new OracleCommand(query, connection))
                    {
                        cmd.Parameters.Add("end_time", OracleType.DateTime).Value = currentDateTime;
                        cmd.Parameters.Add("id", OracleType.Int32).Value = int.Parse(progressData.ProcessId);

                        cmd.AddNullableParam("processobjcountplan", OracleType.Number, progressData.ProcessObjCountPlan);
                        cmd.AddNullableParam("processobjcountfact", OracleType.Number, progressData.ProcessObjCountFact);
                        cmd.AddNullableParam("errorscount", OracleType.Number, progressData.ErrorsCount);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                var progressDataErr = new ExportProgressData(ExportProgressDataLevel.ERROR, ExportProgressDataStage.PROCESS_MAIN);
                progressDataErr.Error = ex.Message;
                progressDataErr.ErrorDetails = ex.StackTrace;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }
        }

        public static void SaveConnWorkLogInDB(string prefix, ExportProgressData progressData, string connectionString)
        {
            //try
            //{
                var query =
                    $"insert into {prefix}CONNWORKLOG (id, process_id, dbid, username, stage, eventlevel, eventid, eventtime, message, schemaobjcountplan, typeobjcountplan, objtype, objname, currentnum, schemaobjcountfact, typeobjcountfact, metaobjcountfact, errorscount) values " +
                    $"({prefix}CONNWORKLOG_seq.Nextval, :process_id, :dbid, :username, :stage, :eventlevel, :eventid, :eventtime, :message, :schemaobjcountplan, :typeobjcountplan, :objtype, :objname, :currentnum, :schemaobjcountfact, :typeobjcountfact, :metaobjcountfact, :errorscount)";



            using (OracleConnection connection = new OracleConnection(connectionString))
                {
                    connection.Open();
                    using (OracleCommand cmd = new OracleCommand(query, connection))
                    {
                        cmd.Parameters.Add("process_id", OracleType.Number).Value = int.Parse(progressData.ProcessId);
                        cmd.Parameters.Add("dbid", OracleType.VarChar).Value = progressData.CurrentConnection.DBIdC;
                        cmd.Parameters.Add("username", OracleType.VarChar).Value =
                            progressData.CurrentConnection.UserName;
                        cmd.Parameters.Add("stage", OracleType.VarChar).Value = progressData.Stage.ToString();
                        cmd.Parameters.Add("eventlevel", OracleType.VarChar).Value = progressData.Level.ToString();
                        cmd.Parameters.Add("eventid", OracleType.VarChar).Value = progressData.EventId;
                        cmd.Parameters.Add("eventtime", OracleType.DateTime).Value = progressData.EventTime;
                        cmd.Parameters.Add("message", OracleType.VarChar).Value = progressData.Message;

                        cmd.AddNullableParam("schemaobjcountplan", OracleType.Number, progressData.SchemaObjCountPlan);
                        cmd.AddNullableParam("typeobjcountplan", OracleType.Number, progressData.TypeObjCountPlan);
                        cmd.AddNullableParam("objtype", OracleType.VarChar, progressData.ObjectType);
                        cmd.AddNullableParam("objname", OracleType.VarChar, progressData.ObjectName);
                        cmd.AddNullableParam("currentnum", OracleType.Number, progressData.Current);
                        cmd.AddNullableParam("schemaobjcountfact", OracleType.Number, progressData.SchemaObjCountFact);
                        cmd.AddNullableParam("typeobjcountfact", OracleType.Number, progressData.TypeObjCountFact);
                        cmd.AddNullableParam("metaobjcountfact", OracleType.Number, progressData.MetaObjCountFact);
                        cmd.AddNullableParam("errorscount", OracleType.Number, progressData.ErrorsCount);
                        cmd.ExecuteNonQuery();
                    }
                }
            //}
            //catch (Exception ex)
            //{
            //    var progressDataErr = new ExportProgressData(ExportProgressDataLevel.ERROR, progressData.Stage);
            //    progressDataErr.Error = ex.Message;
            //    progressDataErr.ErrorDetails = ex.StackTrace;
            //    _progressDataManager.ReportCurrentProgress(progressDataErr);
            //}
        }

    }
}
