using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ServiceCheck.Core
{
    public static class Common
    {
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
            if (string.IsNullOrWhiteSpace(objectTypeCommonName))
                return string.Empty;
            if (!objectTypeMapping.ContainsKey(objectTypeCommonName))
                return "UNKNOWN";
            return objectTypeMapping[objectTypeCommonName];
        }

        public static string GetObjectTypeNameReverse(string typeName)
        {
            return objectTypeMapping.FirstOrDefault(c => c.Value==typeName).Key;
        }

        public static string GetExtensionForObjectType(string objectType, bool packageHasHeader, bool packageHasBody)
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

        private static readonly string[] ReservedInWindowsNames = 
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        public static string MakeValidFileName(string input, char replacement = '_')
        {
            if (input == null)
                return null;

            if (input.Length == 0)
                return string.Empty;

            // Недопустимые символы в Windows
            char[] invalidChars = { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };
        
            // Преобразуем строку, заменяя недопустимые символы
            var resultChars = input.Select(ch => 
                invalidChars.Contains(ch) ? replacement : ch
            ).ToArray();

            string result = new string(resultChars);

            // Удаляем пробелы в конце
            result = result.TrimEnd(' ');

            // Обрабатываем точку в конце
            if (result.Length > 0 && result[result.Length - 1] == '.')
            {
                if (result.Length == 1)
                {
                    // Если строка состоит только из точки
                    result = replacement.ToString();
                }
                else
                {
                    // Заменяем последнюю точку на символ замены
                    result = result.Substring(0, result.Length - 1) + replacement;
                }
            }

            // Проверяем на зарезервированные имена
            if (ReservedInWindowsNames.Contains(result.ToUpperInvariant()))
            {
                result += replacement;
            }

            // Если после всех преобразований строка пустая, возвращаем символ замены
            if (string.IsNullOrEmpty(result))
            {
                return replacement.ToString();
            }

            return result;
        }


        public static void SaveObjectsListList(List<ObjectTypeNames> names, string pathToObjListFiles, string dbFolder, string schemaName, string dbLink, string processId)
        {

            var objListFilePath = Path.Combine(pathToObjListFiles, dbFolder);
            var procIdAdd = string.IsNullOrWhiteSpace(processId)
                ? ""
                : $"_{processId}";
            var dbLinkAdd = string.IsNullOrWhiteSpace(dbLink)
                ? ""
                : $"_{dbLink.ToUpper()}";
            var fileNameWithoutExt = $"{schemaName.ToUpper()}{dbLinkAdd}{procIdAdd}";
            var fileName =  Common.MakeValidFileName(fileNameWithoutExt)+".csv";
            var fileFullName = Path.Combine(objListFilePath, fileName);
            if (!Directory.Exists(objListFilePath)) Directory.CreateDirectory(objListFilePath);

            var data = new List<List<string>>();
            var header = new List<string>
            {
                "Owner", "ObjType", "ObjName", "ObjId", "Created", "LastDDLTime", "Status", "Generated"
            };
            data.Add(header);
            foreach (var objType in names.OrderBy(c=>c.SchemaName).ThenBy(c=>c.ObjectType))
            {
                foreach (var obj in objType.Objects.OrderBy(c=>c.ObjectName).ThenBy(c=>c.ObjectType))
                {
                    var row = new List<string>
                    {
                        obj.Owner, 
                        obj.ObjectType, 
                        obj.ObjectName,
                        obj.ObjectId==null?"":obj.ObjectId.Value.ToString(),
                        obj.Created==null?"":obj.Created.Value.ToString("yyyy.MM.dd HH:mm:ss"),
                        obj.LastDDLTime==null?"":obj.LastDDLTime.Value.ToString("yyyy.MM.dd HH:mm:ss"),
                        obj.Status,
                        obj.Generated
                    };
                    data.Add(row);
                }
                
            }
            CSVWorker.WriteCsv(data, ";", fileFullName);
        }
    }
}
