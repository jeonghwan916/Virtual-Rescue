using System;
using System.Collections.Generic;

namespace VirtualRescue.EditorTools.DialogueCsvAuthoring
{
    internal enum DialogueCsvAuthoringMode
    {
        Single,
        Group
    }

    internal sealed class DialogueCsvSaveRequest
    {
        public string DialogueAssetPath { get; set; }
        public string TextAssetPath { get; set; }
        public DialogueCsvAuthoringMode Mode { get; set; }
        public string Id { get; set; }
        public string Group { get; set; }
        public string Language { get; set; }
        public string Speaker { get; set; }
        public string AudioPath { get; set; }
        public string CallbackKey { get; set; }
        public string DelayAfterAudio { get; set; }
        public List<DialogueCsvLine> Lines { get; } = new();
    }

    internal readonly struct DialogueCsvLine
    {
        public DialogueCsvLine(
            string text,
            string speaker,
            string audioPath,
            string callbackKey,
            string delayAfterAudio)
        {
            Text = text;
            Speaker = speaker;
            AudioPath = audioPath;
            CallbackKey = callbackKey;
            DelayAfterAudio = delayAfterAudio;
        }

        public string Text { get; }
        public string Speaker { get; }
        public string AudioPath { get; }
        public string CallbackKey { get; }
        public string DelayAfterAudio { get; }
    }

    internal readonly struct DialogueCsvSaveResult
    {
        public DialogueCsvSaveResult(
            bool success,
            string message,
            IReadOnlyList<string> warnings,
            int dialogueRowsAdded,
            int textRowsAdded)
        {
            Success = success;
            Message = message;
            Warnings = warnings ?? Array.Empty<string>();
            DialogueRowsAdded = dialogueRowsAdded;
            TextRowsAdded = textRowsAdded;
        }

        public bool Success { get; }
        public string Message { get; }
        public IReadOnlyList<string> Warnings { get; }
        public int DialogueRowsAdded { get; }
        public int TextRowsAdded { get; }
    }

    internal readonly struct DialogueCsvValidationResult
    {
        public DialogueCsvValidationResult(
            bool canSave,
            IReadOnlyList<string> warnings)
        {
            CanSave = canSave;
            Warnings = warnings ?? Array.Empty<string>();
        }

        public bool CanSave { get; }
        public IReadOnlyList<string> Warnings { get; }
    }

    internal sealed class DialogueCsvEditEntry
    {
        public string Id { get; set; }
        public string Group { get; set; }
        public string Order { get; set; }
        public string Language { get; set; }
        public string Text { get; set; }
        public string Speaker { get; set; }
        public string AudioPath { get; set; }
        public string CallbackKey { get; set; }
        public string DelayAfterAudio { get; set; }
    }
}
