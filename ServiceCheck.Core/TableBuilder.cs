using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TableBuilder
{
    public enum Alignment
    {
        Left,
        Center,
        Right
    }

    public class Column
    {
        public string Id { get; set; }
        public int MinWidth { get; set; } = 0;
        public int? MaxWidth { get; set; } = null;
        public bool WrapText { get; set; } = false;
        public int Padding { get; set; } = 1;
        public Alignment Alignment { get; set; } = Alignment.Center;
    }

    public class HeaderCell
    {
        public int ColSpan { get; set; } = 1;
        public int RowSpan { get; set; } = 1;
        public string Content { get; set; }
        public string ColumnId { get; set; }
    }

    public class DataCell
    {
        public int ColSpan { get; set; } = 1;
        public int RowSpan { get; set; } = 1;
        public string Content { get; set; }
        public string ColumnId { get; set; }
    }

    public class TableBuilder
    {
        public List<Column> Columns { get; set; } = new List<Column>();
        public List<List<HeaderCell>> HeaderRows { get; set; } = new List<List<HeaderCell>>();
        public List<List<DataCell>> DataRows { get; set; } = new List<List<DataCell>>();

        public override string ToString()
        {
            var columnWidths = CalculateColumnWidths();
            var columnPositions = CalculateColumnPositions(columnWidths);
            return RenderTable(columnWidths, columnPositions);
        }

        private int[] CalculateColumnWidths()
        {
            int columnCount = Columns.Count;
            var widths = new int[columnCount];

            for (int i = 0; i < columnCount; i++)
            {
                widths[i] = Columns[i].MinWidth;
            }

            // Process header cells
            foreach (var row in HeaderRows)
            {
                foreach (var cell in row)
                {
                    int colIndex = Columns.FindIndex(c => c.Id == cell.ColumnId);
                    if (colIndex < 0) continue;

                    var column = Columns[colIndex];
                    int cellWidth = cell.Content.Length + 2 * column.Padding;

                    if (cell.ColSpan == 1)
                    {
                        if (column.MaxWidth.HasValue)
                            cellWidth = Math.Min(cellWidth, column.MaxWidth.Value);

                        widths[colIndex] = Math.Max(widths[colIndex], cellWidth);
                    }
                    else
                    {
                        int spanEnd = Math.Min(colIndex + cell.ColSpan, columnCount);
                        int totalSpanWidth = 0;

                        for (int i = colIndex; i < spanEnd; i++)
                        {
                            totalSpanWidth += widths[i];
                            if (i > colIndex) totalSpanWidth += 1;
                        }

                        if (totalSpanWidth < cellWidth)
                        {
                            int extraWidth = cellWidth - totalSpanWidth;
                            int columnsInSpan = spanEnd - colIndex;
                            int widthPerColumn = (int)Math.Ceiling((double)extraWidth / columnsInSpan);

                            for (int i = colIndex; i < spanEnd; i++)
                            {
                                widths[i] += widthPerColumn;

                                if (Columns[i].MaxWidth.HasValue)
                                    widths[i] = Math.Min(widths[i], Columns[i].MaxWidth.Value);
                            }
                        }
                    }
                }
            }

            // Process data cells
            foreach (var row in DataRows)
            {
                foreach (var cell in row)
                {
                    int colIndex = Columns.FindIndex(c => c.Id == cell.ColumnId);
                    if (colIndex < 0) continue;

                    var column = Columns[colIndex];
                    int cellWidth = cell.Content.Length + 2 * column.Padding;

                    if (cell.ColSpan == 1)
                    {
                        if (column.MaxWidth.HasValue)
                            cellWidth = Math.Min(cellWidth, column.MaxWidth.Value);

                        widths[colIndex] = Math.Max(widths[colIndex], cellWidth);
                    }
                    else
                    {
                        int spanEnd = Math.Min(colIndex + cell.ColSpan, columnCount);
                        int totalSpanWidth = 0;

                        for (int i = colIndex; i < spanEnd; i++)
                        {
                            totalSpanWidth += widths[i];
                            if (i > colIndex) totalSpanWidth += 1;
                        }

                        if (totalSpanWidth < cellWidth)
                        {
                            int extraWidth = cellWidth - totalSpanWidth;
                            int columnsInSpan = spanEnd - colIndex;
                            int widthPerColumn = (int)Math.Ceiling((double)extraWidth / columnsInSpan);

                            for (int i = colIndex; i < spanEnd; i++)
                            {
                                widths[i] += widthPerColumn;

                                if (Columns[i].MaxWidth.HasValue)
                                    widths[i] = Math.Min(widths[i], Columns[i].MaxWidth.Value);
                            }
                        }
                    }
                }
            }

            return widths;
        }

        private int[] CalculateColumnPositions(int[] columnWidths)
        {
            int[] positions = new int[columnWidths.Length + 1];
            positions[0] = 0;

            for (int i = 0; i < columnWidths.Length; i++)
            {
                positions[i + 1] = positions[i] + columnWidths[i] + 1;
            }

            return positions;
        }

        private string RenderTable(int[] columnWidths, int[] columnPositions)
        {
            var result = new StringBuilder();

            // Render top border
            result.AppendLine(RenderHorizontalBorder(columnWidths, '='));

            // Render header
            var headerGrid = BuildHeaderGrid(columnWidths, columnPositions);
            for (int i = 0; i < headerGrid.Count; i++)
            {
                result.Append(RenderHeaderGridRow(headerGrid[i], columnWidths, columnPositions));

                if (i < headerGrid.Count - 1)
                {
                    // For rows with rowspan, we need to render special borders
                    result.AppendLine(RenderHeaderRowBorder(headerGrid[i], columnWidths, columnPositions));
                }
                else
                {
                    result.AppendLine(RenderHorizontalBorder(columnWidths, '='));
                }
            }

            // Render data
            for (int i = 0; i < DataRows.Count; i++)
            {
                var row = DataRows[i];
                result.Append(RenderDataRow(row, columnWidths, columnPositions));

                if (i < DataRows.Count - 1)
                    result.AppendLine(RenderHorizontalBorder(columnWidths, '-'));
                else
                    result.AppendLine(RenderHorizontalBorder(columnWidths, '='));
            }

            return result.ToString();
        }

        private List<List<HeaderCell>> BuildHeaderGrid(int[] columnWidths, int[] columnPositions)
        {
            var grid = new List<List<HeaderCell>>();
            var rowSpans = new Dictionary<int, HeaderCell>();

            foreach (var row in HeaderRows)
            {
                var gridRow = new List<HeaderCell>();
                int currentCol = 0;

                while (currentCol < Columns.Count)
                {
                    // Check if this column is covered by a rowspan from previous rows
                    if (rowSpans.ContainsKey(currentCol))
                    {
                        gridRow.Add(rowSpans[currentCol]);
                        currentCol += rowSpans[currentCol].ColSpan;
                        continue;
                    }

                    // Find cell for current column
                    var cell = row.FirstOrDefault(c =>
                    {
                        int colIndex = Columns.FindIndex(col => col.Id == c.ColumnId);
                        return colIndex <= currentCol && colIndex + c.ColSpan > currentCol;
                    });

                    if (cell != null)
                    {
                        gridRow.Add(cell);

                        // Track rowspan cells
                        if (cell.RowSpan > 1)
                        {
                            for (int i = 0; i < cell.ColSpan; i++)
                            {
                                rowSpans[currentCol + i] = cell;
                            }
                        }

                        currentCol += cell.ColSpan;
                    }
                    else
                    {
                        // Empty cell
                        gridRow.Add(null);
                        currentCol++;
                    }
                }

                grid.Add(gridRow);

                // Decrement rowspan counters and remove completed ones
                var keysToRemove = new List<int>();
                foreach (var kvp in rowSpans.ToList())
                {
                    // Create a new cell with decremented rowspan
                    var newCell = new HeaderCell
                    {
                        ColSpan = kvp.Value.ColSpan,
                        RowSpan = kvp.Value.RowSpan - 1,
                        Content = "", // Empty content for continuation rows
                        ColumnId = kvp.Value.ColumnId
                    };

                    rowSpans[kvp.Key] = newCell;

                    if (newCell.RowSpan <= 1)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    rowSpans.Remove(key);
                }
            }

            return grid;
        }

        private string RenderHeaderGridRow(List<HeaderCell> row, int[] columnWidths, int[] columnPositions)
        {
            var result = new StringBuilder();
            var lines = new List<string[]>();
            var maxLines = 1;

            // Prepare content lines
            foreach (var cell in row)
            {
                if (cell == null)
                {
                    lines.Add(new string[0]);
                    continue;
                }

                int colIndex = Columns.FindIndex(c => c.Id == cell.ColumnId);
                if (colIndex < 0)
                {
                    lines.Add(new string[0]);
                    continue;
                }

                int width = columnPositions[colIndex + cell.ColSpan] - columnPositions[colIndex] - 1;
                var cellLines = WrapText(cell.Content, width - 2 * Columns[colIndex].Padding, true);
                lines.Add(cellLines);
                maxLines = Math.Max(maxLines, cellLines.Length);
            }

            // Render each line
            for (int lineIdx = 0; lineIdx < maxLines; lineIdx++)
            {
                result.Append("|");
                int currentCol = 0;
                int cellIndex = 0;

                while (currentCol < Columns.Count)
                {
                    var cell = row[cellIndex];

                    if (cell != null)
                    {
                        int colIndex = Columns.FindIndex(c => c.Id == cell.ColumnId);
                        int colSpan = cell.ColSpan;

                        var cellLines = lines[cellIndex];
                        string lineContent = lineIdx < cellLines.Length ? cellLines[lineIdx] : "";
                        int width = columnPositions[colIndex + colSpan] - columnPositions[colIndex] - 1;

                        result.Append(AlignText(lineContent, width, Alignment.Center, Columns[colIndex].Padding));
                        result.Append("|");

                        currentCol += colSpan;
                    }
                    else
                    {
                        // Empty cell
                        int width = columnWidths[currentCol];
                        result.Append(AlignText("", width, Alignment.Center, 1));
                        result.Append("|");
                        currentCol++;
                    }

                    cellIndex++;
                }
                result.AppendLine();
            }

            return result.ToString();
        }

        private string RenderHeaderRowBorder(List<HeaderCell> row, int[] columnWidths, int[] columnPositions)
        {
            var result = new StringBuilder("+");
            int currentCol = 0;
            int cellIndex = 0;

            while (currentCol < Columns.Count)
            {
                var cell = row[cellIndex];
                if (cell == null)
                {
                    // Handle empty cell
                    int width = columnWidths[currentCol];
                    result.Append(new string('-', width));
                    result.Append('+');
                    currentCol++;
                    cellIndex++;
                    continue;
                }

                int colIndex = Columns.FindIndex(c => c.Id == cell.ColumnId);
                if (colIndex < 0)
                {
                    // Skip if column not found
                    currentCol++;
                    cellIndex++;
                    continue;
                }

                int colSpan = cell.ColSpan;
                int cellWidth = columnPositions[colIndex + colSpan] - columnPositions[colIndex] - 1;

                // Determine the fill character based on rowspan
                char fillChar = cell.RowSpan > 1 ? ' ' : '-';

                result.Append(new string(fillChar, cellWidth));
                result.Append('+');

                currentCol += colSpan;
                cellIndex++;
            }

            return result.ToString();
        }

        private string RenderDataRow(List<DataCell> row, int[] columnWidths, int[] columnPositions)
        {
            var result = new StringBuilder();
            var lines = new List<string[]>();
            var maxLines = 1;

            // Prepare content lines
            foreach (var cell in row)
            {
                int colIndex = Columns.FindIndex(c => c.Id == cell.ColumnId);
                if (colIndex < 0) continue;

                var column = Columns[colIndex];
                int width = columnPositions[colIndex + cell.ColSpan] - columnPositions[colIndex] - 1;
                var cellLines = WrapText(cell.Content, width - 2 * column.Padding, column.WrapText);
                lines.Add(cellLines);
                maxLines = Math.Max(maxLines, cellLines.Length);
            }

            // Render each line
            for (int lineIdx = 0; lineIdx < maxLines; lineIdx++)
            {
                result.Append("|");
                int cellIdx = 0;
                int currentCol = 0;

                while (currentCol < Columns.Count)
                {
                    // Find the cell that covers the current column
                    var cell = row.FirstOrDefault(c =>
                    {
                        int colIndex = Columns.FindIndex(col => col.Id == c.ColumnId);
                        return colIndex <= currentCol && colIndex + c.ColSpan > currentCol;
                    });

                    if (cell != null)
                    {
                        int colIndex = Columns.FindIndex(c => c.Id == cell.ColumnId);
                        var column = Columns[colIndex];
                        int colSpan = cell.ColSpan;

                        var cellLines = lines[row.IndexOf(cell)];
                        string lineContent = lineIdx < cellLines.Length ? cellLines[lineIdx] : "";
                        int width = columnPositions[colIndex + colSpan] - columnPositions[colIndex] - 1;

                        result.Append(AlignText(lineContent, width, column.Alignment, column.Padding));
                        result.Append("|");

                        currentCol += colSpan;
                    }
                    else
                    {
                        // Empty cell
                        int width = columnWidths[currentCol];
                        result.Append(AlignText("", width, Alignment.Center, 1));
                        result.Append("|");
                        currentCol++;
                    }
                }
                result.AppendLine();
            }

            return result.ToString();
        }

        private string RenderHorizontalBorder(int[] columnWidths, char borderChar)
        {
            var result = new StringBuilder("+");
            foreach (var width in columnWidths)
            {
                result.Append(new string(borderChar, width));
                result.Append("+");
            }
            return result.ToString();
        }

        private string[] WrapText(string text, int width, bool wrapText)
        {
            if (!wrapText || text.Length <= width) return new[] { text };

            var lines = new List<string>();
            var words = text.Split(' ');
            var currentLine = new StringBuilder();

            foreach (var word in words)
            {
                if (currentLine.Length + word.Length + 1 <= width)
                {
                    if (currentLine.Length > 0) currentLine.Append(' ');
                    currentLine.Append(word);
                }
                else
                {
                    if (currentLine.Length > 0) lines.Add(currentLine.ToString());
                    currentLine.Clear();

                    if (word.Length > width)
                    {
                        for (int i = 0; i < word.Length; i += width)
                        {
                            if (i + width <= word.Length)
                                lines.Add(word.Substring(i, width));
                            else
                                currentLine.Append(word.Substring(i));
                        }
                    }
                    else
                    {
                        currentLine.Append(word);
                    }
                }
            }

            if (currentLine.Length > 0) lines.Add(currentLine.ToString());
            return lines.ToArray();
        }

        private string AlignText(string text, int width, Alignment alignment, int padding)
        {
            int availableWidth = width - 2 * padding;
            if (text.Length > availableWidth) text = text.Substring(0, availableWidth);

            string paddingStr = new string(' ', padding);

            switch (alignment)
            {
                case Alignment.Left:
                    return paddingStr + text.PadRight(availableWidth) + paddingStr;
                case Alignment.Right:
                    return paddingStr + text.PadLeft(availableWidth) + paddingStr;
                case Alignment.Center:
                default:
                    int spaces = availableWidth - text.Length;
                    int leftSpaces = spaces / 2;
                    int rightSpaces = spaces - leftSpaces;
                    return paddingStr + new string(' ', leftSpaces) + text + new string(' ', rightSpaces) + paddingStr;
            }
        }
    }
}