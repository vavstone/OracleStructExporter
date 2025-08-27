using OracleStructExporter.Core.DBStructs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OracleClient;
using System.IO;
using System.Linq;
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

        List<TableOrViewComment> tablesComments = new List<TableOrViewComment>();
        List<TableOrViewComment> viewsComments = new List<TableOrViewComment>();
        List<ColumnComment> tablesAndViewsColumnsComments = new List<ColumnComment>();
        List<TableStruct> tablesStructs = new List<TableStruct>();
        List<TableColumnStruct> tablesAndViewsColumnStruct = new List<TableColumnStruct>();
        List<IndexStruct> tablesIndexes = new List<IndexStruct>();
        List<ConstraintStruct> tablesConstraints = new List<ConstraintStruct>();
        List<PartTables> partTables = new List<PartTables>();

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

        static string AddCreateOrReplace(string source)
        {
            source = source.Trim();
            if (!string.IsNullOrWhiteSpace(source) &&
                !source.StartsWith("CREATE OR REPLACE", StringComparison.OrdinalIgnoreCase))
            {
                return $"CREATE OR REPLACE {source}";
            }

            return source;
        }

        static string GetObjectDdlForSynonym(List<SynonymAttributes> synonymAttributesList, string objectName, List<string> addSlashTo)
        {
            var attributes = synonymAttributesList.First(c => c.Name.ToUpper() == objectName.ToUpper());
            var objectOwnerAddStr = "";
            if (!string.IsNullOrWhiteSpace(attributes.TargetSchema))
                objectOwnerAddStr += attributes.TargetSchema + ".";
            var dblinkAddStr = "";
            if (!string.IsNullOrWhiteSpace(attributes.DBLink)) dblinkAddStr += "@" + attributes.DBLink;
            var res = $@"create or replace synonym {attributes.Name}
  for {objectOwnerAddStr}{attributes.TargetObjectName}{dblinkAddStr};";
            if (addSlashTo.Contains("SYNONYMS"))
                res += Environment.NewLine + "/";
            return res;
        }

        static string GetObjectDdlForSequence(List<SequenceAttributes> sequenceAttributesList, string objectName, bool resetStartValueTo1, List<string> addSlashTo)
        {
            var attributes = sequenceAttributesList.FirstOrDefault(c => c.SequenceName.ToUpper() == objectName.ToUpper());

            var sb = new StringBuilder($"create sequence {objectName.ToUpper()}");
            if (attributes.MinValue != null)
            {
                sb.AppendLine();
                sb.Append($"minvalue {attributes.MinValue.Value}");
            }

            if (attributes.MaxValue != null)
            {
                sb.AppendLine();
                sb.Append($"maxvalue {attributes.MaxValue.Value}");
            }

            var startVal = resetStartValueTo1 ? 1 : attributes.LastNumber;
            sb.AppendLine();
            sb.Append($"start with {startVal}");
            sb.AppendLine();
            sb.Append($"increment by {attributes.IncrementBy}");
            sb.AppendLine();
            if (attributes.CacheSize > 0)
                sb.Append($"cache {attributes.CacheSize}");
            else
                sb.Append("nocache");
            if (attributes.CycleFlag == "Y")
            {
                sb.AppendLine();
                sb.Append("cycle");
            }

            if (attributes.OrderFlag == "Y")
            {
                sb.AppendLine();
                sb.Append("order");
            }

            sb.Append(";");

            if (addSlashTo.Contains("SEQUENCES"))
            {
                sb.AppendLine();
                sb.Append("/");
            }
            return sb.ToString();
        }

        static void AddJobArgsString(StringBuilder sb, string curArg, Dictionary<string,string> jobCreateArgs, int prefixLength)
        { 
            var longestArglength = jobCreateArgs.Keys.Select(c => c.Length).Max();
            if (jobCreateArgs.ContainsKey(curArg))
            {
                sb.AppendLine(",");
                sb.AppendPadded("", prefixLength);
                sb.Append(curArg);
                sb.AppendPadded(" ", longestArglength - curArg.Length + 1);
                sb.Append($"=> {jobCreateArgs[curArg]}");
            }
        }

        static string GetObjectDdlForSchedulerJob(List<SchedulerJob> shedulerJobsList, string objectName, List<string> addSlashTo)
        {
            var job = shedulerJobsList.FirstOrDefault(c => c.JobName.ToUpper() == objectName.ToUpper());
            if (job == null)
                return string.Empty;

            var sb = new StringBuilder("begin");
            sb.AppendLine();

            Dictionary<string,string> jobCreateArgs = new Dictionary<string,string>();
            jobCreateArgs["job_name"] = $"'{job.JobName}'";
            if (!string.IsNullOrWhiteSpace(job.JobType))
                jobCreateArgs["job_type"] = $"'{job.JobType}'";
            if (!string.IsNullOrWhiteSpace(job.JobAction))
                jobCreateArgs["job_action"] = $"'{job.JobAction}'";
            if (job.NumberOfArguments!=null && job.NumberOfArguments>0)
                jobCreateArgs["number_of_arguments"] = job.NumberOfArguments.Value.ToString();
            if (job.StartDate != null)
            {
                var strDate = job.StartDate.Value.ToString("dd-MM-yyyy HH:mm:ss");
                jobCreateArgs["start_date"] = $"to_date('{strDate}', 'dd-mm-yyyy hh24:mi:ss')";
            }
            else
                jobCreateArgs["start_date"] = "to_date(null)";

            //if (!string.IsNullOrWhiteSpace(job.RepeatInterval)) 
                jobCreateArgs["repeat_interval"] = $"'{job.RepeatInterval}'";
            if (job.EndDate != null)
            {
                var strDate = job.EndDate.Value.ToString("dd-MM-yyyy HH:mm:ss");
                jobCreateArgs["end_date"] = $"to_date('{strDate}', 'dd-mm-yyyy hh24:mi:ss')";
            }
            else
                jobCreateArgs["end_date"] = "to_date(null)";
            if (!string.IsNullOrWhiteSpace(job.JobClass))
                jobCreateArgs["job_class"] = $"'{job.JobClass}'";
            if (job.ArgumentList.Any())
                jobCreateArgs["enabled"] = "false";
            else
            {
                if (!string.IsNullOrWhiteSpace(job.Enabled))
                    jobCreateArgs["enabled"] = job.Enabled.ToLower();
            }

            if (!string.IsNullOrWhiteSpace(job.AutoDrop))
                jobCreateArgs["auto_drop"] = job.AutoDrop.ToLower();
            jobCreateArgs["comments"] = $"'{job.Comments}'";


            var longestArglength = jobCreateArgs.Keys.Select(c => c.Length).Max();

            var startBlockStr = "  dbms_scheduler.create_job(";
            sb.Append(startBlockStr);
            var curArg = "job_name";
            sb.Append(curArg);
            sb.AppendPadded(" ", longestArglength - curArg.Length +1);
            sb.Append($"=> {jobCreateArgs[curArg]}");

            AddJobArgsString(sb, "job_type", jobCreateArgs, startBlockStr.Length);
            AddJobArgsString(sb, "job_action", jobCreateArgs, startBlockStr.Length);
            AddJobArgsString(sb, "number_of_arguments", jobCreateArgs, startBlockStr.Length);
            AddJobArgsString(sb, "start_date", jobCreateArgs, startBlockStr.Length);
            AddJobArgsString(sb, "repeat_interval", jobCreateArgs, startBlockStr.Length);
            AddJobArgsString(sb, "end_date", jobCreateArgs, startBlockStr.Length);
            AddJobArgsString(sb, "job_class", jobCreateArgs, startBlockStr.Length);
            AddJobArgsString(sb, "enabled", jobCreateArgs, startBlockStr.Length);
            AddJobArgsString(sb, "auto_drop", jobCreateArgs, startBlockStr.Length);
            AddJobArgsString(sb, "comments", jobCreateArgs, startBlockStr.Length);

            sb.AppendLine(");");

            foreach (var arg in job.ArgumentList)
            {
                jobCreateArgs.Clear();
                jobCreateArgs["job_name"] = $"'{arg.JobName}'";
                if (!string.IsNullOrWhiteSpace(arg.ArgumentName))
                    jobCreateArgs["argument_name"] = $"'{arg.ArgumentName}'";
                jobCreateArgs["argument_position"] = arg.ArgumentPosition.ToString();
                jobCreateArgs["argument_value"] = $"'{arg.Value}'";

                longestArglength = jobCreateArgs.Keys.Select(c => c.Length).Max();
                startBlockStr = "  dbms_scheduler.set_job_argument_value(";
                sb.Append(startBlockStr);
                curArg = "job_name";
                sb.Append(curArg);
                sb.AppendPadded(" ", longestArglength - curArg.Length + 1);
                sb.Append($"=> {jobCreateArgs[curArg]}");

                AddJobArgsString(sb, "argument_name", jobCreateArgs, startBlockStr.Length);
                AddJobArgsString(sb, "argument_position", jobCreateArgs, startBlockStr.Length);
                AddJobArgsString(sb, "argument_value", jobCreateArgs, startBlockStr.Length);
                sb.AppendLine(");");
            }

            if (job.Enabled == "TRUE" && job.ArgumentList.Any())
                sb.AppendLine($"  dbms_scheduler.enable(name => '{job.JobName}');");

            sb.Append("end;");

            if (addSlashTo.Contains("JOBS"))
            {
                sb.AppendLine();
                sb.Append("/");
            }
            return sb.ToString();
        }

        static string GetObjectDdlForDBMSJob(List<DBMSJob> dbmsJobs, string objectName, List<string> addSlashTo)
        {
            var job = dbmsJobs.FirstOrDefault(c => c.Job.ToString() == objectName);
            if (job == null)
                return string.Empty;

            var sb = new StringBuilder("declare");
            sb.AppendLine("  l_job NUMBER;");
            sb.AppendLine("begin");
            sb.AppendLine("  select max (job) + 1 into l_job from user_jobs;");

            Dictionary<string, string> jobCreateArgs = new Dictionary<string, string>();
            jobCreateArgs["job"] = "l_job";
            if (!string.IsNullOrWhiteSpace(job.What))
                jobCreateArgs["what"] = $"'{job.What.Replace("'","''")}'";
            var strDate = job.NextTime.ToString("dd-MM-yyyy HH:mm:ss");
            jobCreateArgs["next_date"] = $"to_date('{strDate}', 'dd-mm-yyyy hh24:mi:ss')";
            if (job.Interval!=null)
                jobCreateArgs["interval"] = $"'{job.Interval.Replace("'","''")}'";
            else
                jobCreateArgs["interval"] = "'NULL'";

            var longestArglength = jobCreateArgs.Keys.Select(c => c.Length).Max();

            var startBlockStr = "  dbms_job.submit(";
            sb.Append(startBlockStr);
            var curArg = "job";
            sb.Append(curArg);
            sb.AppendPadded(" ", longestArglength - curArg.Length + 1);
            sb.Append($"=> {jobCreateArgs[curArg]}");

            AddJobArgsString(sb, "what", jobCreateArgs, startBlockStr.Length);
            AddJobArgsString(sb, "next_date", jobCreateArgs, startBlockStr.Length);
            AddJobArgsString(sb, "interval", jobCreateArgs, startBlockStr.Length);

            sb.AppendLine(");");
            sb.AppendLine("  commit;");
            sb.Append("end;");

            if (addSlashTo.Contains("JOBS"))
            {
                sb.AppendLine();
                sb.Append("/");
            }
            return sb.ToString();
        }

        static string GetObjectDdlForView(Dictionary<string,string> views, List<TableColumnStruct> columnsStructs, List<TableOrViewComment> viewComments,
            List<ColumnComment> columnsComments, string objectName, List<string> addSlashTo)
        {
            var objectNameUpper = objectName.ToUpper();
            var viewText = views[objectName];

            var curViewColumnsStruct =
                columnsStructs.Where(c => c.TableName.ToUpper() == objectNameUpper).ToList();
            var colsToShow = curViewColumnsStruct.Where(c => c.HiddenColumn != "YES").OrderBy(c => c.ColumnId)
                .ToList();

            var sb = new StringBuilder($"create or replace force view {objectName.ToLower()} as");
            sb.AppendLine();
            sb.Append(viewText);
            sb.Append(";");
            var curViewComment = viewComments.FirstOrDefault(c => c.TableName.ToUpper() == objectNameUpper);
            var curViewColumnsComments = columnsComments.Where(c => c.TableName.ToUpper() == objectNameUpper).ToList();
            var commentsString =
                GetCommentsString(curViewComment, curViewColumnsComments, colsToShow, objectNameUpper, false);
            if (!string.IsNullOrWhiteSpace(commentsString))
            {
                sb.Append(commentsString);
            }

            if (addSlashTo.Contains("VIEWS"))
            {
                sb.AppendLine();
                sb.Append("/");
            }
            return sb.ToString();
        }

        static string getMergedColumns(List<string> cols)
        {
            var columns_str = "";
            foreach (var col in cols)
                columns_str += col + ", ";
            if (!string.IsNullOrWhiteSpace(columns_str))
                columns_str = "(" + columns_str.Substring(0, columns_str.Length - 2) + ")";
            return columns_str;
        }


        static string GetContstraintText(ConstraintStruct constraint, List<ConstraintStruct> constraints,
            string schemaName)
        {
            StringBuilder sb = new StringBuilder();
            var columns_str = getMergedColumns(constraint.ConstraintColumnStructs.OrderBy(c => c.Position)
                .Select(c => c.ColumnName).ToList());
            var addConstrNameStr = constraint.Generated == "USER NAME"
                ? $"constraint {constraint.ConstraintName.ToUpper()} "
                : "";

            var typeKey = "";
            switch (constraint.ConstraintType.ToUpper())
            {
                case "P":
                    typeKey = "primary key";
                    break;
                case "R":
                    typeKey = "foreign key";
                    break;
                case "U":
                    typeKey = "unique";
                    break;
            }

            sb.Append($"{addConstrNameStr}{typeKey} {columns_str}");
            if (constraint.ConstraintType.ToUpper() == "R")
            {
                var addSchemaInfo = constraint.ROwner.ToUpper() != schemaName
                    ? constraint.ROwner.ToUpper() + "."
                    : "";
                var outerConstraint = constraint.ReferenceConstraint;
                if (outerConstraint == null)
                {
                    throw new Exception($"Не загружена информация о связанном ключе {constraint.RConstraintName}!");
                }

                sb.AppendLine("");
                sb.Append(
                    $"  references {addSchemaInfo}{outerConstraint.TableName} {getMergedColumns(outerConstraint.ConstraintColumnStructs.OrderBy(c => c.Position).Select(c => c.ColumnName).ToList())}");
            }

            if (!string.IsNullOrWhiteSpace(constraint.DeleteRule) && constraint.DeleteRule != "NO ACTION")
            {
                sb.Append($" on delete {constraint.DeleteRule.ToLower()}");
            }

            if (constraint.Status == "DISABLED")
            {
                sb.AppendLine();
                sb.Append("  disable");
            }

            if (constraint.Validated == "NOT VALIDATED")
            {
                sb.AppendLine();
                sb.Append("  novalidate");
            }

            return sb.ToString();
        }

        static void BindConstraintsAndIndexes(List<ConstraintStruct> constraints, List<IndexStruct> indexes)
        {
            foreach (var constraint in constraints)
            {
                constraint.BindedIndexStruct = indexes.FirstOrDefault(c =>
                    c.TableName == constraint.TableName && c.IndexName == constraint.ConstraintName);
            }

            foreach (var index in indexes)
            {
                index.BindedConstraintStruct = constraints.FirstOrDefault(c =>
                    c.TableName == index.TableName && c.ConstraintName == index.IndexName);
            }
        }

        static string GetObjectDdlForTable(
            List<TableStruct> tablesStructs,
            List<TableColumnStruct> tablesColumnsStructs,
            List<ConstraintStruct> tablesConstraints,
            List<IndexStruct> tablesIndexes,
            List<TableOrViewComment> tableComments,
            List<ColumnComment> columnsComments,
            List<PartTables> partTables,
            string objectName,
            string schemaName,
            List<string> addSlashTo)
        {
            var objectNameUpper = objectName.ToUpper();
            var curTableStruct = tablesStructs.FirstOrDefault(c => c.TableName.ToUpper() == objectNameUpper);
            var curTableColumnsStruct =
                tablesColumnsStructs.Where(c => c.TableName.ToUpper() == objectNameUpper).ToList();
            var curTableComment = tableComments.FirstOrDefault(c => c.TableName.ToUpper() == objectNameUpper);
            var curTableColumnsComments = columnsComments.Where(c => c.TableName.ToUpper() == objectNameUpper).ToList();
            var curTableConstraints = tablesConstraints.Where(c => c.TableName.ToUpper() == objectNameUpper).ToList();
            var curTableIndexes = tablesIndexes.Where(c => c.TableName.ToUpper() == objectNameUpper).ToList();
            var curPartTable = partTables.FirstOrDefault(c => c.TableName.ToUpper() == objectNameUpper);

            BindConstraintsAndIndexes(curTableConstraints, curTableIndexes);

            var sb = new StringBuilder();
            var sbString = new StringBuilder();
            bool isGlobalTemporary =
                curTableStruct != null && curTableStruct.Temporary.ToUpper() == "Y" ? true : false;
            var actionForTempTable = "";
            if (isGlobalTemporary)
            {
                if (curTableStruct != null && curTableStruct.Duration.ToUpper() == "SYS$TRANSACTION")
                    actionForTempTable = "delete";
                else
                    actionForTempTable = "preserve";
            }

            var addToCreate = isGlobalTemporary ? "global temporary " : "";
            var addToEnd = "";

            if (isGlobalTemporary)
            {
                addToEnd += $"on commit {actionForTempTable} rows";
            }
            else
            {
                if (curTableStruct != null && curTableStruct.Compression == "ENABLED")
                    addToEnd += "compress";
                if (curTableStruct != null && curTableStruct.Dependencies == "ENABLED")
                {
                    if (!string.IsNullOrWhiteSpace(addToEnd)) addToEnd += " ";
                    addToEnd += "rowdependencies";
                }
                if (curTableStruct != null && curTableStruct.IOTType == "IOT")
                {
                    if (!string.IsNullOrWhiteSpace(addToEnd)) addToEnd += " ";
                    addToEnd += "organization index";
                }
                if (curTableStruct != null && curTableStruct.Logging == "NO")
                {
                    if (!string.IsNullOrWhiteSpace(addToEnd)) addToEnd += " ";
                    addToEnd += "nologging";
                }
            }

            sb.AppendLine($"create {addToCreate}table {objectNameUpper}");
            sb.Append("(");
            var maxColNameLength = curTableColumnsStruct.Where(c => c.HiddenColumn != "YES").Max(c => c.ColumnNameToShow.Length);
            var colsToShow = curTableColumnsStruct.Where(c => c.HiddenColumn != "YES").OrderBy(c => c.ColumnId)
                .ToList();
            for (int i = 0; i < colsToShow.Count; i++)
            {
                var col = colsToShow[i];
                sb.AppendLine();
                sbString.Append("  ");
                sbString.Append(col.ColumnNameToShow);
                var curColLength = col.ColumnNameToShow.Length;
                var lengthDiff = maxColNameLength - curColLength;

                var identityStr = string.Empty;
                if (col.IdentityColumnStruct != null &&
                    !string.IsNullOrWhiteSpace(col.IdentityColumnStruct.IdentityOptions))
                {
                    var dict = col.IdentityColumnStruct.IdentityOptions.SplitToDictionary(",", ":", true);
                    var sbIdent = new StringBuilder(" generated");
                    if (!string.IsNullOrWhiteSpace(col.IdentityColumnStruct.GenerationType))
                        sbIdent.Append($" {col.IdentityColumnStruct.GenerationType.ToLower()}");
                    sbIdent.Append(" as identity");
                    var start_with_option = "";
                    if (dict.ContainsKey("START WITH") && dict["START WITH"]!="1")
                        start_with_option = $"start with {dict["START WITH"]}";
                    var order_option = "";
                    if (dict.ContainsKey("ORDER_FLAG") && dict["ORDER_FLAG"] == "Y")
                        order_option = "order";
                    var nocache_option = "";
                    if (dict.ContainsKey("CACHE_SIZE") && dict["CACHE_SIZE"] == "0")
                        nocache_option = "nocache";
                    if (!string.IsNullOrWhiteSpace(start_with_option) || !string.IsNullOrWhiteSpace(order_option) || !string.IsNullOrWhiteSpace(nocache_option))
                    {
                        sbIdent.Append(" (");
                        if (!string.IsNullOrWhiteSpace(start_with_option))
                            sbIdent.Append(start_with_option);

                        if (!string.IsNullOrWhiteSpace(order_option))
                        {
                            if (!string.IsNullOrWhiteSpace(start_with_option))
                                sbIdent.Append(" ");
                            sbIdent.Append(order_option);
                        }

                        if (!string.IsNullOrWhiteSpace(nocache_option))
                        {
                            if (!string.IsNullOrWhiteSpace(start_with_option) || !string.IsNullOrWhiteSpace(order_option))
                                sbIdent.Append(" ");
                            sbIdent.Append(nocache_option);
                        }

                        sbIdent.Append(")");
                    }
                    identityStr = sbIdent.ToString();
                }

                sbString.AppendPadded(" ", lengthDiff + 1);
                var colDataTypeToShow = col.DataType.ToUpper();
                if (colDataTypeToShow == "NUMBER" && col.DataScale != null && col.DataScale == 0 &&
                    (col.DataPrecision == null || col.DataPrecision == 0))
                    colDataTypeToShow = "INTEGER";

                var dataTypeOwnerAddStr = string.IsNullOrWhiteSpace(col.DataTypeOwner)
                    ? ""
                    : col.DataTypeOwner.ToUpper() + ".";

                sbString.Append(dataTypeOwnerAddStr + colDataTypeToShow);
                if (col.DataType.ToUpper() == "VARCHAR2" || col.DataType.ToUpper() == "NVARCHAR2" ||
                    col.DataType.ToUpper() == "CHAR" || col.DataType.ToUpper() == "RAW")
                {
                    var charDataLength = col.CharLength;
                    if (charDataLength == null || charDataLength == 0)
                        charDataLength = col.DataLength;
                    var charUsedAddStr = "";
                    if (col.CharUsed == "C" && (col.DataType.ToUpper() == "VARCHAR2" || col.DataType.ToUpper() == "CHAR"))
                        charUsedAddStr = " CHAR";
                    sbString.Append($"({charDataLength}{charUsedAddStr})");
                }

                if ((col.DataType.ToUpper() == "NUMBER" && col.DataPrecision != null) || 
                    (col.DataType.ToUpper() == "FLOAT" && col.DataPrecision != null && col.DataPrecision<126)) //почему-то с 126 не нужно выводить точность
                {
                    var dataScaleAdd = col.DataScale != null && col.DataScale > 0 ? $",{col.DataScale}" : "";
                    sbString.Append($"({col.DataPrecision}{dataScaleAdd})");
                }

                if (!string.IsNullOrWhiteSpace(identityStr))
                    sbString.Append(identityStr);
                else
                {
                    if (!string.IsNullOrWhiteSpace(col.DataDefault) && col.DataDefault.ToUpper() != "NULL")
                    {
                        sbString.Append(" default");
                        if (col.DefaultOnNull == "YES")
                            sbString.Append(" on null");
                        sbString.Append(" " + col.DataDefault);
                    }

                    if (col.Nullable.ToUpper() == "N")
                        sbString.Append(" not null");
                }

                if (i < colsToShow.Count - 1)
                    sbString.Append(",");
                sb.Append(sbString);
                sbString.Clear();
            }

            foreach (var constraint in curTableConstraints.Where(c =>
                             (c.ConstraintType.ToUpper() == "P" || c.ConstraintType.ToUpper() == "U") &&
                             c.ConstraintColumnStructs.Any() &&
                             (c.BindedIndexStruct != null && c.BindedIndexStruct.IndexType == "IOT - TOP"))
                         .OrderBy(c => c.ConstraintType).ThenBy(c => c.ConstraintName, new OracleLikeStringComparer()))
            {
                sb.Append(",");
                sb.AppendLine("");
                sb.Append("  " + GetContstraintText(constraint, tablesConstraints, schemaName));
            }

            sb.AppendLine();

            sb.AppendLine(")");
            sb.Append($"{addToEnd}");

            //партиции
            if (curPartTable != null)
            {
                if (curPartTable.PartKeyColumns.Any())
                {
                    var mergedCols = getMergedColumns(curPartTable.PartKeyColumns.OrderBy(c => c.ColumnPosition)
                        .Select(c => c.ColumnName).ToList());
                    sb.Append($"partition by {curPartTable.PartitioningType.ToLower()} {mergedCols}");
                    if (curPartTable.SubPartKeyColumns.Any())
                    {
                        sb.AppendLine();
                        mergedCols = getMergedColumns(curPartTable.SubPartKeyColumns.OrderBy(c => c.ColumnPosition)
                            .Select(c => c.ColumnName).ToList());
                        sb.Append($"subpartition by {curPartTable.SubPartitioningType.ToLower()} {mergedCols}");
                    }

                    //наполнение
                    if (curPartTable.Partitions.Any())
                    {
                        sb.AppendLine();
                        sb.Append("(");
                        for (int i = 0; i < curPartTable.Partitions.Count; i++)
                        {
                            var partition = curPartTable.Partitions[i];
                            sb.AppendLine();
                            sb.AppendLine($"  partition {partition.PartitionName} values ({partition.HighValue})");
                            sb.Append($"    tablespace {partition.TableSpaceName}");
                            if (partition.SubPartitions.Any())
                            {
                                sb.AppendLine();
                                sb.Append("  (");
                                for (int j = 0; j < partition.SubPartitions.Count; j++)
                                {
                                    var subPartition = partition.SubPartitions[j];
                                    sb.AppendLine();
                                    var highValueAddStr = string.IsNullOrWhiteSpace(subPartition.HighValue)
                                        ? ""
                                        : $" values ({subPartition.HighValue})";
                                    sb.Append(
                                        $"    subpartition {subPartition.SubPartitionName}{highValueAddStr} tablespace {subPartition.TableSpaceName}");
                                    if (j < partition.SubPartitions.Count - 1)
                                        sb.Append(",");
                                }
                                sb.AppendLine();
                                sb.Append("  )");
                            }

                            if (i < curPartTable.Partitions.Count - 1)
                                sb.Append(",");
                        }
                        sb.AppendLine();
                        sb.Append(")");
                    }
                }
            }

            sb.Append(";");

            var commentsString =
                GetCommentsString(curTableComment, curTableColumnsComments, colsToShow, objectNameUpper, true);
            if (!string.IsNullOrWhiteSpace(commentsString))
            {
                sb.Append(commentsString);
            }

            //не выводим индексы, совпадающие именами с ключами
            var idxForIterate = curTableIndexes.Where(c => c.BindedConstraintStruct == null &&
                    c.IndexColumnStructs.Any())
                .OrderBy(c => c.IndexName, new OracleLikeStringComparer()).ToList();

            foreach (var index in idxForIterate)
            {
                var columns_str = "";

                foreach (var idxColumnStruct in index.IndexColumnStructs.OrderBy(c => c.ColumnPosition))
                {

                    {
                        var colOrExprStr = string.IsNullOrWhiteSpace(idxColumnStruct.Expression)
                            ? idxColumnStruct.ColumnName.ToUpper()
                            : idxColumnStruct.Expression;
                        columns_str += colOrExprStr +
                                       (idxColumnStruct.Descend.ToUpper() == "ASC"
                                           ? ""
                                           : $" {idxColumnStruct.Descend.ToUpper()}") + ", ";
                    }
                }

                if (!string.IsNullOrWhiteSpace(columns_str))
                {
                    columns_str = "(" + columns_str.Substring(0, columns_str.Length - 2) + ")";
                }

                var uniqueAddStr = index.Uniqueness == "UNIQUE" ? "unique " : "";

                sb.AppendLine("");
                var idxAddStr = "";
                switch (index.IndexType.ToUpper())
                {
                    case "NORMAL":
                    case "FUNCTION-BASED NORMAL":
                        idxAddStr = "";
                        break;
                    case "FUNCTION-BASED BITMAP":
                        idxAddStr = "bitmap ";
                        break;
                    default:
                        idxAddStr = index.IndexType.ToLower() + " ";
                        break;
                }

                sb.Append(
                    $"create {uniqueAddStr}{idxAddStr}index {index.IndexName.ToUpper()} on {objectNameUpper} {columns_str}");
                if (index.Compression == "ENABLED" || index.Logging!= "YES" || index.Locality=="LOCAL")
                {
                    sb.AppendLine();
                    sb.Append("  ");
                    if (index.Compression == "ENABLED")
                    {
                        sb.Append("compress");
                        if (index.PrefixLength != null &&
                            index.PrefixLength == 1) //почему-то только это значение выводим в ddl
                            sb.Append(" " + index.PrefixLength);
                    }

                    if (index.Logging != "YES")
                    {
                        if (index.Compression == "ENABLED")
                            sb.Append(" ");
                        sb.Append("nologging");
                    }

                    if (index.Locality == "LOCAL")
                    {
                        if (index.Compression == "ENABLED" || index.Logging != "YES")
                        {
                            sb.Append(" ");
                            //для совместимости с PLSQL Developer
                            sb.Append(" ");
                        }
                        sb.Append("local");
                    }
                }

                sb.Append(";");
            }

            foreach (var constraint in curTableConstraints.Where(c =>
                             (c.ConstraintType.ToUpper() == "P" || c.ConstraintType.ToUpper() == "R" ||
                              c.ConstraintType.ToUpper() == "U") &&
                             c.ConstraintColumnStructs.Any() &&
                             (c.BindedIndexStruct == null || c.BindedIndexStruct.IndexType != "IOT - TOP"))
                         .OrderBy(c => c.ConstraintType).ThenBy(c => c.ConstraintName, new OracleLikeStringComparer()))
            {
                sb.AppendLine("");
                sb.AppendLine($"alter table {objectNameUpper}");
                sb.Append("  add ");
                sb.Append(GetContstraintText(constraint, tablesConstraints, schemaName));
                sb.Append(";");
            }

            if (addSlashTo.Contains("TABLES"))
            {
                sb.AppendLine();
                sb.Append("/");
            }

            return sb.ToString();
        }

        static string GetCommentsString(TableOrViewComment tableOrViewComment, List<ColumnComment> columnsComments, List<TableColumnStruct> columnsStructs, string objectName, bool isTable)
        {
            var sb = new StringBuilder();
            if (tableOrViewComment != null && !string.IsNullOrWhiteSpace(tableOrViewComment.Comments))
            {
                sb.AppendLine("");
                sb.Append($"comment on table {objectName}");
                if (isTable)
                {
                    sb.AppendLine();
                    sb.Append(" ");
                }

                sb.Append($" is '{tableOrViewComment.Comments.EscapeNotValidInSqlSymbols()}';");
            }

            foreach (var curCol in columnsStructs.OrderBy(c => c.ColumnId))
            {
                var comment = columnsComments.FirstOrDefault(c =>
                    c.ColumnName == curCol.ColumnName && !string.IsNullOrWhiteSpace(c.Comments));
                if (comment != null)
                {
                    sb.AppendLine("");
                    var colName = isTable ? comment.ColumnName.ToLower() : comment.ColumnName.ToUpper();
                    sb.Append($"comment on column {objectName}.{colName}");
                    if (isTable)
                    {
                        sb.AppendLine();
                        sb.Append(" ");
                    }
                    sb.Append($" is '{comment.Comments.EscapeNotValidInSqlSymbols()}';");
                }
            }
            return sb.ToString();
        }


        static string GetObjectDdlForPackageHeader(Dictionary<string, string> packagesHeaders, string objectName, List<string> addSlashTo)
        {
            var ddl = new StringBuilder();
            var headerText = packagesHeaders.ContainsKey(objectName) ? packagesHeaders[objectName] : string.Empty;
            var header = AddCreateOrReplace(headerText);
            if (!string.IsNullOrWhiteSpace(header))
            {
                ddl.Append(header);
                // добавляем разделитель
                if (addSlashTo.Contains("PACKAGES"))
                {
                    ddl.AppendLine();
                    ddl.Append("/");
                }
            }
            return ddl.ToString();
        }

        static string GetObjectDdlForPackageBody(Dictionary<string, string> packagesBodies, string objectName, List<string> addSlashTo)
        {
            var ddl = new StringBuilder();
            var bodyText = packagesBodies.ContainsKey(objectName) ? packagesBodies[objectName] : string.Empty;
            var body = AddCreateOrReplace(bodyText);
            if (!string.IsNullOrWhiteSpace(body))
            {
                ddl.Append(body);
                // добавляем разделитель
                if (addSlashTo.Contains("PACKAGES"))
                {
                    ddl.AppendLine();
                    ddl.Append("/");
                }
            }
            return ddl.ToString();
        }

        static string MergeHeadAndBody(string header, string body)
        {
            var ddl = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(header))
                ddl.Append(header);
            if (!string.IsNullOrWhiteSpace(header) && !string.IsNullOrWhiteSpace(body))
            {
                ddl.AppendLine();
                ddl.AppendLine();
            }
            if (!string.IsNullOrWhiteSpace(body))

                ddl.Append(body);
            return ddl.ToString();
        }

        static string GetObjectDdlForSourceText(Dictionary<string, string> funcText, string objectName, string objectType, List<string> addSlashTo)
        {

            var ddl = new StringBuilder();
            var inText = funcText[objectName];
            ddl.Append(AddCreateOrReplace(inText));
            var addsplitter = addSlashTo.Contains(objectType);
            if (addsplitter)
            {
                ddl.AppendLine();
                ddl.Append("/");
            }
            return ddl.ToString();
        }

        static string GetObjectGrants(List<GrantAttributes> grantsAttributes, string objectName,
            List<string> orderGrantOptions)
        {
            StringBuilder grants = new StringBuilder();
            var attr = grantsAttributes.Where(c => c.ObjectName.ToUpper() == objectName.ToUpper()).ToList();
            foreach (var granteeInfo in attr.GroupBy(c => c.Grantee)
                         .OrderBy(c => c.Key, new OracleLikeStringComparer()))
            {
                foreach (var grantableInfo in granteeInfo.GroupBy(c => c.Grantable).OrderBy(c => c.Key))
                {
                    var privList = new List<string>();
                    foreach (var orderItem in orderGrantOptions)
                    {
                        if (grantableInfo.Any(c => c.Privilege.ToUpper() == orderItem))
                            privList.Add(orderItem);
                    }

                    foreach (var notInSortListItem in grantableInfo.Where(c =>
                                 !orderGrantOptions.Contains(c.Privilege.ToUpper())))
                    {
                        privList.Add(notInSortListItem.Privilege);
                    }

                    var addStr = "";
                    if (grantableInfo.Key.ToUpper() == "YES")
                        addStr = " with grant option";
                    grants.AppendLine();
                    grants.Append(
                        $"grant {string.Join(", ", privList.ToArray()).ToLower()} on {objectName.ToUpper()} to {granteeInfo.Key.ToUpper()}{addStr};");
                }
            }

            return grants.ToString();
        }

        static string GetDDlWithGrants(string ddl, string grants)
        {
            var res = ddl;
            if (!string.IsNullOrWhiteSpace(grants))
                res += grants;
            return res + Environment.NewLine;
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

        public async void StartWork(ThreadInfo threadInfo)
        {
            // Если задача уже выполняется
            if (_cancellationTokenSource != null)
                throw new Exception("Задача уже выполняется");
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                var ct = _cancellationTokenSource.Token;
                await Task.Run(() => StartWork(threadInfo, ct), ct);
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
            var currentTime = DateTime.Now;

            var progressManager = new ProgressDataManager(_progressReporter, threadInfo.ProcessId, threadInfo.Connection);
            var exportSettingsDetails = threadInfo.ExportSettings.ExportSettingsDetails;
            var settingsConnection = threadInfo.Connection;
            var objectNameMask = exportSettingsDetails.MaskForFileNames?.Include;
            var outputFolder = threadInfo.ExportSettings.PathToExportDataMain;
            var objectTypesToProcess = threadInfo.ExportSettings.ExportSettingsDetails.ObjectTypesToProcessC;

            string connectionString = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)" +
                                      $"(HOST={settingsConnection.Host})(PORT={settingsConnection.Port}))" +
                                      $"(CONNECT_DATA=(SID={settingsConnection.SID})));" +
                                      $"User Id={settingsConnection.UserName};Password={settingsConnection.PasswordC};";


            try
            {

                var currentSchemaDescr =
                    $"{settingsConnection.UserName}@{settingsConnection.Host}:{settingsConnection.Port}/{settingsConnection.SID}";

                if (ct.IsCancellationRequested)
                {
                    progressManager.ReportCurrentProgress(ExportProgressDataLevel.CANCEL,
                        ExportProgressDataStage.UNPLANNED_EXIT, null, 0,
                        0, true, null, 0, null, null);
                    return;
                }

                progressManager.ReportCurrentProgress(ExportProgressDataLevel.STAGESTARTINFO,
                    ExportProgressDataStage.PROCESS_SCHEMA, currentObjectName, currentObjectNumber,
                    totalObjectsToProcess, false, currentSchemaDescr, 0, null, null);

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
                                totalObjectsToProcess, false, currentObjectTypes, 0, null, null);

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
                                            totalObjectsToProcess, true, null, 0, null, null);
                                        return;
                                    }

                                    progressManager.ReportCurrentProgress(ExportProgressDataLevel.STAGESTARTINFO,
                                        ExportProgressDataStage.PROCESS_OBJECT, currentObjectName, currentObjectNumber,
                                        totalObjectsToProcess, false, null, 0, null, null);
                                    if (objectType == "PACKAGES")
                                    {
                                        ddlPackageHead = GetObjectDdlForPackageHeader(packagesHeaders, objectName,
                                            exportSettingsDetails.AddSlashToC);
                                        ddlPackageBody = GetObjectDdlForPackageBody(packagesBodies, objectName,
                                            exportSettingsDetails.AddSlashToC);
                                    }
                                    else if (objectType == "FUNCTIONS")
                                    {
                                        ddl = GetObjectDdlForSourceText(functionsText, objectName, objectType,
                                            exportSettingsDetails.AddSlashToC);
                                    }
                                    else if (objectType == "PROCEDURES")
                                    {
                                        ddl = GetObjectDdlForSourceText(proceduresText, objectName, objectType,
                                            exportSettingsDetails.AddSlashToC);
                                    }
                                    else if (objectType == "TRIGGERS")
                                    {
                                        ddl = GetObjectDdlForSourceText(triggersText, objectName, objectType,
                                            exportSettingsDetails.AddSlashToC);
                                    }
                                    else if (objectType == "TYPES")
                                    {
                                        ddl = GetObjectDdlForSourceText(typesText, objectName, objectType,
                                            exportSettingsDetails.AddSlashToC);
                                    }
                                    else if (objectType == "SYNONYMS")
                                    {
                                        ddl = GetObjectDdlForSynonym(synonymsStructs, objectName,
                                            exportSettingsDetails.AddSlashToC);
                                    }
                                    else if (objectType == "SEQUENCES")
                                    {
                                        ddl = GetObjectDdlForSequence(sequencesStructs, objectName,
                                            exportSettingsDetails.SetSequencesValuesTo1,
                                            exportSettingsDetails.AddSlashToC);
                                    }
                                    else if (objectType == "VIEWS")
                                    {
                                        ddl = GetObjectDdlForView(viewsText, tablesAndViewsColumnStruct, viewsComments,
                                            tablesAndViewsColumnsComments, objectName,
                                            exportSettingsDetails.AddSlashToC);
                                    }
                                    else if (objectType == "TABLES")
                                    {

                                        ddl = GetObjectDdlForTable(tablesStructs, tablesAndViewsColumnStruct,
                                            tablesConstraints,
                                            tablesIndexes, tablesComments, tablesAndViewsColumnsComments, partTables,
                                            objectName, userId, exportSettingsDetails.AddSlashToC);
                                    }
                                    else if (objectType == "JOBS")
                                    {
                                        ddl = GetObjectDdlForSchedulerJob(schedulerJobsStructs, objectName,
                                            exportSettingsDetails.AddSlashToC);
                                        if (!string.IsNullOrWhiteSpace(ddl))
                                            currentObjIsSchedulerJob = true;
                                        else
                                        {
                                            ddl = GetObjectDdlForDBMSJob(dbmsJobsStructs, objectName,
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
                                        ddl = AddCreateOrReplace(dbWorker.GetObjectSource(objectName, objectType,
                                            exportSettingsDetails.AddSlashToC,
                                            ExportProgressDataStage.GET_UNKNOWN_OBJECT_DDL, totalObjectsToProcess,
                                            currentObjectNumber, out canceledByUser));
                                        if (canceledByUser) return;
                                    }

                                    string objectGrants = GetObjectGrants(grants, objectName,
                                        exportSettingsDetails.OrderGrantOptionsC);
                                    if (objectType != "PACKAGES")
                                    {
                                        ddl = GetDDlWithGrants(ddl, objectGrants);
                                    }
                                    else
                                    {
                                        ddlPackageHead = GetDDlWithGrants(ddlPackageHead, objectGrants);
                                        ddlPackageBody = GetDDlWithGrants(ddlPackageBody, objectGrants);
                                        ddl = MergeHeadAndBody(ddlPackageHead, ddlPackageBody);
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
                                        totalObjectsToProcess, false, null, 0, null, null);

                                    currentObjectName = string.Empty;

                                }
                                catch (Exception ex)
                                {
                                    progressManager.ReportCurrentProgress(ExportProgressDataLevel.ERROR,
                                        ExportProgressDataStage.UNPLANNED_EXIT, currentObjectName, currentObjectNumber,
                                        totalObjectsToProcess, false, null, 0, ex.Message, ex.StackTrace);
                                    currentObjectNumber--;
                                    currentTypeObjectsCounter--;
                                }
                            }

                            progressManager.ReportCurrentProgress(ExportProgressDataLevel.STAGEENDINFO,
                                ExportProgressDataStage.PROCESS_OBJECT_TYPE, currentObjectName, currentObjectNumber,
                                totalObjectsToProcess, false, currentObjectTypes, currentTypeObjectsCounter, null,
                                null);
                        }
                        catch (Exception ex)
                        {
                            progressManager.ReportCurrentProgress(ExportProgressDataLevel.ERROR,
                                ExportProgressDataStage.UNPLANNED_EXIT, currentObjectName, currentObjectNumber,
                                totalObjectsToProcess, false, null, 0, ex.Message, ex.StackTrace);
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


                progressManager.ReportCurrentProgress(ExportProgressDataLevel.STAGEENDINFO,
                    ExportProgressDataStage.PROCESS_SCHEMA, currentObjectName, currentObjectNumber,
                    totalObjectsToProcess, true, currentSchemaDescr, currentObjectNumber, null, null);
            }
            catch (Exception ex)
            {
                progressManager.ReportCurrentProgress(ExportProgressDataLevel.ERROR,
                    ExportProgressDataStage.UNPLANNED_EXIT, currentObjectName, currentObjectNumber,
                    totalObjectsToProcess, true, null, 0, ex.Message, ex.StackTrace);
            }
        }

        public static void SetNewProcess(string dbLogPrefix, Connection dbLogConnection, out string processId)
        {
            processId = "1";
            {
                //TODO формируется запись в главный лог БД типа
                //insert into {префикс}PROCESS (id, connections_to_process, start_time) values ({префикс}PROCESS_seq.Nextval, {количество отобранных заданий}, {текущее системное время});
                //и возвращается вставленный идентификатор
            }
        }

        public static void UpdateProcess(string dbLogPrefix, Connection dbLogConnection, string processId)
        {
            {
                //TODO обновляется время завершения процесса
                //update {префикс}PROCESS set end_time = {текущее системное время} where id={processId};
            }
        }
    }
}
