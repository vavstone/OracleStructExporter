using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OracleStructExporter.Core
{

    public enum TextAlignment
    {
        Left,
        Center,
        Right
    }

    public class ColumnSettings
    {
        public string Header { get; set; } = string.Empty;
        public TextAlignment Alignment { get; set; } = TextAlignment.Left;
        public int Padding { get; set; } = 1;
        public int? MinWidth { get; set; }
        public int? MaxWidth { get; set; }
    }

    public class TextTableBuilder
    {
        public string BuildTable(List<List<string>> tableData,
            List<ColumnSettings> columnsSettings = null,
            bool highlightHeader = true,
            string tableTitle = null)
        {
            if (tableData == null || tableData.Count == 0)
                return string.Empty;

            int columnsCount = tableData[0].Count;

            if (columnsSettings == null)
            {
                columnsSettings = new List<ColumnSettings>();
                for (int i = 0; i < columnsCount; i++)
                {
                    columnsSettings.Add(new ColumnSettings());
                }
            }
            else if (columnsSettings.Count < columnsCount)
            {
                for (int i = columnsSettings.Count; i < columnsCount; i++)
                {
                    columnsSettings.Add(new ColumnSettings());
                }
            }

            bool hasHeader = columnsSettings.Any(cs => !string.IsNullOrEmpty(cs.Header));
            int[] columnsWidths = CalculateColumnsWidths(tableData, columnsSettings);

            string normalLine = CreateLine(columnsWidths, '-');
            string headerLine = CreateLine(columnsWidths, '=');

            var result = new StringBuilder();

            // Верхняя граница таблицы
            string topLine = highlightHeader && hasHeader ? headerLine : normalLine;
            result.AppendLine(topLine);

            // Заголовок таблицы (если есть)
            if (!string.IsNullOrEmpty(tableTitle))
            {
                int tableWidth = normalLine.Length - 2;
                int maxPadding = columnsSettings.Max(cs => cs.Padding);
                int availableWidth = tableWidth - 2 * maxPadding;

                var titleLines = WrapText(tableTitle, availableWidth);

                foreach (var line in titleLines)
                {
                    result.AppendLine("|" + AlignText(line, tableWidth, TextAlignment.Center, maxPadding) + "|");
                }

                result.AppendLine(topLine);
            }

            // Заголовок колонок (если есть)
            if (hasHeader)
            {
                var headerLines = new List<List<string>>();
                for (int i = 0; i < columnsCount; i++)
                {
                    int availableWidth = columnsWidths[i] - 2 * columnsSettings[i].Padding;
                    if (columnsSettings[i].MaxWidth.HasValue)
                    {
                        availableWidth = Math.Min(availableWidth, columnsSettings[i].MaxWidth.Value);
                    }

                    headerLines.Add(WrapText(columnsSettings[i].Header, availableWidth));
                }

                int maxHeaderLines = headerLines.Max(h => h.Count);
                for (int lineIndex = 0; lineIndex < maxHeaderLines; lineIndex++)
                {
                    var lineCells = new List<string>();
                    for (int i = 0; i < columnsCount; i++)
                    {
                        string headerText = lineIndex < headerLines[i].Count ? headerLines[i][lineIndex] : "";
                        lineCells.Add(" " + AlignText(headerText, columnsWidths[i], TextAlignment.Center,
                            columnsSettings[i].Padding) + " ");
                    }

                    result.AppendLine("|" + string.Join("|", lineCells) + "|");
                }

                result.AppendLine(topLine);
            }

            // Данные
            for (int i = 0; i < tableData.Count; i++)
            {
                var rowLines = new List<List<string>>();

                for (int j = 0; j < columnsCount; j++)
                {
                    int availableWidth = columnsWidths[j] - 2 * columnsSettings[j].Padding;
                    if (columnsSettings[j].MaxWidth.HasValue)
                    {
                        availableWidth = Math.Min(availableWidth, columnsSettings[j].MaxWidth.Value);
                    }

                    var cellLines = WrapText(tableData[i][j], availableWidth);
                    rowLines.Add(cellLines);
                }

                int maxLines = rowLines.Max(r => r.Count);

                for (int lineIndex = 0; lineIndex < maxLines; lineIndex++)
                {
                    var lineCells = new List<string>();

                    for (int j = 0; j < columnsCount; j++)
                    {
                        string cellLine = lineIndex < rowLines[j].Count ? rowLines[j][lineIndex] : "";
                        lineCells.Add(" " + AlignText(cellLine, columnsWidths[j], columnsSettings[j].Alignment,
                            columnsSettings[j].Padding) + " ");
                    }

                    result.AppendLine("|" + string.Join("|", lineCells) + "|");
                }

                if (i < tableData.Count - 1)
                    result.AppendLine(normalLine);
            }

            result.AppendLine(normalLine);
            return result.ToString();
        }

        private string CreateLine(int[] columnsWidths, char lineChar)
        {
            return "+" + string.Join("+", columnsWidths.Select(w => new string(lineChar, w + 2))) + "+";
        }

        private int[] CalculateColumnsWidths(List<List<string>> tableData, List<ColumnSettings> columnsSettings)
        {
            int columnsCount = tableData[0].Count;
            int[] columnsWidths = new int[columnsCount];

            for (int i = 0; i < columnsCount; i++)
            {
                // Устанавливаем минимальную ширину, если задана
                if (columnsSettings[i].MinWidth.HasValue)
                {
                    columnsWidths[i] = columnsSettings[i].MinWidth.Value + 2 * columnsSettings[i].Padding;
                }

                // Учитываем ширину заголовка
                if (!string.IsNullOrEmpty(columnsSettings[i].Header))
                {
                    int headerWidth = columnsSettings[i].Header.Length + 2 * columnsSettings[i].Padding;
                    columnsWidths[i] = Math.Max(columnsWidths[i], headerWidth);
                }

                // Учитываем ширину данных
                foreach (var row in tableData)
                {
                    int cellWidth = row[i].Length + 2 * columnsSettings[i].Padding;
                    columnsWidths[i] = Math.Max(columnsWidths[i], cellWidth);
                }

                // Учитываем максимальную ширину
                if (columnsSettings[i].MaxWidth.HasValue)
                {
                    int maxAllowedWidth = columnsSettings[i].MaxWidth.Value + 2 * columnsSettings[i].Padding;
                    columnsWidths[i] = Math.Min(columnsWidths[i], maxAllowedWidth);
                }
            }

            return columnsWidths;
        }

        private List<string> WrapText(string text, int maxLineLength)
        {
            var result = new List<string>();

            if (string.IsNullOrEmpty(text))
            {
                result.Add("");
                return result;
            }

            // Если текст помещается в одну строку
            if (text.Length <= maxLineLength)
            {
                result.Add(text);
                return result;
            }

            int startIndex = 0;
            while (startIndex < text.Length)
            {
                // Определяем длину следующей строки
                int length = Math.Min(maxLineLength, text.Length - startIndex);

                // Если это не последний фрагмент текста, пытаемся разбить по слову
                if (startIndex + length < text.Length)
                {
                    // Ищем последний пробел в текущем фрагменте
                    int lastSpace = text.LastIndexOf(' ', startIndex + length - 1, length);

                    // Если нашли пробел, используем его для разбиения
                    if (lastSpace > startIndex)
                    {
                        length = lastSpace - startIndex + 1;
                    }
                }

                result.Add(text.Substring(startIndex, length).Trim());
                startIndex += length;

                // Пропускаем пробелы в начале следующей строки
                while (startIndex < text.Length && text[startIndex] == ' ')
                    startIndex++;
            }

            return result;
        }

        private string AlignText(string value, int width, TextAlignment alignment, int padding)
        {
            if (string.IsNullOrEmpty(value))
                return new string(' ', width);

            // Учитываем отступы
            string paddedValue = new string(' ', padding) + value + new string(' ', padding);

            if (paddedValue.Length > width)
                paddedValue = paddedValue.Substring(0, width);

            switch (alignment)
            {
                case TextAlignment.Left:
                    return paddedValue.PadRight(width);
                case TextAlignment.Right:
                    return paddedValue.PadLeft(width);
                case TextAlignment.Center:
                    int totalPadding = width - paddedValue.Length;
                    int leftPadding = totalPadding / 2;
                    int rightPadding = totalPadding - leftPadding;
                    return new string(' ', leftPadding) + paddedValue + new string(' ', rightPadding);
                default:
                    return paddedValue.PadRight(width);
            }
        }
    }
}