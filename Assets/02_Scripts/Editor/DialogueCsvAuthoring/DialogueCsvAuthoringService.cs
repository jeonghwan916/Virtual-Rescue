using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace VirtualRescue.EditorTools.DialogueCsvAuthoring
{
    internal static class DialogueCsvAuthoringService
    {
        public const string DefaultDialogueAssetPath =
            "Assets/Resources/Loop Dialogue Sheets/Loop - Dialogue.csv";
        public const string DefaultTextAssetPath =
            "Assets/Resources/Loop Dialogue Sheets/Loop - Text.csv";
        public const string DefaultLanguage = "kr";
        public const string DefaultDelayAfterAudio = "3";

        private static readonly string[] DialogueRequiredHeaders = { "id" };
        private static readonly string[] DialogueGroupHeaders = { "group", "order" };
        private static readonly string[] TextRequiredHeaders =
            { "id", "language", "text" };

        public static DialogueCsvValidationResult ValidateFiles(
            string dialogueAssetPath,
            string textAssetPath,
            bool requiresGroup)
        {
            List<string> warnings = new();
            bool canSave = true;

            if (!TryReadRows(dialogueAssetPath, out List<string[]> dialogueRows, out string error))
            {
                warnings.Add(error);
                canSave = false;
            }
            else
            {
                HashSet<string> headers = ReadHeaderSet(dialogueRows);
                foreach (string header in DialogueRequiredHeaders)
                {
                    if (!headers.Contains(header))
                    {
                        warnings.Add($"Dialogue CSV is missing required field: {header}");
                        canSave = false;
                    }
                }

                foreach (string header in DialogueGroupHeaders)
                {
                    if (!headers.Contains(header))
                    {
                        string level = requiresGroup ? "required" : "optional";
                        warnings.Add(
                            $"Dialogue CSV is missing {level} group field: {header}");
                        if (requiresGroup)
                        {
                            canSave = false;
                        }
                    }
                }
            }

            if (!TryReadRows(textAssetPath, out List<string[]> textRows, out error))
            {
                warnings.Add(error);
                canSave = false;
            }
            else
            {
                HashSet<string> headers = ReadHeaderSet(textRows);
                foreach (string header in TextRequiredHeaders)
                {
                    if (!headers.Contains(header))
                    {
                        warnings.Add($"Text CSV is missing required field: {header}");
                        canSave = false;
                    }
                }
            }

            return new DialogueCsvValidationResult(canSave, warnings);
        }

        public static bool TryAppendRows(
            DialogueCsvSaveRequest request,
            out DialogueCsvSaveResult result)
        {
            result = default;
            if (request == null)
            {
                result = Fail("Save request is empty.");
                return false;
            }

            bool requiresGroup = request.Mode == DialogueCsvAuthoringMode.Group;
            DialogueCsvValidationResult validation = ValidateFiles(
                request.DialogueAssetPath,
                request.TextAssetPath,
                requiresGroup);
            if (!validation.CanSave)
            {
                result = new DialogueCsvSaveResult(
                    false,
                    "CSV headers are invalid.",
                    validation.Warnings,
                    0,
                    0);
                return false;
            }

            if (!TryReadRows(request.DialogueAssetPath, out List<string[]> dialogueRows, out string error))
            {
                result = Fail(error);
                return false;
            }

            if (!TryReadRows(request.TextAssetPath, out List<string[]> textRows, out error))
            {
                result = Fail(error);
                return false;
            }

            Dictionary<string, int> dialogueHeaderMap = BuildHeaderMap(dialogueRows[0]);
            Dictionary<string, int> textHeaderMap = BuildHeaderMap(textRows[0]);
            HashSet<string> existingDialogueIds = ReadIds(dialogueRows, dialogueHeaderMap["id"]);
            HashSet<string> existingTextIds = ReadIds(textRows, textHeaderMap["id"]);

            if (!TryBuildRows(
                    request,
                    dialogueHeaderMap,
                    textHeaderMap,
                    existingDialogueIds,
                    existingTextIds,
                    out List<string> dialogueRowsToAppend,
                    out List<string> textRowsToAppend,
                    out error))
            {
                result = Fail(error);
                return false;
            }

            string dialogueAbsolutePath = ToAbsolutePath(request.DialogueAssetPath);
            string textAbsolutePath = ToAbsolutePath(request.TextAssetPath);
            AppendRows(dialogueAbsolutePath, dialogueRowsToAppend);
            AppendRows(textAbsolutePath, textRowsToAppend);
            AssetDatabase.ImportAsset(request.DialogueAssetPath);
            AssetDatabase.ImportAsset(request.TextAssetPath);
            AssetDatabase.Refresh();

            result = new DialogueCsvSaveResult(
                true,
                $"Rows added. Dialogue: {dialogueRowsToAppend.Count}, Text: {textRowsToAppend.Count}.",
                validation.Warnings,
                dialogueRowsToAppend.Count,
                textRowsToAppend.Count);
            return true;
        }

        internal static bool TryBuildRows(
            DialogueCsvSaveRequest request,
            IReadOnlyDictionary<string, int> dialogueHeaderMap,
            IReadOnlyDictionary<string, int> textHeaderMap,
            ISet<string> existingDialogueIds,
            ISet<string> existingTextIds,
            out List<string> dialogueRows,
            out List<string> textRows,
            out string error)
        {
            dialogueRows = new List<string>();
            textRows = new List<string>();
            error = string.Empty;

            string baseId = request.Id?.Trim() ?? string.Empty;
            string language = string.IsNullOrWhiteSpace(request.Language)
                ? DefaultLanguage
                : request.Language.Trim();
            if (string.IsNullOrWhiteSpace(baseId))
            {
                error = "ID is required.";
                return false;
            }

            if (request.Mode == DialogueCsvAuthoringMode.Single)
            {
                DialogueCsvLine line = request.Lines.FirstOrDefault();
                if (string.IsNullOrWhiteSpace(line.Text))
                {
                    error = "Text is required.";
                    return false;
                }

                return TryAddLine(
                    baseId,
                    string.Empty,
                    string.Empty,
                    language,
                    line,
                    request,
                    dialogueHeaderMap,
                    textHeaderMap,
                    existingDialogueIds,
                    existingTextIds,
                    dialogueRows,
                    textRows,
                    out error);
            }

            string group = request.Group?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(group))
            {
                error = "Group is required for group dialogue.";
                return false;
            }

            List<DialogueCsvLine> lines = request.Lines
                .Where(line => !string.IsNullOrWhiteSpace(line.Text))
                .ToList();
            if (lines.Count == 0)
            {
                error = "At least one group line is required.";
                return false;
            }

            for (int index = 0; index < lines.Count; index++)
            {
                string id = $"{baseId}_{index + 1:000}";
                string order = (index + 1).ToString();
                if (!TryAddLine(
                        id,
                        group,
                        order,
                        language,
                        lines[index],
                        request,
                        dialogueHeaderMap,
                        textHeaderMap,
                        existingDialogueIds,
                        existingTextIds,
                        dialogueRows,
                        textRows,
                        out error))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryAddLine(
            string id,
            string group,
            string order,
            string language,
            DialogueCsvLine line,
            DialogueCsvSaveRequest request,
            IReadOnlyDictionary<string, int> dialogueHeaderMap,
            IReadOnlyDictionary<string, int> textHeaderMap,
            ISet<string> existingDialogueIds,
            ISet<string> existingTextIds,
            List<string> dialogueRows,
            List<string> textRows,
            out string error)
        {
            error = string.Empty;
            if (existingDialogueIds.Contains(id) || existingTextIds.Contains(id))
            {
                error = $"ID already exists in one of the selected CSV files: {id}";
                return false;
            }

            string[] dialogueCells = new string[GetColumnCount(dialogueHeaderMap)];
            SetCell(dialogueCells, dialogueHeaderMap, "id", id);
            SetCell(dialogueCells, dialogueHeaderMap, "group", group);
            SetCell(dialogueCells, dialogueHeaderMap, "order", order);
            SetCell(dialogueCells, dialogueHeaderMap, "speaker", FirstFilled(line.Speaker, request.Speaker));
            SetCell(dialogueCells, dialogueHeaderMap, "audioPath", FirstFilled(line.AudioPath, request.AudioPath));
            SetCell(dialogueCells, dialogueHeaderMap, "callbackKey", FirstFilled(line.CallbackKey, request.CallbackKey));
            SetCell(
                dialogueCells,
                dialogueHeaderMap,
                "delayAfterAudio",
                FirstFilled(line.DelayAfterAudio, request.DelayAfterAudio));

            string[] textCells = new string[GetColumnCount(textHeaderMap)];
            SetCell(textCells, textHeaderMap, "id", id);
            SetCell(textCells, textHeaderMap, "language", language);
            SetCell(textCells, textHeaderMap, "text", line.Text?.Trim() ?? string.Empty);

            dialogueRows.Add(DialogueCsvParser.CreateRow(dialogueCells));
            textRows.Add(DialogueCsvParser.CreateRow(textCells));
            return true;
        }

        private static bool TryReadRows(
            string assetPath,
            out List<string[]> rows,
            out string error)
        {
            rows = null;
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                error = "CSV asset path is empty.";
                return false;
            }

            string absolutePath = ToAbsolutePath(assetPath);
            if (!File.Exists(absolutePath))
            {
                error = $"CSV file was not found: {assetPath}";
                return false;
            }

            rows = DialogueCsvParser.Parse(File.ReadAllText(absolutePath, Encoding.UTF8));
            if (rows.Count == 0)
            {
                error = $"CSV has no header row: {assetPath}";
                return false;
            }

            return true;
        }

        private static Dictionary<string, int> BuildHeaderMap(IReadOnlyList<string> headers)
        {
            Dictionary<string, int> headerMap = new(StringComparer.Ordinal);
            for (int index = 0; index < headers.Count; index++)
            {
                string header = NormalizeHeader(headers[index]);
                if (!string.IsNullOrEmpty(header) && !headerMap.ContainsKey(header))
                {
                    headerMap.Add(header, index);
                }
            }

            return headerMap;
        }

        private static HashSet<string> ReadHeaderSet(IReadOnlyList<string[]> rows)
        {
            if (rows.Count == 0)
            {
                return new HashSet<string>(StringComparer.Ordinal);
            }

            return new HashSet<string>(
                BuildHeaderMap(rows[0]).Keys,
                StringComparer.Ordinal);
        }

        private static HashSet<string> ReadIds(
            IReadOnlyList<string[]> rows,
            int idColumnIndex)
        {
            HashSet<string> ids = new(StringComparer.Ordinal);
            for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                string[] row = rows[rowIndex];
                if (idColumnIndex >= row.Length)
                {
                    continue;
                }

                string id = row[idColumnIndex].Trim();
                if (!string.IsNullOrEmpty(id))
                {
                    ids.Add(id);
                }
            }

            return ids;
        }

        private static void SetCell(
            string[] cells,
            IReadOnlyDictionary<string, int> headerMap,
            string header,
            string value)
        {
            if (headerMap.TryGetValue(header, out int index))
            {
                cells[index] = value ?? string.Empty;
            }
        }

        private static int GetColumnCount(
            IReadOnlyDictionary<string, int> headerMap)
        {
            int columnCount = 0;
            foreach (int index in headerMap.Values)
            {
                columnCount = Math.Max(columnCount, index + 1);
            }

            return columnCount;
        }

        private static string FirstFilled(string first, string fallback)
        {
            return !string.IsNullOrWhiteSpace(first)
                ? first.Trim()
                : fallback?.Trim() ?? string.Empty;
        }

        private static string NormalizeHeader(string header)
        {
            return header?.Trim() ?? string.Empty;
        }

        private static void AppendRows(string path, IReadOnlyList<string> rows)
        {
            if (rows == null || rows.Count == 0)
            {
                return;
            }

            string existing = File.ReadAllText(path, Encoding.UTF8);
            StringBuilder builder = new();
            if (existing.Length > 0 &&
                !existing.EndsWith("\n", StringComparison.Ordinal))
            {
                builder.AppendLine();
            }

            foreach (string row in rows)
            {
                builder.AppendLine(row);
            }

            File.AppendAllText(path, builder.ToString(), Encoding.UTF8);
        }

        private static string ToAbsolutePath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            return Path.GetFullPath(Path.Combine(
                projectRoot ?? string.Empty,
                assetPath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static DialogueCsvSaveResult Fail(string message)
        {
            return new DialogueCsvSaveResult(
                false,
                message,
                Array.Empty<string>(),
                0,
                0);
        }
    }
}
