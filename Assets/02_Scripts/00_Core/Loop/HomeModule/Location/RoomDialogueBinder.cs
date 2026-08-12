using UnityEngine;
using VirtualRescue.DialogueSystem;
using VirtualRescue.Player;

namespace VirtualRescue.Locations
{
    [DisallowMultipleComponent]
    public sealed class RoomDialogueBinder : MonoBehaviour
    {
        [SerializeField] private DialogueManager _dialogueManager;

        private void Start()
        {
            PersistentPlayerRoot playerRoot = PersistentPlayerRoot.Instance;
            if (playerRoot == null)
            {
                Debug.LogWarning(
                    $"{name}: PersistentPlayerRoot를 찾을 수 없습니다.",
                    this);
                return;
            }

            RoomVisitTracker visitTracker =
                playerRoot.GetComponent<RoomVisitTracker>();
            if (visitTracker == null)
            {
                Debug.LogWarning(
                    $"{name}: RoomVisitTracker를 찾을 수 없습니다.",
                    this);
                return;
            }

            if (_dialogueManager == null)
            {
                Debug.LogWarning(
                    $"{name}: DialogueManager가 연결되지 않았습니다.",
                    this);
                return;
            }

            visitTracker.BindDialogueManager(_dialogueManager);
        }
    }
}
