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

        public static bool TryFindEntries(
            string dialogueAssetPath,
            string textAssetPath,
            string id,
            string group,
            string language,
            out List<DialogueCsvEditEntry> entries,
            out string message)
        {
            entries = new List<DialogueCsvEditEntry>();
            message = string.Empty;
            DialogueCsvValidationResult validation = ValidateFiles(
                dialogueAssetPath,
                textAssetPath,
                false);
            if (!validation.CanSave)
            {
                message = "CSV headers are invalid.";
                return false;
            }

            if (!TryReadRows(dialogueAssetPath, out List<string[]> dialogueRows, out string error))
            {
                message = error;
                return false;
            }

            if (!TryReadRows(textAssetPath, out List<string[]> textRows, out error))
            {
                message = error;
                return false;
            }

            Dictionary<string, int> dialogueHeaderMap = BuildHeaderMap(dialogueRows[0]);
            Dictionary<string, int> textHeaderMap = BuildHeaderMap(textRows[0]);
            string normalizedId = id?.Trim() ?? string.Empty;
            string normalizedGroup = group?.Trim() ?? string.Empty;
            string normalizedLanguage = string.IsNullOrWhiteSpace(language)
                ? DefaultLanguage
                : language.Trim();
            if (string.IsNullOrEmpty(normalizedId) &&
                string.IsNullOrEmpty(normalizedGroup))
            {
                message = "Enter an ID or Group to search.";
                return false;
            }

            if (!string.IsNullOrEmpty(normalizedGroup) &&
                !dialogueHeaderMap.ContainsKey("group"))
            {
                message = "Dialogue CSV does not have a group field.";
                return false;
            }

            Dictionary<string, string> textById = ReadTextById(
                textRows,
                textHeaderMap,
                normalizedLanguage);

            for (int rowIndex = 1; rowIndex < dialogueRows.Count; rowIndex++)
            {
                string[] row = dialogueRows[rowIndex];
                string rowId = GetCell(row, dialogueHeaderMap, "id");
                if (string.IsNullOrWhiteSpace(rowId))
                {
                    continue;
                }

                string rowGroup = GetCell(row, dialogueHeaderMap, "group");
                bool idMatches =
                    !string.IsNullOrEmpty(normalizedId) &&
                    string.Equals(rowId, normalizedId, StringComparison.Ordinal);
                bool groupMatches =
                    !string.IsNullOrEmpty(normalizedGroup) &&
                    string.Equals(rowGroup, normalizedGroup, StringComparison.Ordinal);
                if (!idMatches && !groupMatches)
                {
                    continue;
                }

                entries.Add(new DialogueCsvEditEntry
                {
                    Id = rowId,
                    Group = rowGroup,
                    Order = GetCell(row, dialogueHeaderMap, "order"),
                    Language = normalizedLanguage,
                    Text = textById.TryGetValue(rowId, out string text)
                        ? text
                        : string.Empty,
                    Speaker = GetCell(row, dialogueHeaderMap, "speaker"),
                    AudioPath = GetCell(row, dialogueHeaderMap, "audioPath"),
                    CallbackKey = GetCell(row, dialogueHeaderMap, "callbackKey"),
                    DelayAfterAudio = GetCell(row, dialogueHeaderMap, "delayAfterAudio")
                });
            }

            entries.Sort((left, right) =>
                CompareOrderThenId(left.Order, left.Id, right.Order, right.Id));
            message = entries.Count == 0
                ? "No matching dialogue rows were found."
                : $"Found {entries.Count} dialogue row(s).";
            return entries.Count > 0;
        }

        public static bool TryUpdateEntries(
            string dialogueAssetPath,
            string textAssetPath,
            IReadOnlyList<DialogueCsvEditEntry> entries,
            out string message)
        {
            message = string.Empty;
            if (entries == null || entries.Count == 0)
            {
                message = "No edit entries were provided.";
                return false;
            }

            DialogueCsvValidationResult validation = ValidateFiles(
                dialogueAssetPath,
                textAssetPath,
                false);
            if (!validation.CanSave)
            {
                message = "CSV headers are invalid.";
                return false;
            }

            if (!TryReadRows(dialogueAssetPath, out List<string[]> dialogueRows, out string error))
            {
                message = error;
                return false;
            }

            if (!TryReadRows(textAssetPath, out List<string[]> textRows, out error))
            {
                message = error;
                return false;
            }

            Dictionary<string, int> dialogueHeaderMap = BuildHeaderMap(dialogueRows[0]);
            Dictionary<string, int> textHeaderMap = BuildHeaderMap(textRows[0]);
            Dictionary<string, DialogueCsvEditEntry> entryById =
                entries.ToDictionary(entry => entry.Id, StringComparer.Ordinal);
            int dialogueUpdated = 0;
            int textUpdated = 0;

            for (int rowIndex = 1; rowIndex < dialogueRows.Count; rowIndex++)
            {
                string[] row = EnsureColumnCount(
                    dialogueRows[rowIndex],
                    dialogueRows[0].Length);
                string id = GetCell(row, dialogueHeaderMap, "id");
                if (!entryById.TryGetValue(id, out DialogueCsvEditEntry entry))
                {
                    dialogueRows[rowIndex] = row;
                    continue;
                }

                SetCell(row, dialogueHeaderMap, "group", entry.Group);
                SetCell(row, dialogueHeaderMap, "order", entry.Order);
                SetCell(row, dialogueHeaderMap, "speaker", entry.Speaker);
                SetCell(row, dialogueHeaderMap, "audioPath", entry.AudioPath);
                SetCell(row, dialogueHeaderMap, "callbackKey", entry.CallbackKey);
                SetCell(row, dialogueHeaderMap, "delayAfterAudio", entry.DelayAfterAudio);
                dialogueRows[rowIndex] = row;
                dialogueUpdated++;
            }

            for (int rowIndex = 1; rowIndex < textRows.Count; rowIndex++)
            {
                string[] row = EnsureColumnCount(textRows[rowIndex], textRows[0].Length);
                string id = GetCell(row, textHeaderMap, "id");
                string language = GetCell(row, textHeaderMap, "language");
                if (!entryById.TryGetValue(id, out DialogueCsvEditEntry entry) ||
                    !string.Equals(language, entry.Language, StringComparison.Ordinal))
                {
                    textRows[rowIndex] = row;
                    continue;
                }

                SetCell(row, textHeaderMap, "text", entry.Text);
                textRows[rowIndex] = row;
                textUpdated++;
            }

            if (dialogueUpdated == 0 && textUpdated == 0)
            {
                message = "No matching rows were updated.";
                return false;
            }

            File.WriteAllText(
                ToAbsolutePath(dialogueAssetPath),
                CreateCsvText(dialogueRows),
                Encoding.UTF8);
            File.WriteAllText(
                ToAbsolutePath(textAssetPath),
                CreateCsvText(textRows),
                Encoding.UTF8);
            AssetDatabase.ImportAsset(dialogueAssetPath);
            AssetDatabase.ImportAsset(textAssetPath);
            AssetDatabase.Refresh();

            message =
                $"Rows updated. Dialogue: {dialogueUpdated}, Text: {textUpdated}.";
            return true;
        }

        public static bool TryDeleteEntries(
            string dialogueAssetPath,
            string textAssetPath,
            IReadOnlyList<string> ids,
            out string message)
        {
            message = string.Empty;
            if (ids == null || ids.Count == 0)
            {
                message = "No IDs were selected for deletion.";
                return false;
            }

            HashSet<string> idSet = new(
                ids.Where(id => !string.IsNullOrWhiteSpace(id))
                    .Select(id => id.Trim()),
                StringComparer.Ordinal);
            if (idSet.Count == 0)
            {
                message = "No valid IDs were selected for deletion.";
                return false;
            }

            DialogueCsvValidationResult validation = ValidateFiles(
                dialogueAssetPath,
                textAssetPath,
                false);
            if (!validation.CanSave)
            {
                message = "CSV headers are invalid.";
                return false;
            }

            if (!TryReadRows(dialogueAssetPath, out List<string[]> dialogueRows, out string error))
            {
                message = error;
                return false;
            }

            if (!TryReadRows(textAssetPath, out List<string[]> textRows, out error))
            {
                message = error;
                return false;
            }

            Dictionary<string, int> dialogueHeaderMap = BuildHeaderMap(dialogueRows[0]);
            Dictionary<string, int> textHeaderMap = BuildHeaderMap(textRows[0]);
            int dialogueDeleted = RemoveRowsById(
                dialogueRows,
                dialogueHeaderMap["id"],
                idSet);
            int textDeleted = RemoveRowsById(
                textRows,
                textHeaderMap["id"],
                idSet);

            if (dialogueDeleted == 0 && textDeleted == 0)
            {
                message = "No matching rows were deleted.";
                return false;
            }

            File.WriteAllText(
                ToAbsolutePath(dialogueAssetPath),
                CreateCsvText(dialogueRows),
                Encoding.UTF8);
            File.WriteAllText(
                ToAbsolutePath(textAssetPath),
                CreateCsvText(textRows),
                Encoding.UTF8);
            AssetDatabase.ImportAsset(dialogueAssetPath);
            AssetDatabase.ImportAsset(textAssetPath);
            AssetDatabase.Refresh();

            message =
                $"Rows deleted. Dialogue: {dialogueDeleted}, Text: {textDeleted}.";
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

        private static Dictionary<string, string> ReadTextById(
            IReadOnlyList<string[]> rows,
            IReadOnlyDictionary<string, int> headerMap,
            string language)
        {
            Dictionary<string, string> textById =
                new(StringComparer.Ordinal);
            for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                string[] row = rows[rowIndex];
                string rowLanguage = GetCell(row, headerMap, "language");
                if (!string.Equals(rowLanguage, language, StringComparison.Ordinal))
                {
                    continue;
                }

                string id = GetCell(row, headerMap, "id");
                if (!string.IsNullOrWhiteSpace(id))
                {
                    textById[id] = GetCell(row, headerMap, "text");
                }
            }

            return textById;
        }

        private static string GetCell(
            IReadOnlyList<string> row,
            IReadOnlyDictionary<string, int> headerMap,
            string header)
        {
            if (!headerMap.TryGetValue(header, out int index) ||
                index < 0 ||
                index >= row.Count)
            {
                return string.Empty;
            }

            return row[index]?.Trim() ?? string.Empty;
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

        private static string[] EnsureColumnCount(
            string[] row,
            int columnCount)
        {
            if (row.Length >= columnCount)
            {
                return row;
            }

            string[] expanded = new string[columnCount];
            Array.Copy(row, expanded, row.Length);
            return expanded;
        }

        private static int RemoveRowsById(
            List<string[]> rows,
            int idColumnIndex,
            ISet<string> ids)
        {
            int deleted = 0;
            for (int rowIndex = rows.Count - 1; rowIndex >= 1; rowIndex--)
            {
                string[] row = rows[rowIndex];
                if (idColumnIndex >= row.Length)
                {
                    continue;
                }

                string id = row[idColumnIndex]?.Trim() ?? string.Empty;
                if (!ids.Contains(id))
                {
                    continue;
                }

                rows.RemoveAt(rowIndex);
                deleted++;
            }

            return deleted;
        }

        private static string CreateCsvText(IReadOnlyList<string[]> rows)
        {
            StringBuilder builder = new();
            foreach (string[] row in rows)
            {
                builder.AppendLine(DialogueCsvParser.CreateRow(row));
            }

            return builder.ToString();
        }

        private static int CompareOrderThenId(
            string leftOrder,
            string leftId,
            string rightOrder,
            string rightId)
        {
            bool leftHasOrder = int.TryParse(leftOrder, out int left);
            bool rightHasOrder = int.TryParse(rightOrder, out int right);
            if (leftHasOrder && rightHasOrder && left != right)
            {
                return left.CompareTo(right);
            }

            if (leftHasOrder != rightHasOrder)
            {
                return leftHasOrder ? -1 : 1;
            }

            return string.Compare(leftId, rightId, StringComparison.Ordinal);
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
