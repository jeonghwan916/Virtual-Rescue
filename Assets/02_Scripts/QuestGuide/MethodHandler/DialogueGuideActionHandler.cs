using System;
using UnityEngine;
using VirtualRescue.DialogueSystem;

namespace VirtualRescue.QuestGuide
{
    [DisallowMultipleComponent]
    public sealed class DialogueGuideActionHandler : MonoBehaviour, IQuestGuideActionHandler
    {
        private const string DialoguePrefix = "dialogue:";
        private const string DialogueGroupPrefix = "dialogue-group:";

        [SerializeField] private DialogueManager _dialogueManager;

        public bool CanHandle(string actionId)
        {
            return IsDialogueAction(actionId) || IsDialogueGroupAction(actionId);
        }

        public void Handle(string actionId)
        {
            if (_dialogueManager == null)
            {
                Debug.LogWarning("DialogueManager is not assigned for quest guide dialogue action.", this);
                return;
            }

            if (IsDialogueAction(actionId))
            {
                PlayDialogue(actionId);
                return;
            }

            if (IsDialogueGroupAction(actionId))
            {
                PlayDialogueGroup(actionId);
            }
        }

        private static bool IsDialogueAction(string actionId)
        {
            return !string.IsNullOrWhiteSpace(actionId) &&
                   actionId.StartsWith(DialoguePrefix, StringComparison.Ordinal);
        }

        private static bool IsDialogueGroupAction(string actionId)
        {
            return !string.IsNullOrWhiteSpace(actionId) &&
                   actionId.StartsWith(DialogueGroupPrefix, StringComparison.Ordinal);
        }

        private void PlayDialogue(string actionId)
        {
            string dialogueId = actionId.Substring(DialoguePrefix.Length);
            _dialogueManager.Play(dialogueId);
        }

        private void PlayDialogueGroup(string actionId)
        {
            string groupId = actionId.Substring(DialogueGroupPrefix.Length);
            _dialogueManager.PlayGroup(groupId);
        }
    }
}
