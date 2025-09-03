using System;
using System.Collections.Generic;
using System.Data.OracleClient;
using System.Linq;
using System.Text;

namespace OracleStructExporter.Core
{
    public static class Extensions
    {
        public static void AppendPadded(this StringBuilder builder, string value, int length)
        {
            builder.Append($"{value}{new string(' ', length)}".Substring(0, length));
        }
        public static void AppendPadded(this StringBuilder builder, int value, int length)
        {
            builder.Append($"{new string('0', length)}{value}".Reverse().ToString().Substring(0, length).Reverse());
        }

        public static bool ContainsLowerCaseSymbols(this string str)
        {
            return str.Any(char.IsLower);
        }

        public static string EscapeNotValidInSqlSymbols(this string str)
        {
            string[] charsToTrim = { "'" };
            var strTmp = str;
            foreach (var item in charsToTrim)
            {
                strTmp = strTmp.Replace(item, item + item);
            }

            return strTmp;
        }

        public static string MergeFormatted(this List<string> strList, string eachItemFramingString, string splitterString)
        {
            if (strList==null || !strList.Any()) return null;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < strList.Count; i++)
            {
                var item = strList[i];
                sb.Append($"{eachItemFramingString}{item}{eachItemFramingString}");
                if (i < strList.Count - 1)
                    sb.Append(splitterString);
            }
            return sb.ToString();
        }

        public static string MergeFormatted(this Dictionary<string,bool> dictionary, string splitter)
        {
            if (dictionary == null || !dictionary.Any()) return null;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < dictionary.Count; i++)
            {
                var key = dictionary.Keys.ElementAt(i);
                var val = dictionary[key] ? "да" : "нет";
                sb.Append($"{key}: {val}");
                if (i < dictionary.Count - 1)
                    sb.Append(splitter);
            }
            return sb.ToString();
        }

        public static Dictionary<string,string> SplitToDictionary(this string str, string mainSplitter, string keyvalueSplitter, bool trimKeysAndValues)
        {
            if (string.IsNullOrWhiteSpace(str)) return null;
            var res = new Dictionary<string, string>();
            var pairs = str.Split(new []{mainSplitter}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var items = pair.Split(new[] {keyvalueSplitter},  StringSplitOptions.RemoveEmptyEntries);
                if (items.Length == 2)
                {
                    var key = trimKeysAndValues ? items[0].Trim() : items[0];
                    var val = trimKeysAndValues ? items[1].Trim() : items[1];
                    res[key] = val;
                }
                else if (items.Length==1)
                {
                    var key = trimKeysAndValues ? items[0].Trim() : items[0];
                    res[key] = null;
                }
                else
                {
                    throw new Exception("Непредусмотренная строка словаря - " + str);
                }
            }

            return res;
        }

        public static void AddNullableParam(this OracleCommand cmd, string paramName, OracleType paramType, object paramValue)
        {
            if (paramValue == null)
                cmd.Parameters.Add(paramName, paramType).Value = DBNull.Value;
            else
                cmd.Parameters.Add(paramName, paramType).Value = paramValue;
        }
    }
}
