using ServiceCheck.Core.DBStructs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceCheck.Core
{
    public static class DDLCreator
    {

        static void BindConstraintsColumnsAndIndexes(List<ConstraintStruct> constraints, List<TableColumnStruct> tableColumns, List<IndexStruct> indexes)
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
                foreach (var indexColStruct in index.IndexColumnStructs)
                {
                    indexColStruct.BindedTableColumnStruct = tableColumns.FirstOrDefault(c =>
                        c.TableName == indexColStruct.TableName && c.ColumnName == indexColStruct.ColumnName);
                }
            }
        }

        public static string AddCreateOrReplace(string source)
        {
            source = source.Trim();
            if (!string.IsNullOrWhiteSpace(source) &&
                !source.StartsWith("CREATE OR REPLACE", StringComparison.OrdinalIgnoreCase))
            {
                return $"CREATE OR REPLACE {source}";
            }

            return source;
        }

        public static string GetDDlWithGrants(string ddl, string grants)
        {
            var res = ddl;
            if (!string.IsNullOrWhiteSpace(grants))
                res += grants;
            return res + Environment.NewLine;
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
                    //Для представления ранее - в upper, затем - берем оригинальное, пробуем
                    //var colName = isTable ? comment.ColumnName.ToLower() : comment.ColumnName.ToUpper();
                    var colName = isTable ? comment.ColumnName.ToLower() : comment.ColumnName;
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

        public static string MergeHeadAndBody(string header, string body)
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

        public static string GetObjectGrants(List<GrantAttributes> grantsAttributes, string objectName,
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

        static string GetContstraintText(ConstraintStruct constraint, List<ConstraintStruct> constraints,
            string schemaName)
        {
            StringBuilder sb = new StringBuilder();
            var addConstrNameStr = constraint.Generated == "USER NAME"
                ? $" constraint {constraint.ConstraintName.ToUpper()}"
                : "";

            if (constraint.ConstraintType.ToUpper() == "C")
            {
                sb.AppendLine($"{addConstrNameStr}");
                sb.Append($"  check ({constraint.SearchCondition})");
            }
            else
            {
                var columns_str = getMergedColumns(constraint.ConstraintColumnStructs.OrderBy(c => c.Position)
                    .Select(c => c.ColumnName).ToList());

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

                sb.Append($"{addConstrNameStr} {typeKey} {columns_str}");
            }
            


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

        static void AddJobArgsString(StringBuilder sb, string curArg, Dictionary<string, string> jobCreateArgs, int prefixLength)
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

        public static string GetObjectDdlForSchedulerJob(List<SchedulerJob> shedulerJobsList, string objectName, List<string> addSlashTo)
        {
            var job = shedulerJobsList.FirstOrDefault(c => c.JobName.ToUpper() == objectName.ToUpper());
            if (job == null)
                return string.Empty;

            var sb = new StringBuilder("begin");
            sb.AppendLine();

            Dictionary<string, string> jobCreateArgs = new Dictionary<string, string>();
            jobCreateArgs["job_name"] = $"'{job.JobName}'";
            if (!string.IsNullOrWhiteSpace(job.JobType))
                jobCreateArgs["job_type"] = $"'{job.JobType}'";
            if (!string.IsNullOrWhiteSpace(job.JobAction))
                jobCreateArgs["job_action"] = $"'{job.JobAction}'";
            if (job.NumberOfArguments != null && job.NumberOfArguments > 0)
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
            sb.AppendPadded(" ", longestArglength - curArg.Length + 1);
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

        public static string GetObjectDdlForDBMSJob(List<DBMSJob> dbmsJobs, string objectName, List<string> addSlashTo)
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
                jobCreateArgs["what"] = $"'{job.What.Replace("'", "''")}'";
            var strDate = job.NextTime.ToString("dd-MM-yyyy HH:mm:ss");
            jobCreateArgs["next_date"] = $"to_date('{strDate}', 'dd-mm-yyyy hh24:mi:ss')";
            if (job.Interval != null)
                jobCreateArgs["interval"] = $"'{job.Interval.Replace("'", "''")}'";
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

        public static string GetObjectDdlForTable(
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

            BindConstraintsColumnsAndIndexes(curTableConstraints, curTableColumnsStruct, curTableIndexes);

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
                    if (!string.IsNullOrWhiteSpace(addToEnd)) addToEnd += Environment.NewLine;
                    addToEnd += "rowdependencies";
                }
                if (curTableStruct != null && curTableStruct.IOTType == "IOT")
                {
                    if (!string.IsNullOrWhiteSpace(addToEnd)) addToEnd += Environment.NewLine;
                    addToEnd += "organization index";
                }
                if (curTableStruct != null && curTableStruct.Logging == "NO")
                {
                    if (!string.IsNullOrWhiteSpace(addToEnd)) addToEnd += Environment.NewLine;
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
                    if (dict.ContainsKey("START WITH") && dict["START WITH"] != "1")
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

                if (col.VirtualColumn == "NO" || string.IsNullOrWhiteSpace(col.DataDefault))
                {
                    var colDataTypeToShow = col.DataType.ToUpper();
                    if (colDataTypeToShow == "NUMBER" && col.DataScale != null && col.DataScale == 0 &&
                        (col.DataPrecision == null || col.DataPrecision == 0))
                        colDataTypeToShow = "INTEGER";

                    var dataTypeOwnerAddStr = string.IsNullOrWhiteSpace(col.DataTypeOwner)
                        ? ""
                        : col.DataTypeOwner.ToUpper() + ".";

                    sbString.Append(dataTypeOwnerAddStr + colDataTypeToShow);
                    if (col.DataType.ToUpper() == "VARCHAR2" || col.DataType.ToUpper() == "NVARCHAR2" ||
                        col.DataType.ToUpper() == "CHAR" || col.DataType.ToUpper() == "RAW" ||
                        col.DataType.ToUpper() == "UROWID")
                    {
                        var charDataLength = col.CharLength;
                        if (charDataLength == null || charDataLength == 0)
                            charDataLength = col.DataLength;
                        var charUsedAddStr = "";
                        if (col.CharUsed == "C" &&
                            (col.DataType.ToUpper() == "VARCHAR2" || col.DataType.ToUpper() == "CHAR"))
                            charUsedAddStr = " CHAR";
                        sbString.Append($"({charDataLength}{charUsedAddStr})");
                    }

                    if ((col.DataType.ToUpper() == "NUMBER" && col.DataPrecision != null) ||
                        (col.DataType.ToUpper() == "FLOAT" && col.DataPrecision != null &&
                         col.DataPrecision < 126)) //почему-то с 126 не нужно выводить точность
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
                }
                else
                {
                    sbString.Append($"as ({col.DataDefault})");
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
                sb.Append(" " + GetContstraintText(constraint, tablesConstraints, schemaName));
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
                    var partLessThenAppend = "";
                    if (curPartTable.PartitioningType == "RANGE" && !string.IsNullOrWhiteSpace(curPartTable.Interval))
                    {
                        sb.Append($" interval ({curPartTable.Interval})");
                        partLessThenAppend = " less than";
                    }

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
                            sb.AppendLine($"  partition {partition.PartitionName} values{partLessThenAppend} ({partition.HighValue})");
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
                                        : $" values{partLessThenAppend} ({subPartition.HighValue})";
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
                        var useExpressionInsteadColNameInIndex = true;
                        if (idxColumnStruct.BindedTableColumnStruct == null ||
                            idxColumnStruct.BindedTableColumnStruct.HiddenColumn == "NO"/* &&
                            !string.IsNullOrWhiteSpace(idxColumnStruct.Expression)*/)
                            useExpressionInsteadColNameInIndex = false;

                        var colOrExprStr = useExpressionInsteadColNameInIndex
                            ? idxColumnStruct.Expression
                            : idxColumnStruct.ColumnName.ToUpper();
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
                    case "DOMAIN":
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
                if (index.IndexType == "DOMAIN" || 
                    index.Compression == "ENABLED" || 
                    index.Logging != "YES" || 
                    index.Locality == "LOCAL")
                {
                    sb.AppendLine();
                    sb.Append("  ");
                    if (index.IndexType == "DOMAIN")
                    {
                        sb.Append($"indextype is {index.ItypOwner}.{index.ItypName}");
                    }
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
                              c.ConstraintType.ToUpper() == "U" || 
                              (c.ConstraintType.ToUpper() == "C" && c.Generated=="USER NAME")) &&
                             c.ConstraintColumnStructs.Any() &&
                             (c.BindedIndexStruct == null || c.BindedIndexStruct.IndexType != "IOT - TOP"))
                         .OrderBy(c => GetOrdererSymbols(c.ConstraintType.ToUpper(),new List<string> {"P", "U","R","C"})).
                         ThenBy(c => c.ConstraintName, new OracleLikeStringComparer()))
            {
                sb.AppendLine("");
                sb.AppendLine($"alter table {objectNameUpper}");
                sb.Append("  add");
                sb.Append(GetContstraintText(constraint, tablesConstraints, schemaName));
                sb.Append(";");
                if (constraint.BindedIndexStruct != null && constraint.BindedIndexStruct.Logging == "NO")
                {
                    sb.AppendLine();
                    sb.Append($"alter index {constraint.ConstraintName} nologging;");
                }
            }

            if (addSlashTo.Contains("TABLES"))
            {
                sb.AppendLine();
                sb.Append("/");
            }

            return sb.ToString();
        }

        public static int GetOrdererSymbols(string inputSymbol, List<string> orderList)
        {
            int i = 1;
            foreach (var cur in orderList)
            {
                if (inputSymbol == cur)
                    return i;
                i++;
            }

            return 999;
        }

        public static string GetObjectDdlForView(Dictionary<string, string> views, List<TableColumnStruct> columnsStructs, List<TableOrViewComment> viewComments,
            List<ColumnComment> columnsComments, string objectName, List<string> addSlashTo)
        {
            var objectNameUpper = objectName.ToUpper();
            var viewText = views[objectName];

            var curViewColumnsStruct =
                columnsStructs.Where(c => c.TableName.ToUpper() == objectNameUpper).ToList();
            var colsToShow = curViewColumnsStruct.Where(c => c.HiddenColumn != "YES").OrderBy(c => c.ColumnId)
                .ToList();

            var colCompar = new ViewColumnComparator();
            var needExplColList =
                colCompar.NeedExplicitColumnList(colsToShow.Select(c => c.ColumnNameToShowWithoutBrackets).ToList(), viewText);
            var explColList = " ";
            if (needExplColList)
            {
                explColList = Environment.NewLine+"(";
                for (int i = 0;i<colsToShow.Count;i++)
                {
                    var colName = colsToShow[i].ColumnNameToShow;
                    explColList += colName;
                    if (i < colsToShow.Count - 1)
                        explColList += ", ";
                }
                explColList += ")"+Environment.NewLine;
            }

            var sb = new StringBuilder($"create or replace force view {objectName.ToLower()}{explColList}as");
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

        public static string GetObjectDdlForSequence(List<SequenceAttributes> sequenceAttributesList, string objectName, bool resetStartValueTo1, List<string> addSlashTo)
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

        public static string GetObjectDdlForSynonym(List<SynonymAttributes> synonymAttributesList, string objectName, List<string> addSlashTo)
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

        public static string GetObjectDdlForSourceText(Dictionary<string, string> funcText, string objectName, string objectType, List<string> addSlashTo)
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

        public static string GetObjectDdlForPackageHeader(Dictionary<string, string> packagesHeaders, string objectName, List<string> addSlashTo)
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

        public static string GetObjectDdlForPackageBody(Dictionary<string, string> packagesBodies, string objectName, List<string> addSlashTo)
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


    }
}
