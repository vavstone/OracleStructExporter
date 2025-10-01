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

        public static List<List<string>> ReadCsv(string filePath, string delimiter)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            var result = new List<List<string>>();

            using (var reader = new StreamReader(filePath, Encoding.UTF8))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (string.IsNullOrEmpty(line))
                        continue;

                    var row = ParseCsvLine(line, delimiter);
                    result.Add(row);
                }
            }

            return result;
        }

        private static List<string> ParseCsvLine(string line, string delimiter)
        {
            var fields = new List<string>();
            bool inQuotedField = false;
            int currentFieldStart = 0;
            StringBuilder currentField = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (!inQuotedField)
                    {
                        // Start of a quoted field
                        inQuotedField = true;
                        currentFieldStart = i + 1; // Start after the opening quote
                    }
                    else
                    {
                        // We're inside a quoted field
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            // Escaped double quote ("")
                            currentField.Append('"');
                            i++; // Skip the next quote
                        }
                        else
                        {
                            // Closing quote for the field
                            inQuotedField = false;
                        }
                    }
                }
                else if (c == delimiter[0] && !inQuotedField)
                {
                    // Found a delimiter, and we're not inside quotes
                    // Check if it's the full delimiter (for multi-char delimiters)
                    if (delimiter.Length == 1 || IsFullDelimiter(line, i, delimiter))
                    {
                        // Add the field to our list
                        fields.Add(currentField.ToString());
                        currentField.Clear();
                        if (delimiter.Length > 1)
                        {
                            i += delimiter.Length - 1;
                        }

                        continue;
                    }
                    else
                    {
                        currentField.Append(c);
                    }
                }
                else
                {
                    // Regular character, add it to the current field
                    currentField.Append(c);
                }
            }

            // Add the last field
            fields.Add(currentField.ToString());

            return fields;
        }

        private static bool IsFullDelimiter(string line, int position, string delimiter)
        {
            if (position + delimiter.Length > line.Length)
                return false;

            for (int i = 0; i < delimiter.Length; i++)
            {
                if (line[position + i] != delimiter[i])
                    return false;
            }

            return true;
        }
    }
}
