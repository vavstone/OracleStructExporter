﻿namespace OracleStructExporter.Core
{
    public class ConstraintColumnStruct
    {
        public string ConstraintName { get; set; }
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public int? Position { get; set; }
    }
}
