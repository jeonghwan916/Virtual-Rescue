using System;
using System.Collections.Generic;
using UnityEngine;
using VirtualRescue.DialogueSystem;

namespace VirtualRescue.Locations
{
    [DisallowMultipleComponent]
    public sealed class RoomVisitTracker : MonoBehaviour
    {
        [SerializeField] private RoomLocation _currentRoomLocation;
        [SerializeField] private DialogueManager _dialogue;

        public void Enter(RoomLocation roomLocation)
        {
            _currentRoomLocation = roomLocation;
        }

        public void Leave()
        {
            _currentRoomLocation = RoomLocation.None;
        }

        public void BindDialogueManager(DialogueManager dialogueManager)
        {
            _dialogue = dialogueManager;
        }

        public void DisplayDialogue(string locationID)
        {
            if (_dialogue == null)
            {
                Debug.LogWarning(
                    $"{name}: DialogueManager가 연결되지 않았습니다.",
                    this);
                return;
            }

            _dialogue.Play(locationID);
        }
    }
}
