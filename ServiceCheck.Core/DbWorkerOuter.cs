using ServiceCheck.Core.DBStructs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OracleClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ServiceCheck.Core
{
    public class DbWorkerOuter
    {
        private OracleConnection _connection;
        public string ConnectionString { get; private set; }
        string _dbLink { get; set; }
        private ProgressDataManagerOuter _progressDataManager;
        private CancellationToken _cancellationToken;


        public DbWorkerOuter(OracleConnection connection, string dbLink, ProgressDataManagerOuter progressDataManager/*, CancellationToken cancellationToken*/)
        {
            _connection = connection;
            _dbLink = dbLink;
            _progressDataManager = progressDataManager;
            //_cancellationToken = cancellationToken;
        }

        public DbWorkerOuter(string connectionString, string dbLink, ProgressDataManagerOuter progressDataManager/*, CancellationToken cancellationToken*/)
        {
            ConnectionString = connectionString;
            _dbLink = dbLink;
            _progressDataManager = progressDataManager;
            //_cancellationToken = cancellationToken;
        }

        public void SetCancellationToken(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }




        public List<ObjectTypeNames> GetObjectsNames(List<string> objectTypesList, string schemasIncludeStr, string schemasExcludeStr, ExportProgressDataStageOuter stage, out bool canceledByUser)
        {
            
            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressDataOuter(ExportProgressDataLevel.CANCEL, stage);
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            List<ObjectTypeNames> res = new List<ObjectTypeNames>();
            
            var progressData = new ExportProgressDataOuter(ExportProgressDataLevel.STAGESTARTINFO, stage);
            _progressDataManager.ReportCurrentProgress(progressData);

            var objTypesList2 = objectTypesList.Select(c => GetObjectTypeName(c)).ToList();
            if (objTypesList2.Contains("PACKAGE"))
                objTypesList2.Add("PACKAGE BODY");

            var objTypesNotJobs = objTypesList2.Where(c => c != "JOB").ToList();
            var objTypesJobs = objTypesList2.Where(c => c == "JOB").ToList();

            try
            {
                var items = new List<ObjectTypeName>();
                if (objTypesNotJobs.Any())
                {
                    string objectQuery = GetObjectQuery(objTypesNotJobs, false, schemasIncludeStr, schemasExcludeStr);
                    using (OracleCommand cmd = new OracleCommand(objectQuery, _connection))
                    {
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var item = new ObjectTypeName();
                                item.SchemaName = reader["owner"].ToString();
                                item.ObjectType = reader["object_type"].ToString();
                                item.ObjectName = reader["object_name"].ToString();
                                items.Add(item);
                            }
                        }
                    }
                }

                if (objTypesJobs.Any())
                {
                    string objectQuery = GetObjectQuery(objTypesJobs, true, schemasIncludeStr, schemasExcludeStr);
                    using (OracleCommand cmd = new OracleCommand(objectQuery, _connection))
                    {
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var item = new ObjectTypeName();
                                item.SchemaName = reader["owner"].ToString();
                                item.ObjectType = reader["object_type"].ToString();
                                item.ObjectName = reader["object_name"].ToString();
                                items.Add(item);
                            }
                        }
                    }
                }

                foreach (var item in items)
                {
                    var type = item.ObjectType == "PACKAGE BODY" ? "PACKAGE" : item.ObjectType;
                    type = GetObjectTypeNameReverse(type);
                    ObjectTypeNames resItem = res.FirstOrDefault(c =>
                        c.SchemaName == item.SchemaName && c.ObjectType == type);
                    if (resItem == null)
                    {
                        resItem = new ObjectTypeNames {SchemaName = item.SchemaName, ObjectType = type};
                        res.Add(resItem);
                    }
                    if (resItem.ObjectNames.All(c => c != item.ObjectName))
                        resItem.ObjectNames.Add(item.ObjectName);
                }

            }
            catch (Exception e)
            {
                var progressDataErr = new ExportProgressDataOuter(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }

            var progressData2 = new ExportProgressDataOuter(ExportProgressDataLevel.STAGEENDINFO, stage);
            var counter = res.Sum(c => c.ObjectNames.Count);
            progressData2.AllObjCountPlan = counter;
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

        public static string GetObjectTypeNameReverse(string typeName)
        {
            return objectTypeMapping.FirstOrDefault(c => c.Value==typeName).Key;
        }

        public string GetObjectSource(string objectName, string objectType, List<string> addSlashTo, ExportProgressDataStageOuter stage, int schemaObjCountPlan, int typeObjCountPlan, int current,out bool canceledByUser)
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
                var progressDataCancel = new ExportProgressDataOuter(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.AllObjCountPlan = schemaObjCountPlan;
                progressDataCancel.TypeObjCountPlan = typeObjCountPlan;
                progressDataCancel.Current = current;
                progressDataCancel.ObjectName = objectName;
                progressDataCancel.ObjectType = objectType;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressDataOuter(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.AllObjCountPlan = schemaObjCountPlan;
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
                var progressDataErr = new ExportProgressDataOuter(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.AllObjCountPlan = schemaObjCountPlan;
                progressDataErr.TypeObjCountPlan = typeObjCountPlan;
                progressDataErr.Current = current;
                progressDataErr.ObjectName = objectName;
                progressDataErr.ObjectType = objectType;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }

            var progressData2 = new ExportProgressDataOuter(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = 1;
            progressData2.AllObjCountPlan = schemaObjCountPlan;
            progressData2.TypeObjCountPlan = typeObjCountPlan;
            progressData2.Current = current;
            progressData2.ObjectName = objectName;
            progressData2.ObjectType = objectType;
            _progressDataManager.ReportCurrentProgress(progressData2);


            return sourceCode.ToString();
        }

        public string GetObjectQuery(List<string> objectTypesList, bool isJobs, string ownersInclude, string ownersExclude)
        {
            var strInclude = string.IsNullOrWhiteSpace(ownersInclude) ? "" : $" AND OWNER IN ({ownersInclude})";
            var strExclude = string.IsNullOrWhiteSpace(ownersExclude) ? "" : $" AND OWNER NOT IN ({ownersExclude})";
            var dbLinkAppend = string.IsNullOrWhiteSpace(_dbLink) ? "" : "@" + _dbLink;
            if (!isJobs)
            {
                var objTypesStr = objectTypesList.MergeFormatted("'", ",");
                return $"SELECT owner, object_type, object_name FROM all_objects{dbLinkAppend} WHERE OBJECT_TYPE IN ({objTypesStr}){strInclude}{strExclude}";
            }
            else
            {
                var strIncludeForDBMSJob = string.IsNullOrWhiteSpace(ownersInclude) ? "" : $" AND schema_user IN ({ownersInclude})";
                var strExcludeForDBMSJob = string.IsNullOrWhiteSpace(ownersExclude) ? "" : $" AND schema_user NOT IN ({ownersExclude})";
                return $"SELECT owner, 'SCHEDULER_JOB' object_type, job_name object_name FROM all_scheduler_jobs{dbLinkAppend} WHERE 1=1{strInclude}{strExclude}" +
                       "UNION ALL " +
                       $"SELECT schema_user owner, 'DBMS_JOB' object_type, to_char(job) object_name from all_jobs{dbLinkAppend} WHERE 1=1{strIncludeForDBMSJob}{strExcludeForDBMSJob}";
            }
        }

        public List<SynonymAttributes> GetSynonyms(ExportProgressDataStageOuter stage, int schemaObjCountPlan, int typeObjCountPlan, string objectTypeMulti, string ownersInclude, string ownersExclude, out bool canceledByUser)
        {
            var res = new List<SynonymAttributes>();

            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;

                var progressDataCancel = new ExportProgressDataOuter(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.AllObjCountPlan = schemaObjCountPlan;
                progressDataCancel.TypeObjCountPlan = typeObjCountPlan;
                progressDataCancel.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);

                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressDataOuter(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.AllObjCountPlan = schemaObjCountPlan;
            progressData.TypeObjCountPlan = typeObjCountPlan;
            progressData.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData);
            try
            {
                var strInclude = string.IsNullOrWhiteSpace(ownersInclude) ? "" : $" AND owner IN ({ownersInclude})";
                var strExclude = string.IsNullOrWhiteSpace(ownersExclude) ? "" : $" AND owner NOT IN ({ownersExclude})";
                var dbLinkAppend = string.IsNullOrWhiteSpace(_dbLink) ? "" : "@" + _dbLink;
                string ddlQuery = $"SELECT owner, synonym_name, table_owner, table_name, db_link  FROM all_synonyms{dbLinkAppend} WHERE 1=1{strInclude}{strExclude}";
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
                var progressDataErr = new ExportProgressDataOuter(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.AllObjCountPlan = schemaObjCountPlan;
                progressDataErr.TypeObjCountPlan = typeObjCountPlan;
                progressDataErr.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }

            var progressData2 = new ExportProgressDataOuter(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = res.Count;
            progressData2.AllObjCountPlan = schemaObjCountPlan;
            progressData2.TypeObjCountPlan = typeObjCountPlan;
            progressData2.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData2);

            return res;
        }

        public List<SequenceAttributes> GetSequences(ExportProgressDataStageOuter stage, int schemaObjCountPlan, int typeObjCountPlan, string objectTypeMulti, string ownersInclude, string ownersExclude, out bool canceledByUser)
        {
            var res = new List<SequenceAttributes>();

            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressDataOuter(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.AllObjCountPlan = schemaObjCountPlan;
                progressDataCancel.TypeObjCountPlan = typeObjCountPlan;
                progressDataCancel.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressDataOuter(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.AllObjCountPlan = schemaObjCountPlan;
            progressData.TypeObjCountPlan = typeObjCountPlan;
            progressData.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData);

            try
            {
                var strInclude = string.IsNullOrWhiteSpace(ownersInclude) ? "" : $" AND sequence_owner IN ({ownersInclude})";
                var strExclude = string.IsNullOrWhiteSpace(ownersExclude) ? "" : $" AND sequence_owner NOT IN ({ownersExclude})";
                var dbLinkAppend = string.IsNullOrWhiteSpace(_dbLink) ? "" : "@" + _dbLink;
                string ddlQuery = $"SELECT sequence_owner, sequence_name, min_value, max_value, increment_by, cycle_flag, order_flag, cache_size, last_number FROM all_sequences{dbLinkAppend} WHERE 1=1{strInclude}{strExclude}";
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new SequenceAttributes();
                            item.SequenceOwner = reader["sequence_owner"].ToString();
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
                var progressDataErr = new ExportProgressDataOuter(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.AllObjCountPlan = schemaObjCountPlan;
                progressDataErr.TypeObjCountPlan = typeObjCountPlan;
                progressDataErr.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }

            var progressData2 = new ExportProgressDataOuter(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = res.Count;
            progressData2.AllObjCountPlan = schemaObjCountPlan;
            progressData2.TypeObjCountPlan = typeObjCountPlan;
            progressData2.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData2);

            return res;
        }

        public List<SchedulerJob> GetSchedulerJobs(ExportProgressDataStageOuter stage, int schemaObjCountPlan, int typeObjCountPlan, string objectTypeMulti, string ownersInclude, string ownersExclude, out bool canceledByUser)
        {
            var res = new List<SchedulerJob>();

            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressDataOuter(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.AllObjCountPlan = schemaObjCountPlan;
                progressDataCancel.TypeObjCountPlan = typeObjCountPlan;
                progressDataCancel.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressDataOuter(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.AllObjCountPlan = schemaObjCountPlan;
            progressData.TypeObjCountPlan = typeObjCountPlan;
            progressData.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData);

            try
            {
                var strInclude = string.IsNullOrWhiteSpace(ownersInclude) ? "" : $" AND owner IN ({ownersInclude})";
                var strExclude = string.IsNullOrWhiteSpace(ownersExclude) ? "" : $" AND owner NOT IN ({ownersExclude})";
                var dbLinkAppend = string.IsNullOrWhiteSpace(_dbLink) ? "" : "@" + _dbLink;
                string ddlQuery = $"SELECT owner, job_name, job_type, job_action, CAST(start_date AS DATE) start_date, repeat_interval, CAST(end_date AS DATE) end_date, job_class, enabled, auto_drop, comments, number_of_arguments FROM all_scheduler_jobs{dbLinkAppend} WHERE 1=1{strInclude}{strExclude}";
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new SchedulerJob();
                            item.Owner = reader["owner"].ToString();
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

                ddlQuery = $"select owner, job_name, argument_name, argument_position, value from all_scheduler_job_args{dbLinkAppend} WHERE 1=1{strInclude}{strExclude} order by owner, job_name, argument_position";
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new SchedulerJobArgument();
                            item.Owner = reader["owner"].ToString();
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
                var progressDataErr = new ExportProgressDataOuter(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.AllObjCountPlan = schemaObjCountPlan;
                progressDataErr.TypeObjCountPlan = typeObjCountPlan;
                progressDataErr.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }

            var progressData2 = new ExportProgressDataOuter(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = res.Count;
            progressData2.AllObjCountPlan = schemaObjCountPlan;
            progressData2.TypeObjCountPlan = typeObjCountPlan;
            progressData2.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData2);

            return res;
        }

        public List<DBMSJob> GetDBMSJobs(ExportProgressDataStageOuter stage, int schemaObjCountPlan, int typeObjCountPlan, string objectTypeMulti, string ownersInclude, string ownersExclude, out bool canceledByUser)
        {
            var res = new List<DBMSJob>();

            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressDataOuter(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.AllObjCountPlan = schemaObjCountPlan;
                progressDataCancel.TypeObjCountPlan = typeObjCountPlan;
                progressDataCancel.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);

                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressDataOuter(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.AllObjCountPlan = schemaObjCountPlan;
            progressData.TypeObjCountPlan = typeObjCountPlan;
            progressData.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData);
            try
            {
                var strInclude = string.IsNullOrWhiteSpace(ownersInclude) ? "" : $" AND schema_user IN ({ownersInclude})";
                var strExclude = string.IsNullOrWhiteSpace(ownersExclude) ? "" : $" AND schema_user NOT IN ({ownersExclude})";
                var dbLinkAppend = string.IsNullOrWhiteSpace(_dbLink) ? "" : "@" + _dbLink;
                string ddlQuery = $"select schema_user, job, what, next_date, next_sec, interval from all_jobs{dbLinkAppend} WHERE 1=1{strInclude}{strExclude}";
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new DBMSJob();
                            item.SchemaUser = reader["schema_user"].ToString();
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
                var progressDataErr = new ExportProgressDataOuter(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.AllObjCountPlan = schemaObjCountPlan;
                progressDataErr.TypeObjCountPlan = typeObjCountPlan;
                progressDataErr.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }

            var progressData2 = new ExportProgressDataOuter(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = res.Count;
            progressData2.AllObjCountPlan = schemaObjCountPlan;
            progressData2.TypeObjCountPlan = typeObjCountPlan;
            progressData2.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData2);

            return res;
        }

        public List<DbObjectText> GetViews(ExportProgressDataStageOuter stage, int schemaObjCountPlan, int typeObjCountPlan, string objectTypeMulti, string ownersInclude, string ownersExclude, out bool canceledByUser)
        {
            var res = new List<DbObjectText>();

            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressDataOuter(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.AllObjCountPlan = schemaObjCountPlan;
                progressDataCancel.TypeObjCountPlan = typeObjCountPlan;
                progressDataCancel.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressDataOuter(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.AllObjCountPlan = schemaObjCountPlan;
            progressData.TypeObjCountPlan = typeObjCountPlan;
            progressData.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData);

            try
            {
                var dbLinkAppend = string.IsNullOrWhiteSpace(_dbLink) ? "" : "@" + _dbLink;
                var strInclude = string.IsNullOrWhiteSpace(ownersInclude) ? "" : $" AND OWNER IN ({ownersInclude})";
                var strExclude = string.IsNullOrWhiteSpace(ownersExclude) ? "" : $" AND OWNER NOT IN ({ownersExclude})";
                string ddlQuery = $"SELECT owner, view_name, text  FROM all_views{dbLinkAppend} WHERE 1=1{strInclude}{strExclude}";
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new DbObjectText();
                            item.Owner = reader["owner"].ToString();
                            item.Name = reader["view_name"].ToString();
                            item.Text = reader["text"].ToString().TrimEnd();
                            res.Add(item);
                        }
                    }
                }
            }
            catch (Exception e)
            {

                var progressDataErr = new ExportProgressDataOuter(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.AllObjCountPlan = schemaObjCountPlan;
                progressDataErr.TypeObjCountPlan = typeObjCountPlan;
                progressDataErr.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }

            var progressData2 = new ExportProgressDataOuter(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = res.Count;
            progressData2.AllObjCountPlan = schemaObjCountPlan;
            progressData2.TypeObjCountPlan = typeObjCountPlan;
            progressData2.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData2);

            return res;
        }

        public List<ColumnComment> GetTablesAndViewsColumnComments(ExportProgressDataStageOuter stage, int schemaObjCountPlan, string schemasIncludeStr, string schemasExcludeStr, out bool canceledByUser)
        {
            var res = new List<ColumnComment>();

            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressDataOuter(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.AllObjCountPlan = schemaObjCountPlan;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressDataOuter(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.AllObjCountPlan = schemaObjCountPlan;
            _progressDataManager.ReportCurrentProgress(progressData);

            try
            {
                var dbLinkAppend = string.IsNullOrWhiteSpace(_dbLink) ? "" : "@" + _dbLink;
                var strInclude = string.IsNullOrWhiteSpace(schemasIncludeStr) ? "" : $" AND OWNER IN ({schemasIncludeStr})";
                var strExclude = string.IsNullOrWhiteSpace(schemasExcludeStr) ? "" : $" AND OWNER NOT IN ({schemasExcludeStr})";
                string ddlQuery = $@"SELECT owner, table_name, column_name, comments  FROM ALL_COL_COMMENTS{dbLinkAppend} WHERE 1=1{strInclude}{strExclude}";
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {

                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new ColumnComment();
                            item.Owner = reader["owner"].ToString();
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
                var progressDataErr = new ExportProgressDataOuter(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.AllObjCountPlan = schemaObjCountPlan;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }

            var progressData2 = new ExportProgressDataOuter(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = res.Count;
            progressData2.AllObjCountPlan = schemaObjCountPlan;
            _progressDataManager.ReportCurrentProgress(progressData2);

            return res;
        }

        public List<TableOrViewComment> GetTableOrViewComments(string objectType, ExportProgressDataStageOuter stage, int schemaObjCountPlan, int typeObjCountPlan,  string objectTypeMulti, string ownersInclude, string ownersExclude, out bool canceledByUser)
        {
            var res = new List<TableOrViewComment>();

            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressDataOuter(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.AllObjCountPlan = schemaObjCountPlan;
                progressDataCancel.TypeObjCountPlan = typeObjCountPlan;
                progressDataCancel.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressDataOuter(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.AllObjCountPlan = schemaObjCountPlan;
            progressData.TypeObjCountPlan = typeObjCountPlan;
            progressData.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData);
            try
            {
                var dbLinkAppend = string.IsNullOrWhiteSpace(_dbLink) ? "" : "@" + _dbLink;
                var strInclude = string.IsNullOrWhiteSpace(ownersInclude) ? "" : $" AND OWNER IN ({ownersInclude})";
                var strExclude = string.IsNullOrWhiteSpace(ownersExclude) ? "" : $" AND OWNER NOT IN ({ownersExclude})";
                string ddlQuery = $@"SELECT owner, table_name, comments  FROM ALL_TAB_COMMENTS{dbLinkAppend} WHERE table_type=:objectType{strInclude}{strExclude}";
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    cmd.Parameters.Add("objectType", OracleType.VarChar).Value = objectType;
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new TableOrViewComment();
                            item.Owner = reader["owner"].ToString();
                            item.TableName = reader["table_name"].ToString();
                            item.Comments = reader["comments"].ToString();
                            res.Add(item);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var progressDataErr = new ExportProgressDataOuter(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.AllObjCountPlan = schemaObjCountPlan;
                progressDataErr.TypeObjCountPlan = typeObjCountPlan;
                progressDataErr.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }

            var progressData2 = new ExportProgressDataOuter(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = res.Count;
            progressData2.AllObjCountPlan = schemaObjCountPlan;
            progressData2.TypeObjCountPlan = typeObjCountPlan;
            progressData2.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData2);


            return res;
        }

        public List<TableStruct> GetTablesStruct (ExportProgressDataStageOuter stage, int schemaObjCountPlan, int typeObjCountPlan, string objectTypeMulti, string ownersInclude, string ownersExclude, out bool canceledByUser)
        {
            var res = new List<TableStruct>();

            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressDataOuter(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.AllObjCountPlan = schemaObjCountPlan;
                progressDataCancel.TypeObjCountPlan = typeObjCountPlan;
                progressDataCancel.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressDataOuter(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.AllObjCountPlan = schemaObjCountPlan;
            progressData.TypeObjCountPlan = typeObjCountPlan;
            progressData.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData);

            try
            {
                var dbLinkAppend = string.IsNullOrWhiteSpace(_dbLink) ? "" : "@" + _dbLink;
                var strInclude = string.IsNullOrWhiteSpace(ownersInclude) ? "" : $" AND OWNER IN ({ownersInclude})";
                var strExclude = string.IsNullOrWhiteSpace(ownersExclude) ? "" : $" AND OWNER NOT IN ({ownersExclude})";
                string ddlQuery = $@"SELECT owner, table_name, partitioned, temporary, duration, compression, iot_type, logging, dependencies FROM ALL_TABLES{dbLinkAppend} WHERE 1=1{strInclude}{strExclude}";
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new TableStruct();
                            item.Owner = reader["owner"].ToString();
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
                var progressDataErr = new ExportProgressDataOuter(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.AllObjCountPlan = schemaObjCountPlan;
                progressDataErr.TypeObjCountPlan = typeObjCountPlan;
                progressDataErr.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }
            var progressData2 = new ExportProgressDataOuter(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = res.Count;
            progressData2.AllObjCountPlan = schemaObjCountPlan;
            progressData2.TypeObjCountPlan = typeObjCountPlan;
            progressData2.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData2);
            return res;
        }

        public List<TableColumnStruct> GetTablesAndViewsColumnsStruct(ExportProgressDataStageOuter stage, int schemaObjCountPlan, string schemasIncludeStr, string schemasExcludeStr,  Dictionary<string, List<string>> systemViewInfo, out bool canceledByUser)
        {
            var res = new List<TableColumnStruct>();

            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressDataOuter(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.AllObjCountPlan = schemaObjCountPlan;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressDataOuter(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.AllObjCountPlan = schemaObjCountPlan;
            _progressDataManager.ReportCurrentProgress(progressData);

            try
            {
                var existsIdentityColumnsView = systemViewInfo["USER_TAB_IDENTITY_COLS"] != null;
                var existsDefaultOnNullColumn = systemViewInfo["USER_TAB_COLS"].Any(c => c == "DEFAULT_ON_NULL");
                var addDefalutOnNullSelect = existsDefaultOnNullColumn ? ", default_on_null" : "";

                var dbLinkAppend = string.IsNullOrWhiteSpace(_dbLink) ? "" : "@" + _dbLink;
                var strInclude = string.IsNullOrWhiteSpace(schemasIncludeStr) ? "" : $" AND OWNER IN ({schemasIncludeStr})";
                var strExclude = string.IsNullOrWhiteSpace(schemasExcludeStr) ? "" : $" AND OWNER NOT IN ({schemasExcludeStr})";

                string ddlQuery = $@"SELECT owner, table_name, column_name, data_type, data_type_owner, data_length, char_length,  char_col_decl_length, data_precision, data_scale, nullable, column_id, data_default, hidden_column, virtual_column, char_used{addDefalutOnNullSelect} FROM ALL_TAB_COLS{dbLinkAppend} WHERE 1=1{strInclude}{strExclude}";
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new TableColumnStruct();
                            item.Owner = reader["owner"].ToString();
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
                    ddlQuery = $@"SELECT owner, table_name, column_name, generation_type, identity_options FROM all_tab_identity_cols{dbLinkAppend} WHERE 1=1{strInclude}{strExclude}";
                    using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                    {
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var item = new TableIdentityColumnStruct();
                                item.Owner = reader["owner"].ToString();
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
                var progressDataErr = new ExportProgressDataOuter(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.AllObjCountPlan = schemaObjCountPlan;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }

            var progressData2 = new ExportProgressDataOuter(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = res.Count;
            progressData2.AllObjCountPlan = schemaObjCountPlan;
            _progressDataManager.ReportCurrentProgress(progressData2);

            return res;
        }

        public List<PartTables> GetTablesPartitions(GetPartitionMode getPartitionMode, ExportProgressDataStageOuter stage, int schemaObjCountPlan, int typeObjCountPlan,  string objectTypeMulti, Dictionary<string, List<string>> systemViewInfo, string ownersInclude, string ownersExclude, out bool canceledByUser)
        {
            var res = new List<PartTables>();



            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressDataOuter(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.AllObjCountPlan = schemaObjCountPlan;
                progressDataCancel.TypeObjCountPlan = typeObjCountPlan;
                progressDataCancel.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            if (getPartitionMode == GetPartitionMode.NONE)
                return res;

            var progressData = new ExportProgressDataOuter(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.AllObjCountPlan = schemaObjCountPlan;
            progressData.TypeObjCountPlan = typeObjCountPlan;
            progressData.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData);

            var existsIntervalColumn = systemViewInfo["USER_PART_TABLES"].Any(c => c == "INTERVAL");

            try
            {
                //USER_PART_TABLES
                var addIntervalColumn = existsIntervalColumn ? ", interval" : "";
                var dbLinkAppend = string.IsNullOrWhiteSpace(_dbLink) ? "" : "@" + _dbLink;
                var strInclude = string.IsNullOrWhiteSpace(ownersInclude) ? "" : $" AND OWNER IN ({ownersInclude})";
                var strExclude = string.IsNullOrWhiteSpace(ownersExclude) ? "" : $" AND OWNER NOT IN ({ownersExclude})";
                string ddlQuery =
                    $@"SELECT owner, table_name, partitioning_type, subpartitioning_type, partition_count, def_subpartition_count, partitioning_key_count, subpartitioning_key_count, def_tablespace_name{addIntervalColumn} FROM ALL_PART_TABLES{dbLinkAppend} WHERE 1=1{strInclude}{strExclude}";
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new PartTables();
                            item.Owner = reader["owner"].ToString();
                            item.TableName = reader["table_name"].ToString();
                            item.PartitioningType = reader["partitioning_type"].ToString();
                            item.SubPartitioningType = reader["subpartitioning_type"].ToString();
                            if (!reader.IsDBNull(reader.GetOrdinal("partition_count")))
                                item.PartitionCount = reader.GetInt32(reader.GetOrdinal("partition_count"));
                            if (!reader.IsDBNull(reader.GetOrdinal("def_subpartition_count")))
                                item.DefSubPartitionCount =
                                    reader.GetInt32(reader.GetOrdinal("def_subpartition_count"));
                            if (!reader.IsDBNull(reader.GetOrdinal("partitioning_key_count")))
                                item.PartitioningKeyCount =
                                    reader.GetInt32(reader.GetOrdinal("partitioning_key_count"));
                            if (!reader.IsDBNull(reader.GetOrdinal("subpartitioning_key_count")))
                                item.SubPartitioningKeyCount =
                                    reader.GetInt32(reader.GetOrdinal("subpartitioning_key_count"));
                            item.DefTableSpaceName = reader["def_tablespace_name"].ToString();
                            if (existsIntervalColumn)
                                item.Interval = reader["interval"].ToString();
                            res.Add(item);
                        }
                    }
                }

                //USER_PART_KEY_COLUMNS
                ddlQuery =
                    $@"SELECT owner, name, column_name, column_position FROM ALL_PART_KEY_COLUMNS{dbLinkAppend} where object_type='TABLE'{strInclude}{strExclude}";
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new PartOrSubPartKeyColumns();
                            item.Owner = reader["owner"].ToString();
                            item.TableName = reader["name"].ToString();
                            item.ColumnName = reader["column_name"].ToString();
                            if (!reader.IsDBNull(reader.GetOrdinal("column_position")))
                                item.ColumnPosition = reader.GetInt32(reader.GetOrdinal("column_position"));
                            var table = res.FirstOrDefault(c =>c.Owner == item.Owner &&  c.TableName == item.TableName);
                            if (table != null)
                                table.PartKeyColumns.Add(item);
                        }
                    }
                }

                //USER_SUBPART_KEY_COLUMNS
                ddlQuery =
                    $@"SELECT owner, name, column_name, column_position FROM ALL_SUBPART_KEY_COLUMNS{dbLinkAppend} where object_type='TABLE'{strInclude}{strExclude}";
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new PartOrSubPartKeyColumns();
                            item.Owner = reader["owner"].ToString();
                            item.TableName = reader["name"].ToString();
                            item.ColumnName = reader["column_name"].ToString();
                            if (!reader.IsDBNull(reader.GetOrdinal("column_position")))
                                item.ColumnPosition = reader.GetInt32(reader.GetOrdinal("column_position"));
                            var table = res.FirstOrDefault(c => c.Owner == item.Owner && c.TableName == item.TableName);
                            if (table != null)
                                table.SubPartKeyColumns.Add(item);
                        }
                    }
                }

                //USER_TAB_PARTITIONS
                strInclude = string.IsNullOrWhiteSpace(ownersInclude) ? "" : $" AND table_owner IN ({ownersInclude})";
                strExclude = string.IsNullOrWhiteSpace(ownersExclude) ? "" : $" AND table_owner NOT IN ({ownersExclude})";
                var addRestrStr = getPartitionMode == GetPartitionMode.ONLYDEFPART ? "partition_position=1" : "1=1";
                ddlQuery =
                    $@"SELECT table_owner, table_name, partition_name, subpartition_count, high_value, partition_position, tablespace_name FROM ALL_TAB_PARTITIONS{dbLinkAppend} WHERE {addRestrStr}{strInclude}{strExclude}";
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new TabPartitions();
                            item.TableOwner = reader["table_owner"].ToString();
                            item.TableName = reader["table_name"].ToString();
                            item.PartitionName = reader["partition_name"].ToString();
                            if (!reader.IsDBNull(reader.GetOrdinal("subpartition_count")))
                                item.SubPartitionCount = reader.GetInt32(reader.GetOrdinal("subpartition_count"));
                            item.HighValue = reader["high_value"].ToString();
                            if (!reader.IsDBNull(reader.GetOrdinal("partition_position")))
                                item.PartitionPosition = reader.GetInt32(reader.GetOrdinal("partition_position"));
                            item.TableSpaceName = reader["tablespace_name"].ToString();
                            var table = res.FirstOrDefault(c => c.Owner == item.TableOwner &&  c.TableName == item.TableName);
                            if (table != null)
                                table.Partitions.Add(item);
                        }
                    }
                }

                //USER_TAB_SUBPARTITIONS
                addRestrStr = getPartitionMode == GetPartitionMode.ONLYDEFPART ? "subpartition_position=1" : "1=1";
                ddlQuery =
                    $@"SELECT table_owner, table_name, partition_name, subpartition_name, high_value, subpartition_position, tablespace_name FROM ALL_TAB_SUBPARTITIONS{dbLinkAppend} WHERE {addRestrStr}{strInclude}{strExclude}";
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new TabSubPartitions();
                            item.TableOwner = reader["table_owner"].ToString();
                            item.TableName = reader["table_name"].ToString();
                            item.PartitionName = reader["partition_name"].ToString();
                            item.SubPartitionName = reader["subpartition_name"].ToString();
                            item.HighValue = reader["high_value"].ToString();
                            if (!reader.IsDBNull(reader.GetOrdinal("subpartition_position")))
                                item.SubPartitionPosition =
                                    reader.GetInt32(reader.GetOrdinal("subpartition_position"));
                            item.TableSpaceName = reader["tablespace_name"].ToString();
                            var table = res.FirstOrDefault(c => c.Owner == item.TableOwner && c.TableName == item.TableName);
                            if (table != null)
                            {
                                var partition =
                                    table.Partitions.FirstOrDefault(c => c.PartitionName == item.PartitionName);
                                if (partition != null)
                                    partition.SubPartitions.Add(item);
                            }
                        }
                    }
                }

            }
            catch (Exception e)
            {
                var progressDataErr = new ExportProgressDataOuter(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.AllObjCountPlan = schemaObjCountPlan;
                progressDataErr.TypeObjCountPlan = typeObjCountPlan;
                progressDataErr.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }

            var progressData2 = new ExportProgressDataOuter(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = res.Count;
            progressData2.AllObjCountPlan = schemaObjCountPlan;
            progressData2.TypeObjCountPlan = typeObjCountPlan;
            progressData2.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData2);

            return res;
        }

        public Dictionary<string,List<string>> GetInfoAboutSystemViews(List<string> viewNames, ExportProgressDataStageOuter stage, out bool canceledByUser)
        {
            var res = new Dictionary<string, List<string>>();
            var infoForProgress = new StringBuilder();
            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressDataOuter(ExportProgressDataLevel.CANCEL, stage);
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressDataOuter(ExportProgressDataLevel.STAGESTARTINFO, stage);
            _progressDataManager.ReportCurrentProgress(progressData);

            try
            {
                if (viewNames.Any())
                {
                    var tmpAr = new List<Tuple<string,string>>();
                    var names = viewNames.MergeFormatted("'", ",");
                    var dbLinkAppend = string.IsNullOrWhiteSpace(_dbLink) ? "" : "@" + _dbLink;
                    string ddlQuery = $@"SELECT table_name, column_name FROM all_tab_cols{dbLinkAppend} WHERE owner = 'SYS' AND table_name in ({names})";
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
                var progressDataErr = new ExportProgressDataOuter(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }


            var progressData2 = new ExportProgressDataOuter(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = res.Count;
            progressData2.SetTextAddInfo("SYSTEM_VIEW_INFO",infoForProgress.ToString());
            _progressDataManager.ReportCurrentProgress(progressData2);

            return res;
        }

        public List<SchemaWorkStatOuter> GetStat(int getStatForLastDays, string prefix)
        {
            var res = new List<SchemaWorkStatOuter>();
            try
            {
                string ddlQuery =
                    $@"select process_id, dbid, username, dblink, stage, eventlevel, eventtime, errorscount, schemaobjcountfact
                    from {prefix}CONNWORKLOG where stage='PROCESS_SCHEMA' --and eventlevel='STAGEENDINFO'
                    and 
                    process_id in (select process_id from {prefix}CONNWORKLOG where stage='PROCESS_SCHEMA' and eventlevel='STAGEENDINFO' and sysdate-eventtime<:forLastDays)";
                using (OracleConnection connection = new OracleConnection(ConnectionString))
                {
                    connection.Open();
                    using (OracleCommand cmd = new OracleCommand(ddlQuery, connection))
                    {
                        cmd.Parameters.Add("forLastDays", OracleType.Number).Value = getStatForLastDays;
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ExportProgressDataStageOuter stage;
                                ExportProgressDataLevel level;
                                if (ExportProgressDataStageOuter.TryParse(reader["stage"].ToString(), true, out stage) &&
                                    ExportProgressDataLevel.TryParse(reader["eventlevel"].ToString(), true, out level))
                                {
                                    var item = new SchemaWorkStatOuter();
                                    item.ProcessId = reader.GetInt32(reader.GetOrdinal("process_id"));
                                    item.DBId = reader["dbid"].ToString().ToUpper();
                                    item.UserName = reader["username"].ToString().ToUpper();
                                    item.DbLink = reader["dblink"].ToString().ToUpper();
                                    item.Stage = stage;
                                    item.Level = level;
                                    item.EventTime = reader.GetDateTime(reader.GetOrdinal("eventtime"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("errorscount")))
                                        item.ErrorsCount = reader.GetInt32(reader.GetOrdinal("errorscount"));
                                    //if (!reader.IsDBNull(reader.GetOrdinal("schemaobjcountplan")))
                                    //    item.SchemaObjCountPlan = reader.GetInt32(reader.GetOrdinal("schemaobjcountplan"));
                                    if (!reader.IsDBNull(reader.GetOrdinal("schemaobjcountfact")))
                                        item.SchemaObjCountFact = reader.GetInt32(reader.GetOrdinal("schemaobjcountfact"));
                                    res.Add(item);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            return res;
        }


        public List<AppWorkStat> GetAppWorkStat(int getStatForLastDays, string prefix)
        {
            var res = new List<AppWorkStat>();
            try
            {
                string ddlQuery =
                    $@"select id, start_time, end_time from {prefix}PROCESS where end_time is not null and id in (select process_id from {prefix}CONNWORKLOG where stage='PROCESS_SCHEMA' and eventlevel='STAGEENDINFO' and sysdate-eventtime<:forLastDays)";
                using (OracleConnection connection = new OracleConnection(ConnectionString))
                {
                    connection.Open();
                    using (OracleCommand cmd = new OracleCommand(ddlQuery, connection))
                    {
                        cmd.Parameters.Add("forLastDays", OracleType.Number).Value = getStatForLastDays;
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {

                                var item = new AppWorkStat();
                                item.ProcessId = reader.GetInt32(reader.GetOrdinal("id"));
                                item.StartTime = reader.GetDateTime(reader.GetOrdinal("start_time"));
                                item.EndTime = reader.GetDateTime(reader.GetOrdinal("end_time"));
                                res.Add(item);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            return res;
        }

        public List<CommitStat> GetCommitStat(int getStatForLastDays, string prefix)
        {
            var res = new List<CommitStat>();
            try
            {
                string ddlQuery =
                    $@"select process_id, dbid, username, commit_common_date, is_initial, all_add_cnt, all_upd_cnt, all_del_cnt, all_add_size, all_upd_size, all_del_prev_size 
                    from {prefix}COMMITS where process_id in (select process_id from {prefix}CONNWORKLOG where stage='PROCESS_SCHEMA' and eventlevel='STAGEENDINFO' and sysdate-eventtime<:forLastDays)";
                using (OracleConnection connection = new OracleConnection(ConnectionString))
                {
                    connection.Open();
                    using (OracleCommand cmd = new OracleCommand(ddlQuery, connection))
                    {
                        cmd.Parameters.Add("forLastDays", OracleType.Number).Value = getStatForLastDays;
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {

                                var item = new CommitStat();
                                item.ProcessId = reader.GetInt32(reader.GetOrdinal("process_id"));
                                item.DBId = reader["dbid"].ToString().ToUpper();
                                item.UserName = reader["username"].ToString().ToUpper();
                                item.CommitCommonDate = reader.GetDateTime(reader.GetOrdinal("commit_common_date"));
                                item.IsInitial = reader.GetInt32(reader.GetOrdinal("is_initial")) > 0;
                                item.AllAddCnt = reader.GetInt32(reader.GetOrdinal("all_add_cnt"));
                                item.AllUpdCnt = reader.GetInt32(reader.GetOrdinal("all_upd_cnt"));
                                item.AllDelCnt = reader.GetInt32(reader.GetOrdinal("all_del_cnt"));
                                item.AllAddSize = reader.GetInt32(reader.GetOrdinal("all_add_size"));
                                item.AllUpdSize = reader.GetInt32(reader.GetOrdinal("all_upd_size"));
                                item.AllDelSize = reader.GetInt32(reader.GetOrdinal("all_del_prev_size"));
                                res.Add(item);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            return res;
        }

        public List<IndexStruct> GetTablesIndexes(ExportProgressDataStageOuter stage, int schemaObjCountPlan, int typeObjCountPlan, string objectTypeMulti, string ownersInclude, string ownersExclude, out bool canceledByUser)
        {
            var res = new List<IndexStruct>();

            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressDataOuter(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.AllObjCountPlan = schemaObjCountPlan;
                progressDataCancel.TypeObjCountPlan = typeObjCountPlan;
                progressDataCancel.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressDataOuter(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.AllObjCountPlan = schemaObjCountPlan;
            progressData.TypeObjCountPlan = typeObjCountPlan;
            progressData.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData);

            try
            {
                var dbLinkAppend = string.IsNullOrWhiteSpace(_dbLink) ? "" : "@" + _dbLink;
                var strInclude = string.IsNullOrWhiteSpace(ownersInclude) ? "" : $" AND i.OWNER IN ({ownersInclude})";
                var strExclude = string.IsNullOrWhiteSpace(ownersExclude) ? "" : $" AND i.OWNER NOT IN ({ownersExclude})";
                string ddlQuery = $@"SELECT i.owner, i.table_name, i.index_name, i.index_type, i.uniqueness, i.compression, i.prefix_length, i.logging, p.locality, i.ityp_owner, i.ityp_name  
            from ALL_INDEXES{dbLinkAppend} i 
            LEFT JOIN ALL_PART_INDEXES{dbLinkAppend} p ON i.owner=p.owner and i.TABLE_NAME=p.table_name and i.INDEX_NAME = p.INDEX_NAME
            WHERE i.TABLE_TYPE='TABLE'{strInclude}{strExclude}";
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new IndexStruct();
                            item.Owner = reader["owner"].ToString();
                            item.TableName = reader["table_name"].ToString();
                            item.IndexName = reader["index_name"].ToString();
                            item.IndexType = reader["index_type"].ToString();
                            item.Uniqueness = reader["uniqueness"].ToString();
                            item.Compression = reader["compression"].ToString();
                            if (!reader.IsDBNull(reader.GetOrdinal("prefix_length")))
                                item.PrefixLength = reader.GetInt32(reader.GetOrdinal("prefix_length"));
                            item.Logging = reader["logging"].ToString();
                            item.Locality = reader["locality"].ToString();
                            item.ItypOwner = reader["ityp_owner"].ToString();
                            item.ItypName = reader["ityp_name"].ToString();
                            res.Add(item);
                        }
                    }
                }
                strInclude = string.IsNullOrWhiteSpace(ownersInclude) ? "" : $" AND table_owner IN ({ownersInclude})";
                strExclude = string.IsNullOrWhiteSpace(ownersExclude) ? "" : $" AND table_owner NOT IN ({ownersExclude})";
                ddlQuery = $@"select table_owner, table_name, index_name, column_name, column_position, descend  from ALL_IND_COLUMNS{dbLinkAppend} WHERE 1=1{strInclude}{strExclude}";
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new IndexColumnStruct();
                            item.TableOwner = reader["table_owner"].ToString();
                            item.TableName = reader["table_name"].ToString();
                            item.IndexName = reader["index_name"].ToString();
                            item.ColumnName = reader["column_name"].ToString();
                            if (!reader.IsDBNull(reader.GetOrdinal("column_position")))
                                item.ColumnPosition = reader.GetInt32(reader.GetOrdinal("column_position"));
                            item.Descend = reader["descend"].ToString();
                            var index = res.FirstOrDefault(c => c.Owner == item.TableOwner &&  c.TableName == item.TableName && c.IndexName == item.IndexName);
                            if (index != null)
                                index.IndexColumnStructs.Add(item);
                        }
                    }
                }
                ddlQuery = $@"select table_owner, table_name, index_name, column_expression, column_position from ALL_IND_EXPRESSIONS{dbLinkAppend} WHERE 1=1{strInclude}{strExclude}";
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var tableOwner = reader["table_owner"].ToString().ToUpper();
                            var tableName = reader["table_name"].ToString().ToUpper();
                            var indexName = reader["index_name"].ToString().ToUpper();
                            int? columnPosition = null;
                            if (!reader.IsDBNull(reader.GetOrdinal("column_position")))
                                columnPosition = reader.GetInt32(reader.GetOrdinal("column_position"));
                            if (columnPosition != null)
                            {
                                var index = res.FirstOrDefault(c =>
                                    c.Owner == tableOwner && c.TableName.ToUpper() == tableName && c.IndexName.ToUpper() == indexName);
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
                var progressDataErr = new ExportProgressDataOuter(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.AllObjCountPlan = schemaObjCountPlan;
                progressDataErr.TypeObjCountPlan = typeObjCountPlan;
                progressDataErr.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }

            var progressData2 = new ExportProgressDataOuter(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = res.Count;
            progressData2.AllObjCountPlan = schemaObjCountPlan;
            progressData2.TypeObjCountPlan = typeObjCountPlan;
            progressData2.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData2);

            return res;
        }

        public List<ConstraintStruct> GetTablesConstraints(/*string schemaName, */ExportProgressDataStageOuter stage, int schemaObjCountPlan, int typeObjCountPlan, string objectTypeMulti,/* string ownersInclude, string ownersExclude, */out bool canceledByUser)
        {
            var res = new List<ConstraintStruct>();

            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressDataOuter(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.AllObjCountPlan = schemaObjCountPlan;
                progressDataCancel.TypeObjCountPlan = typeObjCountPlan;
                progressDataCancel.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressDataOuter(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.AllObjCountPlan = schemaObjCountPlan;
            progressData.TypeObjCountPlan = typeObjCountPlan;
            progressData.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData);

            try
            {
                var dbLinkAppend = string.IsNullOrWhiteSpace(_dbLink) ? "" : "@" + _dbLink;
                //var strInclude = string.IsNullOrWhiteSpace(ownersInclude) ? "" : $" AND OWNER IN ({ownersInclude})";
                //var strExclude = string.IsNullOrWhiteSpace(ownersExclude) ? "" : $" AND OWNER NOT IN ({ownersExclude})";
                string ddlQuery = $@"select owner, table_name, constraint_name, constraint_type, status, validated, search_condition, generated, r_owner, r_constraint_name, delete_rule from ALL_CONSTRAINTS{dbLinkAppend}";
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
                ddlQuery = $@"select owner, table_name, constraint_name, column_name, position from ALL_CONS_COLUMNS{dbLinkAppend}";
                using (OracleCommand cmd = new OracleCommand(ddlQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new ConstraintColumnStruct();
                            item.Owner = reader["owner"].ToString();
                            item.TableName = reader["table_name"].ToString();
                            item.ConstraintName = reader["constraint_name"].ToString();
                            item.ColumnName = reader["column_name"].ToString();
                            if (!reader.IsDBNull(reader.GetOrdinal("position")))
                                item.Position = reader.GetInt32(reader.GetOrdinal("position"));
                            var constraint = res.FirstOrDefault(c => c.Owner == item.Owner && c.TableName == item.TableName && c.ConstraintName == item.ConstraintName);
                            if (constraint != null)
                                constraint.ConstraintColumnStructs.Add(item);
                        }
                    }
                }

                foreach (var constraint in res.Where(c => !string.IsNullOrWhiteSpace(c.RConstraintName)))
                {

                    var outerConstraint = res.FirstOrDefault(c =>
                        c.ConstraintName == constraint.RConstraintName &&
                        (string.IsNullOrWhiteSpace(constraint.ROwner) || c.Owner == constraint.ROwner));
                    constraint.ReferenceConstraint = outerConstraint;
                }
            }
            catch (Exception e)
            {
                var progressDataErr = new ExportProgressDataOuter(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.AllObjCountPlan = schemaObjCountPlan;
                progressDataErr.TypeObjCountPlan = typeObjCountPlan;
                progressDataErr.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }

            var progressData2 = new ExportProgressDataOuter(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = res.Count;
            progressData2.AllObjCountPlan = schemaObjCountPlan;
            progressData2.TypeObjCountPlan = typeObjCountPlan;
            progressData2.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData2);

            return res;
        }

        public List<DbObjectText> GetObjectsSourceByType(string objectType, /*string schemaName,*/ExportProgressDataStageOuter stage, int schemaObjCountPlan, int typeObjCountPlan, string objectTypeMulti, string schemasIncludeStr, string schemasExcludeStr,  out bool canceledByUser)
        {
            var res = new List<DbObjectText>();

            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressDataOuter(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.AllObjCountPlan = schemaObjCountPlan;
                progressDataCancel.TypeObjCountPlan = typeObjCountPlan;
                progressDataCancel.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressDataOuter(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.AllObjCountPlan = schemaObjCountPlan;
            progressData.TypeObjCountPlan = typeObjCountPlan;
            progressData.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData);

            try
            {
                var dbLinkAppend = string.IsNullOrWhiteSpace(_dbLink) ? "" : "@" + _dbLink;
                var strInclude = string.IsNullOrWhiteSpace(schemasIncludeStr) ? "" : $" AND OWNER IN ({schemasIncludeStr})";
                var strExclude = string.IsNullOrWhiteSpace(schemasExcludeStr) ? "" : $" AND OWNER NOT IN ({schemasExcludeStr})";
                var sourceQuery =
                    $"SELECT owner, name, text, line FROM all_source{dbLinkAppend} WHERE type = :objectType{strInclude}{strExclude}";
                var linesAr = new List<DbObjectTextByLines>();
                

                using (OracleCommand cmd = new OracleCommand(sourceQuery, _connection))
                {
                    cmd.Parameters.Add("objectType", OracleType.VarChar).Value = objectType;

                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        var curObjName = "";
                        while (reader.Read())
                        {
                            var newItem = new DbObjectTextByLines();
                            newItem.Owner = reader["owner"].ToString();
                            newItem.Name = reader["name"].ToString();
                            newItem.Text = reader["text"].ToString();
                            if (!reader.IsDBNull(reader.GetOrdinal("line")))
                            {
                                newItem.Line = reader.GetInt32(reader.GetOrdinal("line"));

                                //пока уберем эту возможность, позже возможно вернемся
                                /*if (newItem.Line == 1)
                                {
                                    //иногда в первой строке встречается имя схемы, например:
                                    //TRIGGER MCA_FLEX.NEWDOCUM
                                    //или
                                    //TRIGGER "MCA_FLEX".tiVIS_ASKTS4_CONTROLS BEFORE INSERT ON VIS_ASKTS4_CONTROLS
                                    if (newItem.Text.Contains(schemaName))
                                    {
                                        newItem.Text = newItem.Text.Replace($" {schemaName}.", " ")
                                            .Replace($@" ""{schemaName}"".", " ");
                                    }
                                }*/
                            }

                            linesAr.Add(newItem);
                        }
                    }
                }
                StringBuilder sourceCode = new StringBuilder();
                foreach (var owner in linesAr.GroupBy(c=>c.Owner))
                {
                    foreach (var name in  owner.GroupBy(c=>c.Name))
                    {
                        foreach (var item in name.OrderBy(c=>c.Line))
                        {
                            sourceCode.Append(item.Text);
                        }
                        res.Add(new DbObjectText
                        {
                            Owner = owner.Key,
                            Name = name.Key,
                            Text = sourceCode.ToString()
                        });
                        sourceCode.Clear();
                    }
                }
            }
            catch (Exception e)
            {
                var progressDataErr = new ExportProgressDataOuter(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.AllObjCountPlan = schemaObjCountPlan;
                progressDataErr.TypeObjCountPlan = typeObjCountPlan;
                progressDataErr.ObjectType = objectTypeMulti;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }
            var progressData2 = new ExportProgressDataOuter(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = res.Count;
            progressData2.AllObjCountPlan = schemaObjCountPlan;
            progressData2.TypeObjCountPlan = typeObjCountPlan;
            progressData2.ObjectType = objectTypeMulti;
            _progressDataManager.ReportCurrentProgress(progressData2);
            return res;
        }

        public List<GrantAttributes> GetAllObjectsGrants( string schemasIncludeStr, string schemasExcludeStr, List<string> skipGrantOptions, ExportProgressDataStageOuter stage, int schemaObjCountPlan, out bool canceledByUser)
        {
            var res = new List<GrantAttributes>();

            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressDataOuter(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.AllObjCountPlan = schemaObjCountPlan;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressDataOuter(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.AllObjCountPlan = schemaObjCountPlan;
            _progressDataManager.ReportCurrentProgress(progressData);

            try
            {
                var dbLinkAppend = string.IsNullOrWhiteSpace(_dbLink) ? "" : "@" + _dbLink;
                var strInclude = string.IsNullOrWhiteSpace(schemasIncludeStr) ? "" : $" AND table_schema IN ({schemasIncludeStr})";
                var strExclude = string.IsNullOrWhiteSpace(schemasExcludeStr) ? "" : $" AND table_schema NOT IN ({schemasExcludeStr})";
                string grantQuery = $@"SELECT table_schema, table_name, grantee, privilege, grantable, hierarchy FROM all_tab_privs{dbLinkAppend} WHERE 1=1{strInclude}{strExclude}";
                using (OracleCommand cmd = new OracleCommand(grantQuery, _connection))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!skipGrantOptions.Contains(reader["privilege"].ToString().ToUpper()))
                            {
                                var item = new GrantAttributes();
                                item.ObjectSchema = reader["table_schema"].ToString();
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
                var progressDataErr = new ExportProgressDataOuter(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.AllObjCountPlan = schemaObjCountPlan;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }

            var progressData2 = new ExportProgressDataOuter(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = res.Count;
            progressData2.AllObjCountPlan = schemaObjCountPlan;
            _progressDataManager.ReportCurrentProgress(progressData2);

            return res;
        }


        public DbLinkAttrigutes GetCurDbLink(string dbLinkName, ExportProgressDataStageOuter stage, out bool canceledByUser)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressDataOuter(ExportProgressDataLevel.CANCEL, stage);
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressDataOuter(ExportProgressDataLevel.STAGESTARTINFO, stage);
            _progressDataManager.ReportCurrentProgress(progressData);

            try
            {
                string grantQuery = @"SELECT owner, username, host FROM all_db_links WHERE db_link = :db_link";
                using (OracleCommand cmd = new OracleCommand(grantQuery, _connection))
                {
                    cmd.Parameters.Add("db_link", OracleType.VarChar).Value = dbLinkName.ToUpper();
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var item = new DbLinkAttrigutes();
                                item.Owner = reader["owner"].ToString();
                                item.UserName = reader["username"].ToString();
                                item.Host = reader["host"].ToString();
                                item.DbLink = dbLinkName.ToUpper();
                                return item;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var progressDataErr = new ExportProgressDataOuter(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }

            var progressData2 = new ExportProgressDataOuter(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = 1;
            _progressDataManager.ReportCurrentProgress(progressData2);
            return null;
        }

        public string GetObjectDdl(string objectTypeMulti, string objectName, bool setSequencesValuesTo1, List<string> addSlashTo, ExportProgressDataStageOuter stage, int schemaObjCountPlan, int typeObjCountPlan, int current, out bool canceledByUser)
        {
            var ddl = "";
            var dbLinkAppend = string.IsNullOrWhiteSpace(_dbLink) ? "" : "@" + _dbLink;

            string ddlQuery = $@"SELECT dbms_metadata.get_ddl{dbLinkAppend}(:objectType, :objectName) AS ddl FROM dual";

            string dbObjectType = GetObjectTypeName(objectTypeMulti);

            if (_cancellationToken.IsCancellationRequested)
            {
                canceledByUser = true;
                var progressDataCancel = new ExportProgressDataOuter(ExportProgressDataLevel.CANCEL, stage);
                progressDataCancel.AllObjCountPlan = schemaObjCountPlan;
                progressDataCancel.TypeObjCountPlan = typeObjCountPlan;
                progressDataCancel.Current = current;
                progressDataCancel.ObjectType = objectTypeMulti;
                progressDataCancel.ObjectName = objectName;
                _progressDataManager.ReportCurrentProgress(progressDataCancel);
                return null;
            }
            canceledByUser = false;

            var progressData = new ExportProgressDataOuter(ExportProgressDataLevel.STAGESTARTINFO, stage);
            progressData.AllObjCountPlan = schemaObjCountPlan;
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
                var progressDataErr = new ExportProgressDataOuter(ExportProgressDataLevel.ERROR, stage);
                progressDataErr.Error = e.Message;
                progressDataErr.ErrorDetails = e.StackTrace;
                progressDataErr.AllObjCountPlan = schemaObjCountPlan;
                progressDataErr.TypeObjCountPlan = typeObjCountPlan;
                progressDataErr.Current = current;
                progressDataErr.ObjectType = objectTypeMulti;
                progressDataErr.ObjectName = objectName;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }

            var progressData2 = new ExportProgressDataOuter(ExportProgressDataLevel.STAGEENDINFO, stage);
            progressData2.MetaObjCountFact = 1;
            progressData2.AllObjCountPlan = schemaObjCountPlan;
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
                var progressData = new ExportProgressDataOuter(ExportProgressDataLevel.ERROR, ExportProgressDataStageOuter.PROCESS_SCHEMA);
                progressData.Error = ex.Message;
                progressData.ErrorDetails = ex.StackTrace;
                _progressDataManager.ReportCurrentProgress(progressData);
            }
            
        }

        public void SaveNewProcessInDBLog(DateTime currentDateTime, string prefix, out string id)
        {
            id = "";
            try
            {
                var query =
                    $"insert into {prefix}PROCESS (id, connections_to_process, start_time) values ({prefix}PROCESS_seq.Nextval, 1,:start_time) returning id into :new_id";

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
                var progressDataErr = new ExportProgressDataOuter(ExportProgressDataLevel.ERROR, ExportProgressDataStageOuter.PROCESS_MAIN);
                progressDataErr.Error = ex.Message;
                progressDataErr.ErrorDetails = ex.StackTrace;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }
        }

        public void UpdateProcessInDBLog(DateTime currentDateTime, string prefix, ExportProgressDataOuter progressData)
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
                var progressDataErr = new ExportProgressDataOuter(ExportProgressDataLevel.ERROR, ExportProgressDataStageOuter.PROCESS_MAIN);
                progressDataErr.Error = ex.Message;
                progressDataErr.ErrorDetails = ex.StackTrace;
                _progressDataManager.ReportCurrentProgress(progressDataErr);
            }
        }

        public static void AddCommitTypeOperOracleParams(OracleCommand cmd, string objTypePrefix, OracleObjectType? objType, RepoChangeCommitGroupInfo info)
        {
            int addCnt = 0;
            int updCnt = 0;
            int delCnt = 0; 
            long addSize = 0;
            long updSize = 0;
            long delPrevSize = 0;

            var items = info.OperationsList.Where(c =>
                c.Operation == RepoOperation.ADD && (c.ObjectType == objType || objType == null)).ToList();
            if (items.Any())
            {
                addCnt = items.Sum(c=>c.ChangesCount);
                addSize = items.Sum(c => c.FilesSize);
            }

            items = info.OperationsList.Where(c =>
                c.Operation == RepoOperation.UPD && (c.ObjectType == objType || objType == null)).ToList();
            if (items != null)
            {
                updCnt = items.Sum(c => c.ChangesCount);
                updSize = items.Sum(c => c.FilesSize);
            }

            items = info.OperationsList.Where(c =>
                c.Operation == RepoOperation.DEL && (c.ObjectType == objType || objType == null)).ToList();
            if (items != null)
            {
                delCnt = items.Sum(c => c.ChangesCount);
                delPrevSize = items.Sum(c => c.FilesSize);
            }

            cmd.Parameters.Add($"{objTypePrefix}_add_cnt", OracleType.Number).Value = addCnt;
            cmd.Parameters.Add($"{objTypePrefix}_add_size", OracleType.Number).Value = addSize;
            cmd.Parameters.Add($"{objTypePrefix}_upd_cnt", OracleType.Number).Value = updCnt;
            cmd.Parameters.Add($"{objTypePrefix}_upd_size", OracleType.Number).Value = updSize;
            cmd.Parameters.Add($"{objTypePrefix}_del_cnt", OracleType.Number).Value = delCnt;
            cmd.Parameters.Add($"{objTypePrefix}_del_prev_size", OracleType.Number).Value = delPrevSize;
        }


        public static void SaveRepoChangesInDB(string prefix, ExportProgressDataOuter progressData, string connectionString, /*DBLog dbLogSettings,*/ bool saveDetails)
        {

            var repoChanges = progressData.RepoChanges;
            if (repoChanges != null && repoChanges.Any())
            {
                var query =
                $@"insert into {prefix}COMMITS (id, process_id, dbid, username, commit_common_date, is_initial, 
                    dbl_add_cnt, dbl_add_size, dbl_upd_cnt, dbl_upd_size, dbl_del_cnt, dbl_del_prev_size, 
                    dbj_add_cnt, dbj_add_size, dbj_upd_cnt, dbj_upd_size, dbj_del_cnt, dbj_del_prev_size, 
                    fnc_add_cnt, fnc_add_size, fnc_upd_cnt, fnc_upd_size, fnc_del_cnt, fnc_del_prev_size, 
                    pkg_add_cnt, pkg_add_size, pkg_upd_cnt, pkg_upd_size, pkg_del_cnt, pkg_del_prev_size, 
                    prc_add_cnt, prc_add_size, prc_upd_cnt, prc_upd_size, prc_del_cnt, prc_del_prev_size, 
                    scj_add_cnt, scj_add_size, scj_upd_cnt, scj_upd_size, scj_del_cnt, scj_del_prev_size, 
                    seq_add_cnt, seq_add_size, seq_upd_cnt, seq_upd_size, seq_del_cnt, seq_del_prev_size, 
                    syn_add_cnt, syn_add_size, syn_upd_cnt, syn_upd_size, syn_del_cnt, syn_del_prev_size, 
                    tab_add_cnt, tab_add_size, tab_upd_cnt, tab_upd_size, tab_del_cnt, tab_del_prev_size, 
                    trg_add_cnt, trg_add_size, trg_upd_cnt, trg_upd_size, trg_del_cnt, trg_del_prev_size, 
                    tps_add_cnt, tps_add_size, tps_upd_cnt, tps_upd_size, tps_del_cnt, tps_del_prev_size, 
                    viw_add_cnt, viw_add_size, viw_upd_cnt, viw_upd_size, viw_del_cnt, viw_del_prev_size, 
                    all_add_cnt, all_add_size, all_upd_cnt, all_upd_size, all_del_cnt, all_del_prev_size) values 
                    ({prefix}COMMITS_SEQ.Nextval, :process_id, :dbid, :username, :commit_common_date, :is_initial,
                    :dbl_add_cnt, :dbl_add_size, :dbl_upd_cnt, :dbl_upd_size, :dbl_del_cnt, :dbl_del_prev_size,
                    :dbj_add_cnt, :dbj_add_size, :dbj_upd_cnt, :dbj_upd_size, :dbj_del_cnt, :dbj_del_prev_size,
                    :fnc_add_cnt, :fnc_add_size, :fnc_upd_cnt, :fnc_upd_size, :fnc_del_cnt, :fnc_del_prev_size,
                    :pkg_add_cnt, :pkg_add_size, :pkg_upd_cnt, :pkg_upd_size, :pkg_del_cnt, :pkg_del_prev_size,
                    :prc_add_cnt, :prc_add_size, :prc_upd_cnt, :prc_upd_size, :prc_del_cnt, :prc_del_prev_size,
                    :scj_add_cnt, :scj_add_size, :scj_upd_cnt, :scj_upd_size, :scj_del_cnt, :scj_del_prev_size,
                    :seq_add_cnt, :seq_add_size, :seq_upd_cnt, :seq_upd_size, :seq_del_cnt, :seq_del_prev_size,
                    :syn_add_cnt, :syn_add_size, :syn_upd_cnt, :syn_upd_size, :syn_del_cnt, :syn_del_prev_size,
                    :tab_add_cnt, :tab_add_size, :tab_upd_cnt, :tab_upd_size, :tab_del_cnt, :tab_del_prev_size,
                    :trg_add_cnt, :trg_add_size, :trg_upd_cnt, :trg_upd_size, :trg_del_cnt, :trg_del_prev_size,
                    :tps_add_cnt, :tps_add_size, :tps_upd_cnt, :tps_upd_size, :tps_del_cnt, :tps_del_prev_size,
                    :viw_add_cnt, :viw_add_size, :viw_upd_cnt, :viw_upd_size, :viw_del_cnt, :viw_del_prev_size,
                    :all_add_cnt, :all_add_size, :all_upd_cnt, :all_upd_size, :all_del_cnt, :all_del_prev_size)";

                using (OracleConnection connection = new OracleConnection(connectionString))
                {
                    connection.Open();
                    foreach (var dbAndUserItem in repoChanges)
                    {
                        foreach (var commitItem in dbAndUserItem.CommitsList)
                        {
                            using (OracleCommand cmd = new OracleCommand(query, connection))
                            {
                                cmd.Parameters.Clear();
                                cmd.Parameters.Add("process_id", OracleType.Number).Value = commitItem.ProcessId;
                                cmd.Parameters.Add("dbid", OracleType.VarChar).Value = dbAndUserItem.DBId;
                                cmd.Parameters.Add("username", OracleType.VarChar).Value = dbAndUserItem.UserName;
                                cmd.Parameters.Add("commit_common_date", OracleType.DateTime).Value = commitItem.CommitCommonDate;
                                cmd.Parameters.Add("is_initial", OracleType.Number).Value = commitItem.IsInitial;

                                AddCommitTypeOperOracleParams(cmd, "dbl", OracleObjectType.DBLINK, commitItem);
                                AddCommitTypeOperOracleParams(cmd, "dbj", OracleObjectType.DBMS_JOB, commitItem);
                                AddCommitTypeOperOracleParams(cmd, "fnc", OracleObjectType.FUNCTION, commitItem);
                                AddCommitTypeOperOracleParams(cmd, "pkg", OracleObjectType.PACKAGE, commitItem);
                                AddCommitTypeOperOracleParams(cmd, "prc", OracleObjectType.PROCEDURE, commitItem);
                                AddCommitTypeOperOracleParams(cmd, "scj", OracleObjectType.SCHEDULER_JOB, commitItem);
                                AddCommitTypeOperOracleParams(cmd, "seq", OracleObjectType.SEQUENCE, commitItem);
                                AddCommitTypeOperOracleParams(cmd, "syn", OracleObjectType.SYNONYM, commitItem);
                                AddCommitTypeOperOracleParams(cmd, "tab", OracleObjectType.TABLE, commitItem);
                                AddCommitTypeOperOracleParams(cmd, "trg", OracleObjectType.TRIGGER, commitItem);
                                AddCommitTypeOperOracleParams(cmd, "tps", OracleObjectType.TYPE, commitItem);
                                AddCommitTypeOperOracleParams(cmd, "viw", OracleObjectType.VIEW, commitItem);
                                AddCommitTypeOperOracleParams(cmd, "all", null, commitItem);

                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    
                }

                if (saveDetails)
                {

                    //в БД кладем инфо только о фактических коммитах
                    
                    if (progressData.RepoChangesPlainList != null)
                    {
                        var repoChangesPlainList = progressData.RepoChangesPlainList.Where(c=>!c.MaskWorked).ToList();

                        if (repoChangesPlainList.Any())
                        {
                            query =
                            $@"insert into {prefix}COMMITDETAILS (id, process_id, dbid, username, commit_common_date, is_initial, 
                    commit_cur_file_time,commit_oper,commit_file,commit_file_size,obj_type) values 
                    ({prefix}COMMITDETAILS_SEQ.Nextval, :process_id, :dbid, :username, :commit_common_date, :is_initial, 
                    :commit_cur_file_time,:commit_oper,:commit_file,:commit_file_size,:obj_type)";

                            using (OracleConnection connection = new OracleConnection(connectionString))
                            {
                                connection.Open();
                                foreach (var repoItem in repoChangesPlainList)
                                {
                                    using (OracleCommand cmd = new OracleCommand(query, connection))
                                    {
                                        cmd.Parameters.Clear();
                                        cmd.Parameters.Add("process_id", OracleType.Number).Value = repoItem.ProcessId;
                                        cmd.Parameters.Add("dbid", OracleType.VarChar).Value = repoItem.DBId;
                                        cmd.Parameters.Add("username", OracleType.VarChar).Value = repoItem.UserName;
                                        cmd.Parameters.Add("commit_common_date", OracleType.DateTime).Value =
                                            repoItem.CommitCommonDate;
                                        cmd.Parameters.Add("is_initial", OracleType.Number).Value = repoItem.IsInitial;
                                        cmd.Parameters.Add("commit_cur_file_time", OracleType.DateTime).Value =
                                            repoItem.CommitCurFileTime;
                                        cmd.Parameters.Add("commit_oper", OracleType.Number).Value = repoItem.Operation;
                                        cmd.Parameters.Add("commit_file", OracleType.VarChar).Value = repoItem.FileName;
                                        cmd.Parameters.Add("commit_file_size", OracleType.Number).Value =
                                            repoItem.FileSize;
                                        cmd.Parameters.Add("obj_type", OracleType.Number).Value = repoItem.ObjectType;
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }


        public static void SaveConnWorkLogInDB(string prefix, ExportProgressDataOuter progressData,
            string connectionString, DBLog dbLogSettings)
        {
            //try
            //{

            var addCols =
                ",MESSAGE,TYPEOBJCOUNTPLAN,CURRENTNUM,TYPEOBJCOUNTFACT,METAOBJCOUNTFACT,SCHEMAOBJCOUNTPLAN,OBJTYPE,OBJNAME";
            var addCols2 =
                ",:MESSAGE,:TYPEOBJCOUNTPLAN,:CURRENTNUM,:TYPEOBJCOUNTFACT,:METAOBJCOUNTFACT,:SCHEMAOBJCOUNTPLAN,:OBJTYPE,:OBJNAME";
            foreach (var colToExlude in dbLogSettings.ExludeCONNWORKLOGColumnsC)
            {
                addCols = addCols.Replace($",{colToExlude}", "");
                addCols2 = addCols2.Replace($",:{colToExlude}", "");
            }

            var query =
                $"insert into {prefix}CONNWORKLOG (id, process_id, dbid, username, dblink, stage, eventlevel, eventid, eventtime, schemaobjcountfact, errorscount{addCols}) values " +
                $"({prefix}CONNWORKLOG_seq.Nextval, :process_id, :dbid, :username, :dblink, :stage, :eventlevel, :eventid, :eventtime, :schemaobjcountfact, :errorscount{addCols2})";



            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                connection.Open();
                using (OracleCommand cmd = new OracleCommand(query, connection))
                {
                    cmd.Parameters.Add("process_id", OracleType.Number).Value = int.Parse(progressData.ProcessId);
                    cmd.Parameters.Add("dbid", OracleType.VarChar).Value =
                        progressData.CurrentConnection.DBIdC.ToUpper();
                    cmd.Parameters.Add("username", OracleType.VarChar).Value =
                        progressData.CurrentConnection.UserName.ToUpper();
                    cmd.Parameters.Add("dblink", OracleType.VarChar).Value =
                        string.IsNullOrWhiteSpace(progressData.DbLink) ? "<NONE>" : progressData.DbLink.ToUpper();
                    cmd.Parameters.Add("stage", OracleType.VarChar).Value = progressData.Stage.ToString();
                    cmd.Parameters.Add("eventlevel", OracleType.VarChar).Value = progressData.Level.ToString();
                    cmd.Parameters.Add("eventid", OracleType.VarChar).Value = progressData.EventId;
                    cmd.Parameters.Add("eventtime", OracleType.DateTime).Value = progressData.EventTime;
                    cmd.AddNullableParam("errorscount", OracleType.Number, progressData.ErrorsCount);
                    cmd.AddNullableParam("schemaobjcountfact", OracleType.Number, progressData.AllObjCountFact);

                    if (!dbLogSettings.ExludeCONNWORKLOGColumnsC.Contains("MESSAGE"))
                        cmd.Parameters.Add("MESSAGE", OracleType.VarChar).Value = progressData.Message;
                    if (!dbLogSettings.ExludeCONNWORKLOGColumnsC.Contains("TYPEOBJCOUNTPLAN"))
                        cmd.AddNullableParam("TYPEOBJCOUNTPLAN", OracleType.Number, progressData.TypeObjCountPlan);
                    if (!dbLogSettings.ExludeCONNWORKLOGColumnsC.Contains("CURRENTNUM"))
                        cmd.AddNullableParam("CURRENTNUM", OracleType.Number, progressData.Current);
                    if (!dbLogSettings.ExludeCONNWORKLOGColumnsC.Contains("TYPEOBJCOUNTFACT"))
                        cmd.AddNullableParam("TYPEOBJCOUNTFACT", OracleType.Number, progressData.TypeObjCountFact);
                    if (!dbLogSettings.ExludeCONNWORKLOGColumnsC.Contains("METAOBJCOUNTFACT"))
                        cmd.AddNullableParam("METAOBJCOUNTFACT", OracleType.Number, progressData.MetaObjCountFact);
                    if (!dbLogSettings.ExludeCONNWORKLOGColumnsC.Contains("SCHEMAOBJCOUNTPLAN"))
                        cmd.AddNullableParam("SCHEMAOBJCOUNTPLAN", OracleType.Number, progressData.AllObjCountPlan);
                    if (!dbLogSettings.ExludeCONNWORKLOGColumnsC.Contains("OBJTYPE"))
                        cmd.AddNullableParam("OBJTYPE", OracleType.VarChar, progressData.ObjectType);
                    if (!dbLogSettings.ExludeCONNWORKLOGColumnsC.Contains("OBJNAME"))
                        cmd.AddNullableParam("OBJNAME", OracleType.VarChar, progressData.ObjectName);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
