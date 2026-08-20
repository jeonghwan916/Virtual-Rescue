using System.Collections.Generic;
using System.Text;

namespace VirtualRescue.EditorTools.DialogueCsvAuthoring
{
    internal static class DialogueCsvParser
    {
        public static List<string[]> Parse(string csvText)
        {
            List<string[]> rows = new();
            if (string.IsNullOrEmpty(csvText))
            {
                return rows;
            }

            List<string> currentRow = new();
            StringBuilder currentCell = new();
            bool isInsideQuote = false;

            for (int index = 0; index < csvText.Length; index++)
            {
                char current = csvText[index];
                if (current == '"')
                {
                    if (isInsideQuote &&
                        index + 1 < csvText.Length &&
                        csvText[index + 1] == '"')
                    {
                        currentCell.Append('"');
                        index++;
                    }
                    else
                    {
                        isInsideQuote = !isInsideQuote;
                    }
                }
                else if (current == ',' && !isInsideQuote)
                {
                    currentRow.Add(currentCell.ToString());
                    currentCell.Clear();
                }
                else if ((current == '\n' || current == '\r') && !isInsideQuote)
                {
                    if (current == '\r' &&
                        index + 1 < csvText.Length &&
                        csvText[index + 1] == '\n')
                    {
                        index++;
                    }

                    currentRow.Add(currentCell.ToString());
                    rows.Add(currentRow.ToArray());
                    currentRow.Clear();
                    currentCell.Clear();
                }
                else
                {
                    currentCell.Append(current);
                }
            }

            if (currentCell.Length > 0 || currentRow.Count > 0)
            {
                currentRow.Add(currentCell.ToString());
                rows.Add(currentRow.ToArray());
            }

            return rows;
        }

        public static string CreateRow(IReadOnlyList<string> cells)
        {
            StringBuilder builder = new();
            for (int index = 0; index < cells.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(',');
                }

                builder.Append(Escape(cells[index]));
            }

            return builder.ToString();
        }

        private static string Escape(string value)
        {
            value ??= string.Empty;
            bool requiresQuote =
                value.Contains(",") ||
                value.Contains("\"") ||
                value.Contains("\n") ||
                value.Contains("\r");
            if (!requiresQuote)
            {
                return value;
            }

            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}
