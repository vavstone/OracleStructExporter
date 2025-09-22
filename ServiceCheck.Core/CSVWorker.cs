using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ServiceCheck.Core
{
    public static class CSVWorker
    {
        public static void WriteCsv(List<List<string>> data, string delimiter, string filePath)
        {
            if (data == null)
                throw new ArgumentNullException("data");
        
            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                foreach (List<string> row in data)
                {
                    List<string> escapedValues = new List<string>();
                    foreach (string value in row)
                    {
                        escapedValues.Add(EscapeCsvValue(value, delimiter));
                    }
                    writer.WriteLine(string.Join(delimiter, escapedValues));
                }
            }
        }

        private static string EscapeCsvValue(string value, string delimiter)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            // Экранируем кавычки и проверяем необходимость обрамления в кавычки
            bool containsSpecialCharacters = value.Contains("\"") || 
                                             value.Contains("\n") || 
                                             value.Contains("\r") ||
                                             value.Contains(delimiter);

            if (containsSpecialCharacters)
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }
    }
}
