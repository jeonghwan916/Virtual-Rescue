using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;

namespace VirtualRescue.EditorTools.SituationAuthoring
{
    internal static class SituationDialogueCsvService
    {
        private const string DialogueCsvPath =
            "Assets/Resources/Loop Dialogue Sheets/Loop - Dialogue.csv";
        private const string TextCsvPath =
            "Assets/Resources/Loop Dialogue Sheets/Loop - Text.csv";
        private const string DefaultLanguage = "kr";
        private const string DefaultDelayAfterAudio = "3";

        public static bool TryAppendMissingRows(
            IReadOnlyList<SituationDialogueCsvEntry> entries,
            out string message)
        {
            if (entries == null || entries.Count == 0)
            {
                message = "No dialogue entries were provided.";
                return false;
            }

            string dialogueAbsolutePath =
                SituationAuthoringUtility.ToAbsolutePath(DialogueCsvPath);
            string textAbsolutePath =
                SituationAuthoringUtility.ToAbsolutePath(TextCsvPath);

            if (!File.Exists(dialogueAbsolutePath))
            {
                message = $"Loop Dialogue CSV was not found: {DialogueCsvPath}";
                return false;
            }

            if (!File.Exists(textAbsolutePath))
            {
                message = $"Loop Text CSV was not found: {TextCsvPath}";
                return false;
            }

            HashSet<string> dialogueIds = ReadFirstColumnIds(dialogueAbsolutePath);
            HashSet<string> textIds = ReadFirstColumnIds(textAbsolutePath);
            List<string> dialogueRows = new();
            List<string> textRows = new();
            int skippedExisting = 0;

            foreach (SituationDialogueCsvEntry entry in entries)
            {
                string id = entry.Id?.Trim() ?? string.Empty;
                string text = entry.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(id) && string.IsNullOrEmpty(text))
                {
                    continue;
                }

                if (string.IsNullOrEmpty(id))
                {
                    message = "Dialogue text cannot be saved without a Dialogue ID.";
                    return false;
                }

                bool hasDialogueRow = dialogueIds.Contains(id);
                bool hasTextRow = textIds.Contains(id);
                if (hasDialogueRow && hasTextRow)
                {
                    skippedExisting++;
                    continue;
                }

                if (!hasDialogueRow)
                {
                    dialogueRows.Add(
                        string.Join(
                            ",",
                            Escape(id),
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            DefaultDelayAfterAudio));
                    dialogueIds.Add(id);
                }

                if (!hasTextRow)
                {
                    if (string.IsNullOrEmpty(text))
                    {
                        message =
                            $"Dialogue text is required for missing Text CSV row: {id}";
                        return false;
                    }

                    textRows.Add(
                        string.Join(
                            ",",
                            Escape(id),
                            DefaultLanguage,
                            Escape(text)));
                    textIds.Add(id);
                }
            }

            if (dialogueRows.Count == 0 && textRows.Count == 0)
            {
                message = skippedExisting > 0
                    ? "Dialogue CSV rows already exist."
                    : "No dialogue rows were added.";
                return true;
            }

            AppendRows(dialogueAbsolutePath, dialogueRows);
            AppendRows(textAbsolutePath, textRows);
            AssetDatabase.ImportAsset(DialogueCsvPath);
            AssetDatabase.ImportAsset(TextCsvPath);
            AssetDatabase.Refresh();

            message =
                $"Dialogue rows added. Dialogue: {dialogueRows.Count}, " +
                $"Text: {textRows.Count}, skipped: {skippedExisting}.";
            return true;
        }

        private static HashSet<string> ReadFirstColumnIds(string path)
        {
            HashSet<string> ids = new(StringComparer.Ordinal);
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            for (int index = 1; index < lines.Length; index++)
            {
                string id = ReadFirstCell(lines[index]).Trim();
                if (!string.IsNullOrEmpty(id))
                {
                    ids.Add(id);
                }
            }

            return ids;
        }

        private static string ReadFirstCell(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return string.Empty;
            }

            StringBuilder builder = new();
            bool insideQuote = false;
            for (int index = 0; index < line.Length; index++)
            {
                char current = line[index];
                if (current == '"')
                {
                    if (insideQuote && index + 1 < line.Length &&
                        line[index + 1] == '"')
                    {
                        builder.Append('"');
                        index++;
                    }
                    else
                    {
                        insideQuote = !insideQuote;
                    }
                }
                else if (current == ',' && !insideQuote)
                {
                    break;
                }
                else
                {
                    builder.Append(current);
                }
            }

            return builder.ToString();
        }

        private static void AppendRows(string path, IReadOnlyList<string> rows)
        {
            if (rows == null || rows.Count == 0)
            {
                return;
            }

            string existing = File.ReadAllText(path, Encoding.UTF8);
            StringBuilder builder = new();
            if (existing.Length > 0 && !existing.EndsWith("\n", StringComparison.Ordinal))
            {
                builder.AppendLine();
            }

            foreach (string row in rows)
            {
                builder.AppendLine(row);
            }

            File.AppendAllText(path, builder.ToString(), Encoding.UTF8);
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

    internal readonly struct SituationDialogueCsvEntry
    {
        public SituationDialogueCsvEntry(string id, string text)
        {
            Id = id;
            Text = text;
        }

        public string Id { get; }
        public string Text { get; }
    }
}
