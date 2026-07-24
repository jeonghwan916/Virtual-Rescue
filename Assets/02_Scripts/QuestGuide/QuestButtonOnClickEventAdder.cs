using System;
using UnityEngine;
using VirtualRescue.DialogueSystem;

namespace VirtualRescue.QuestGuide
{
    [DisallowMultipleComponent]
    public class QuestButtonOnClickEventAdder : MonoBehaviour
    {
        [SerializeField] private DialogueManager _dialogueManager;

        private void Awake()
        {
            //ResolveDialogueManager();
            SetDialogueManager(_dialogueManager);
        }

        public void SetDialogueManager(DialogueManager dialogueManager)
        {
            _dialogueManager = dialogueManager;
        }

        public void HandleGuideAction(string actionId)
        {
            if (string.IsNullOrWhiteSpace(actionId))
            {
                return;
            }

            const string dialoguePrefix = "dialogue:";
            const string dialogueGroupPrefix = "dialogue-group:";

            if (actionId.StartsWith(dialoguePrefix, StringComparison.Ordinal))
            {
                string dialogueId = actionId.Substring(dialoguePrefix.Length);
                if (ResolveDialogueManager())
                {
                    _dialogueManager.Play(dialogueId);
                }
                return;
            }

            if (actionId.StartsWith(dialogueGroupPrefix, StringComparison.Ordinal))
            {
                string groupId = actionId.Substring(dialogueGroupPrefix.Length);
                if (ResolveDialogueManager())
                {
                    _dialogueManager.PlayGroup(groupId);
                }
                return;
            }

            switch (actionId)
            {
                case "open_door":
                    OpenDoor();
                    break;

                case "complete":
                    CompleteQuest();
                    break;

                case "give_extinguisher":
                    GiveExtinguisher();
                    break;

                default:
                    Debug.LogWarning($"Unhandled guide Action ID: {actionId}", this);
                    break;
            }
        }

        private bool ResolveDialogueManager()
        {
            if (_dialogueManager != null)
            {
                return true;
            }

            _dialogueManager = FindFirstObjectByType<DialogueManager>();
            if (_dialogueManager == null)
            {
                Debug.LogWarning("DialogueManager was not found for quest guide action.", this);
                return false;
            }

            return true;
        }

        private void OpenDoor()
        {
            Debug.Log("OpenDoor");
        }

        private void CompleteQuest()
        {
            Debug.Log("CompleteQuest");
        }

        private void GiveExtinguisher()
        {
            Debug.Log("GiveExtinguisher");
        }
    }
}
