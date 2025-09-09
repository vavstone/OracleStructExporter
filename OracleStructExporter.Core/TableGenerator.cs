using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TableBuilder;

namespace OracleStructExporter.Core
{
    /// <summary>
    /// от Алисы
    /// </summary>
    public class TableGenerator
    {
        private readonly List<Column> _columns;
        private readonly List<List<HeaderCell>> _headerRows;
        private readonly List<List<DataCell>> _dataRows;

        public TableGenerator(List<Column> columns, List<List<HeaderCell>> headerRows, List<List<DataCell>> dataRows)
        {
            _columns = columns;
            _headerRows = headerRows;
            _dataRows = dataRows;
        }

        public string GenerateTable()
        {
            var columnWidths = CalculateColumnWidths();
            var headerGrid = BuildHeaderGrid(columnWidths);
            var dataGrid = BuildDataGrid(columnWidths);

            var sb = new StringBuilder();
            RenderHeader(sb, headerGrid, columnWidths);
            RenderData(sb, dataGrid, columnWidths);
            return sb.ToString();
        }

        private Dictionary<string, int> CalculateColumnWidths()
        {
            var widthCandidates = new Dictionary<string, List<int>>();

            foreach (var column in _columns)
            {
                widthCandidates[column.Id] = new List<int> { column.MinWidth };
            }

            // Process header content
            foreach (var row in _headerRows)
            {
                foreach (var cell in row)
                {
                    var contentWidth = cell.Content.Length + 2; // padding
                    widthCandidates[cell.ColumnId].Add(contentWidth);
                }
            }

            // Process data content
            foreach (var row in _dataRows)
            {
                foreach (var cell in row)
                {
                    var column = _columns.First(c => c.Id == cell.ColumnId);
                    var contentWidth = cell.Content.Length + 2 * column.Padding;
                    widthCandidates[cell.ColumnId].Add(contentWidth);
                }
            }

            var result = new Dictionary<string, int>();
            foreach (var column in _columns)
            {
                var maxContentWidth = widthCandidates[column.Id].Max();
                var calculatedWidth = Math.Max(column.MinWidth, maxContentWidth);

                if (column.MaxWidth.HasValue)
                    calculatedWidth = Math.Min(calculatedWidth, column.MaxWidth.Value);

                result[column.Id] = calculatedWidth;
            }

            return result;
        }

        private List<List<List<string>>> BuildHeaderGrid(Dictionary<string, int> columnWidths)
        {
            var grid = new List<List<List<string>>>();

            foreach (var row in _headerRows)
            {
                var gridRow = new List<List<string>>();

                foreach (var cell in row)
                {
                    var column = _columns.First(c => c.Id == cell.ColumnId);
                    var width = columnWidths[cell.ColumnId] * cell.ColSpan + (cell.ColSpan - 1);

                    var lines = WrapText(cell.Content, width - 2, true);
                    gridRow.Add(lines);
                }

                grid.Add(gridRow);
            }

            return grid;
        }

        private List<List<List<string>>> BuildDataGrid(Dictionary<string, int> columnWidths)
        {
            var grid = new List<List<List<string>>>();

            foreach (var row in _dataRows)
            {
                var gridRow = new List<List<string>>();

                foreach (var cell in row)
                {
                    var column = _columns.First(c => c.Id == cell.ColumnId);
                    var width = columnWidths[cell.ColumnId] * cell.ColSpan + (cell.ColSpan - 1);
                    var availableWidth = width - 2 * column.Padding;

                    var lines = column.WrapText
                        ? WrapText(cell.Content, availableWidth, false)
                        : TruncateText(cell.Content, availableWidth);

                    gridRow.Add(lines);
                }

                grid.Add(gridRow);
            }

            return grid;
        }

        private List<string> WrapText(string text, int maxWidth, bool isHeader)
        {
            if (string.IsNullOrEmpty(text)) return new List<string> { "" };

            var words = text.Split(' ');
            var lines = new List<string>();
            var currentLine = new StringBuilder();

            foreach (var word in words)
            {
                if (currentLine.Length + word.Length + 1 <= maxWidth)
                {
                    currentLine.Append(word + " ");
                }
                else
                {
                    lines.Add(currentLine.ToString().Trim());
                    currentLine.Clear();
                    currentLine.Append(word + " ");
                }
            }

            lines.Add(currentLine.ToString().Trim());

            if (isHeader)
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    lines[i] = AlignText(lines[i], maxWidth, Alignment.Center);
                }
            }

            return lines;
        }

        private List<string> TruncateText(string text, int maxWidth)
        {
            if (text.Length <= maxWidth) return new List<string> { text };
            return new List<string> { text.Substring(0, maxWidth - 3) + "..." };
        }

        private string AlignText(string text, int width, Alignment alignment)
        {
            if (text.Length >= width) return text;

            switch (alignment)
            {
                case Alignment.Left:
                    return text.PadRight(width);
                case Alignment.Right:
                    return text.PadLeft(width);
                default:
                    var padding = width - text.Length;
                    var leftPadding = padding / 2;
                    var rightPadding = padding - leftPadding;
                    return new string(' ', leftPadding) + text + new string(' ', rightPadding);
            }
        }

        private void RenderHeader(StringBuilder sb, List<List<List<string>>> headerGrid, Dictionary<string, int> columnWidths)
        {
            // Implementation similar to RenderData but with centered alignment
            // and different border styles
        }

        private void RenderData(StringBuilder sb, List<List<List<string>>> dataGrid, Dictionary<string, int> columnWidths)
        {
            // Implementation of data rendering with proper borders
            // and alignment based on column settings
        }
    }
}
