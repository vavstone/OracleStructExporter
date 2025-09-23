using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ServiceCheck.Core
{

    public class ViewColumnComparator
    {
        /// <summary>
        /// Определяет, нужно ли включать явный список полей в DDL для представления
        /// </summary>
        public bool NeedExplicitColumnList(List<string> columnsFromTabCols, string viewText)
        {
            if (columnsFromTabCols == null || !columnsFromTabCols.Any() || string.IsNullOrWhiteSpace(viewText))
                return false;

            // Нормализуем текст представления
            string normalizedViewText = NormalizeViewText(viewText);

            // Извлекаем имена колонок из SELECT запроса (самый внешний уровень)
            List<string> selectColumnNames = ExtractColumnNamesFromSelect(normalizedViewText);

            // Если количество колонок не совпадает - нужен явный список
            if (!selectColumnNames.Any() || selectColumnNames.Count != columnsFromTabCols.Count)
                return true;

            // Сравниваем имена колонок
            return !AreColumnNamesEqual(columnsFromTabCols, selectColumnNames);
        }

        private string NormalizeViewText(string viewText)
        {
            // Удаляем комментарии
            string result = Regex.Replace(viewText, @"--.*$", "", RegexOptions.Multiline);
            result = Regex.Replace(result, @"/\*.*?\*/", "", RegexOptions.Singleline);

            // Заменяем множественные пробелы на один
            result = Regex.Replace(result, @"\s+", " ");

            return result.Trim();
        }

        private List<string> ExtractColumnNamesFromSelect(string viewText)
        {
            var columnNames = new List<string>();

            // Находим начало основного SELECT (игнорируя подзапросы)
            int selectIndex = FindMainSelectIndex(viewText);
            if (selectIndex == -1) return columnNames;

            // Находим FROM основного SELECT
            int fromIndex = FindFromIndex(viewText, selectIndex + 6);
            if (fromIndex == -1) return columnNames;

            // Извлекаем часть между SELECT и FROM
            string columnsPart = viewText.Substring(selectIndex + 6, fromIndex - (selectIndex + 6)).Trim();

            if (string.IsNullOrWhiteSpace(columnsPart) || columnsPart == "*")
                return columnNames;

            // Парсим колонки
            return ParseColumnNames(columnsPart);
        }

        private int FindMainSelectIndex(string viewText)
        {
            // Ищем первый SELECT, который не находится внутри скобок (основной уровень)
            int bracketCount = 0;
            bool inQuotes = false;
            char quoteChar = '\0';

            for (int i = 0; i < viewText.Length; i++)
            {
                char c = viewText[i];

                if ((c == '\'' || c == '"') && !inQuotes)
                {
                    inQuotes = true;
                    quoteChar = c;
                }
                else if (inQuotes && c == quoteChar)
                {
                    inQuotes = false;
                }

                if (!inQuotes)
                {
                    if (c == '(') bracketCount++;
                    else if (c == ')') bracketCount--;

                    if (bracketCount == 0 && i + 6 <= viewText.Length)
                    {
                        if (viewText.Substring(i, 6).Equals("SELECT", StringComparison.OrdinalIgnoreCase))
                        {
                            return i;
                        }
                    }
                }
            }

            return -1;
        }

        private int FindFromIndex(string text, int startIndex)
        {
            int bracketCount = 0;
            bool inQuotes = false;
            char quoteChar = '\0';

            for (int i = startIndex; i < text.Length; i++)
            {
                char c = text[i];

                if ((c == '\'' || c == '"') && !inQuotes)
                {
                    inQuotes = true;
                    quoteChar = c;
                }
                else if (inQuotes && c == quoteChar)
                {
                    inQuotes = false;
                }

                if (!inQuotes)
                {
                    if (c == '(') bracketCount++;
                    else if (c == ')') bracketCount--;

                    if (bracketCount == 0 && i + 4 <= text.Length)
                    {
                        if (text.Substring(i, 4).Equals("FROM", StringComparison.OrdinalIgnoreCase))
                        {
                            //убедиться, что FROM обрамлено пробелами или скобками
                            if (i > 0 && 
                                (char.IsWhiteSpace(text[i - 1]) || text[i - 1]==')') && 
                                (char.IsWhiteSpace(text[i + 4]) || text[i + 4]=='('))
                                return i;
                        }
                    }
                }
            }

            return -1;
        }

        private List<string> ParseColumnNames(string columnsPart)
        {
            var columnNames = new List<string>();
            int bracketCount = 0;
            bool inQuotes = false;
            char quoteChar = '\0';
            int start = 0;

            for (int i = 0; i < columnsPart.Length; i++)
            {
                char c = columnsPart[i];

                if ((c == '\'' || c == '"') && !inQuotes)
                {
                    inQuotes = true;
                    quoteChar = c;
                }
                else if (inQuotes && c == quoteChar)
                {
                    inQuotes = false;
                }

                if (!inQuotes)
                {
                    if (c == '(') bracketCount++;
                    else if (c == ')') bracketCount--;

                    if (bracketCount == 0 && c == ',')
                    {
                        string columnExpr = columnsPart.Substring(start, i - start).Trim();
                        string columnName = ExtractColumnName(columnExpr);
                        if (!string.IsNullOrEmpty(columnName))
                            columnNames.Add(columnName);
                        start = i + 1;
                    }
                }
            }

            // Последняя колонка
            if (start < columnsPart.Length)
            {
                string lastColumnExpr = columnsPart.Substring(start).Trim();
                string lastColumnName = ExtractColumnName(lastColumnExpr);
                if (!string.IsNullOrEmpty(lastColumnName))
                    columnNames.Add(lastColumnName);
            }

            return columnNames;
        }

        private string ExtractColumnName(string columnExpression)
        {
            string expr = columnExpression.Trim();

            // Случай 1: Колонка в двойных кавычках - извлекаем как есть
            if (expr.StartsWith("\"") && expr.EndsWith("\""))
            {
                //если после случая 1 внутри остались кавычки, то и колонка и псевдоним в кавычках. чистим и идем дальше
                var exprTmp = expr.Substring(1, expr.Length - 2);
                if (!exprTmp.Contains("\""))
                    return exprTmp;
            }

            // Случай 2: Выражение с псевдонимом в кавычках
            Match quotedAlias = Regex.Match(expr, @"\s+""([^""]+)""$");
            if (quotedAlias.Success)
            {
                return quotedAlias.Groups[1].Value;
            }

            // Случай 3: Выражение с AS и кавычками
            Match asQuoted = Regex.Match(expr, @"\s+AS\s+""([^""]+)""$", RegexOptions.IgnoreCase);
            if (asQuoted.Success)
            {
                return asQuoted.Groups[1].Value;
            }

            // Случай 4: Выражение с AS без кавычек
            Match asSimple = Regex.Match(expr, @"\s+AS\s+([^\s,]+)$", RegexOptions.IgnoreCase);
            if (asSimple.Success)
            {
                return asSimple.Groups[1].Value;
            }

            // Случай 5: Псевдоним без AS (последнее слово после пробела)
            if (expr.Contains(' '))
            {
                string[] parts = expr.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                string lastPart = parts[parts.Length - 1];

                // Проверяем, что последняя часть не является ключевым словом SQL
                if (!IsSqlKeyword(lastPart) && IsValidIdentifier(lastPart))
                {
                    if (lastPart.StartsWith("\"") && lastPart.EndsWith("\""))
                        return lastPart.Substring(1, lastPart.Length - 2);
                    return lastPart;
                }
            }

            // Случай 6: Простое имя колонки (без пробелов и операторов)
            if (!expr.Contains(' ') && !expr.Contains('(') && !expr.Contains(')') &&
                !expr.Contains('+') && !expr.Contains('-') && !expr.Contains('*') &&
                !expr.Contains('/') && !expr.Contains('|'))
            {
                // Если есть квалификатор таблицы, берем только имя колонки
                if (expr.Contains('.'))
                {
                    string[] parts = expr.Split('.');
                    return parts[parts.Length - 1];
                }

                return expr;
            }

            // Случай 7: Сложное выражение без явного псевдонима
            // Возвращаем выражение как есть - при сравнении оно не совпадет с ожидаемым именем
            return expr;
        }

        private bool IsSqlKeyword(string word)
        {
            string[] keywords =
            {
                "SELECT", "FROM", "WHERE", "GROUP", "ORDER", "BY", "HAVING", "JOIN",
                "INNER", "OUTER", "LEFT", "RIGHT", "FULL", "ON", "AND", "OR", "NOT",
                "AS", "ASC", "DESC", "UNION", "ALL", "CASE", "WHEN", "THEN", "ELSE", "END",
                "NULL", "LIKE", "IN", "EXISTS", "BETWEEN", "IS", "DISTINCT", "WITH"
            };

            string cleanWord = word.Trim('"', '\'');
            return keywords.Contains(cleanWord.ToUpper());
        }

        private bool IsValidIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return false;

            string cleanId = identifier.Trim('"', '\'');
            return Regex.IsMatch(cleanId, @"^[a-zA-Z_а-яА-ЯёЁ][a-zA-Z0-9_а-яА-ЯёЁ\s\/\.]*$");
        }

        private bool AreColumnNamesEqual(List<string> expected, List<string> actual)
        {
            if (expected.Count != actual.Count)
                return false;

            for (int i = 0; i < expected.Count; i++)
            {
                if (!string.Equals(expected[i].Replace("\"",""), actual[i].Replace("\"",""), StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }
    }



}
