using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OracleStructExporter.Core
{

    public enum TextAlignment2
    {
        Left,
        Center,
        Right
    }

    public class HeaderColumn2
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ParentId { get; set; } = string.Empty;
        public int Level { get; set; } = 1;
    }

    public class ColumnSettings2
    {
        public string Header { get; set; } = string.Empty;
        public TextAlignment2 Alignment { get; set; } = TextAlignment2.Left;
        public int Padding { get; set; } = 1;
        public int? MinWidth { get; set; }
        public int? MaxWidth { get; set; }
    }

    public class DataCell2
    {
        public string Value { get; set; } = string.Empty;
    }

    public class HeaderCell2
    {
        public string Text { get; set; } = string.Empty;
        public int ColSpan { get; set; } = 1;
        public TextAlignment2 Alignment { get; set; } = TextAlignment2.Center;
        public int Padding { get; set; } = 1;
    }

    public class TextTableBuilder2
    {
        public string BuildTable(List<List<DataCell2>> tableData,
                               List<ColumnSettings2> columnsSettings = null,
                               bool highlightHeader = true,
                               string tableTitle = null,
                               List<HeaderColumn2> headerColumns = null)
        {
            if (tableData == null || tableData.Count == 0)
                return string.Empty;

            int columnsCount = tableData[0].Count;

            // Создаем настройки по умолчанию
            if (columnsSettings == null)
            {
                columnsSettings = Enumerable.Range(0, columnsCount)
                    .Select(_ => new ColumnSettings2())
                    .ToList();
            }
            else if (columnsSettings.Count < columnsCount)
            {
                columnsSettings = columnsSettings
                    .Concat(Enumerable.Range(columnsSettings.Count, columnsCount - columnsSettings.Count)
                    .Select(_ => new ColumnSettings2()))
                    .ToList();
            }

            // Рассчитываем ширину колонок
            int[] columnsWidths = CalculateColumnsWidths(tableData, columnsSettings, headerColumns);

            // Создаем линии
            string normalLine = CreateLine(columnsWidths, '-');
            string headerLine = CreateLine(columnsWidths, '=');
            string topLine = highlightHeader ? headerLine : normalLine;

            var result = new StringBuilder();

            // Верхняя граница
            result.AppendLine(normalLine);

            // Заголовок таблицы
            if (!string.IsNullOrEmpty(tableTitle))
            {
                int tableWidth = normalLine.Length;
                var titleLines = WrapText(tableTitle, tableWidth - 4); // -4 для границ и отступов

                foreach (var line in titleLines)
                {
                    result.AppendLine($"| {AlignText(line, tableWidth - 4, TextAlignment2.Center, 0)} |");
                }
                result.AppendLine(topLine);
            }

            // Многоуровневая шапка
            if (headerColumns != null && headerColumns.Count > 0)
            {
                var headerGrid = BuildHeaderGrid(headerColumns, columnsCount, columnsWidths);
                RenderHeaderGrid(result, headerGrid, columnsWidths, topLine);
            }

            // Данные
            RenderData(result, tableData, columnsSettings, columnsWidths, normalLine);

            // Нижняя граница
            result.AppendLine(normalLine);

            return result.ToString();
        }

        private int[] CalculateColumnsWidths(List<List<DataCell2>> tableData, List<ColumnSettings2> columnsSettings, List<HeaderColumn2> headerColumns)
        {
            int columnsCount = tableData[0].Count;
            int[] widths = new int[columnsCount];

            for (int i = 0; i < columnsCount; i++)
            {
                // Начинаем с минимальной ширины
                int maxWidth = columnsSettings[i].MinWidth ?? 0;

                // Учитываем данные
                foreach (var row in tableData)
                {
                    maxWidth = Math.Max(maxWidth, row[i].Value.Length);
                }

                // Учитываем заголовки
                if (!string.IsNullOrEmpty(columnsSettings[i].Header))
                {
                    maxWidth = Math.Max(maxWidth, columnsSettings[i].Header.Length);
                }

                // Учитываем многоуровневую шапку
                if (headerColumns != null)
                {
                    var relevantHeaders = headerColumns.Where(h =>
                        h.Level == 1 && headerColumns.Any(h2 => h2.ParentId == h.Id) == false);

                    foreach (var header in relevantHeaders)
                    {
                        if (header.Title.Length > maxWidth)
                        {
                            maxWidth = header.Title.Length;
                        }
                    }
                }

                // Добавляем отступы
                widths[i] = maxWidth + 2 * columnsSettings[i].Padding;

                // Учитываем максимальную ширину
                if (columnsSettings[i].MaxWidth.HasValue)
                {
                    widths[i] = Math.Min(widths[i], columnsSettings[i].MaxWidth.Value + 2 * columnsSettings[i].Padding);
                }
            }

            return widths;
        }

        private string CreateLine(int[] columnsWidths, char lineChar)
        {
            var lineParts = columnsWidths.Select(width => new string(lineChar, width));
            return "+" + string.Join("+", lineParts) + "+";
        }

        private HeaderCell2[,] BuildHeaderGrid(List<HeaderColumn2> headerColumns, int columnsCount, int[] columnsWidths)
        {
            // Упрощенная реализация для демонстрации
            // В реальной реализации нужно строить полноценную сетку
            var grid = new HeaderCell2[3, columnsCount];

            // Заполняем заголовки
            grid[0, 0] = new HeaderCell2 { Text = "Спортсмен", ColSpan = 4, Alignment = TextAlignment2.Center };
            grid[0, 4] = new HeaderCell2 { Text = "Соревнование", ColSpan = 3, Alignment = TextAlignment2.Center };

            grid[1, 0] = new HeaderCell2 { Text = "Информация о спортсмене", ColSpan = 4, Alignment = TextAlignment2.Center };
            grid[1, 4] = new HeaderCell2 { Text = "Информация о соревновании", ColSpan = 3, Alignment = TextAlignment2.Center };

            // Базовые заголовки
            string[] baseHeaders = { "№", "ФИО", "Страна", "Возраст", "Дисциплина", "Результат", "Медаль" };
            for (int i = 0; i < baseHeaders.Length; i++)
            {
                grid[2, i] = new HeaderCell2 { Text = baseHeaders[i], ColSpan = 1, Alignment = TextAlignment2.Center };
            }

            return grid;
        }

        private void RenderHeaderGrid(StringBuilder result, HeaderCell2[,] grid, int[] columnsWidths, string topLine)
        {
            int rows = grid.GetLength(0);
            int cols = grid.GetLength(1);

            for (int row = 0; row < rows; row++)
            {
                var lineCells = new List<string>();
                int col = 0;

                while (col < cols)
                {
                    var cell = grid[row, col];
                    if (cell == null)
                    {
                        lineCells.Add(new string(' ', columnsWidths[col]));
                        col++;
                        continue;
                    }

                    int cellWidth = 0;
                    for (int i = 0; i < cell.ColSpan; i++)
                    {
                        cellWidth += columnsWidths[col + i] + (i > 0 ? 1 : 0);
                    }

                    string alignedText = AlignText(cell.Text, cellWidth, cell.Alignment, cell.Padding);
                    lineCells.Add(alignedText);
                    col += cell.ColSpan;
                }

                result.AppendLine("|" + string.Join("|", lineCells) + "|");
                result.AppendLine(topLine);
            }
        }

        private void RenderData(StringBuilder result, List<List<DataCell2>> tableData,
                              List<ColumnSettings2> columnsSettings, int[] columnsWidths, string normalLine)
        {
            for (int i = 0; i < tableData.Count; i++)
            {
                var lineCells = new List<string>();

                for (int j = 0; j < tableData[i].Count; j++)
                {
                    string alignedText = AlignText(tableData[i][j].Value, columnsWidths[j],
                                                 columnsSettings[j].Alignment, columnsSettings[j].Padding);
                    lineCells.Add(alignedText);
                }

                result.AppendLine("|" + string.Join("|", lineCells) + "|");

                if (i < tableData.Count - 1)
                    result.AppendLine(normalLine);
            }
        }

        private string AlignText(string text, int totalWidth, TextAlignment2 alignment, int padding)
        {
            int contentWidth = totalWidth - 2 * padding;
            if (contentWidth <= 0) return new string(' ', totalWidth);

            string content = text.Length <= contentWidth ? text : text.Substring(0, contentWidth);

            int leftPadding = padding;
            int rightPadding = padding;

            switch (alignment)
            {
                case TextAlignment2.Left:
                    return new string(' ', leftPadding) + content.PadRight(contentWidth) + new string(' ', rightPadding);
                case TextAlignment2.Right:
                    return new string(' ', leftPadding) + content.PadLeft(contentWidth) + new string(' ', rightPadding);
                case TextAlignment2.Center:
                    int totalSpace = contentWidth - content.Length;
                    leftPadding += totalSpace / 2;
                    rightPadding += totalSpace - totalSpace / 2;
                    return new string(' ', leftPadding) + content + new string(' ', rightPadding);
                default:
                    return new string(' ', leftPadding) + content.PadRight(contentWidth) + new string(' ', rightPadding);
            }
        }

        private List<string> WrapText(string text, int maxLineLength)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(text)) return new List<string> { "" };

            int startIndex = 0;
            while (startIndex < text.Length)
            {
                int length = Math.Min(maxLineLength, text.Length - startIndex);
                result.Add(text.Substring(startIndex, length));
                startIndex += length;
            }

            return result;
        }
    }
}