using UnityEngine;
using VirtualRescue.GameFlow;

namespace VirtualRescue.Locations
{
    [DisallowMultipleComponent]
    public sealed class RoomSituationController : MonoBehaviour
    {
        private RoomTrigger[] _roomTriggers;
        private bool _roomDialoguesSuppressed;

        [SerializeField] private SituationDiscoveryTracker _discoveryTracker;

        private void Awake()
        {
            _roomTriggers = GetComponentsInChildren<RoomTrigger>(true);

            foreach (RoomTrigger roomTrigger in _roomTriggers)
            {
                roomTrigger.SituationEntryDialoguePlayed +=
                    HandleSituationEntryDialoguePlayed;
            }
        }

        private void OnDestroy()
        {
            if (_roomTriggers == null)
            {
                return;
            }

            foreach (RoomTrigger roomTrigger in _roomTriggers)
            {
                roomTrigger.SituationEntryDialoguePlayed -=
                    HandleSituationEntryDialoguePlayed;
            }
        }

        public void ResetDayState()
        {
            _roomDialoguesSuppressed = false;

            foreach (RoomTrigger roomTrigger in _roomTriggers)
            {
                roomTrigger.ResetDayState();
            }
        }

        public void SuppressEntryDialogues()
        {
            if (_roomDialoguesSuppressed)
            {
                return;
            }

            _roomDialoguesSuppressed = true;

            foreach (RoomTrigger roomTrigger in _roomTriggers)
            {
                roomTrigger.SuppressEntryDialogue();
            }
        }

        public void Configure(SituationDefinition definition)
        {
            if (definition == null ||
                definition.RoomLocation == RoomLocation.None)
            {
                return;
            }

            string dialogueId = GetSituationDialogueId(definition.Level);
            bool found = false;

            foreach (RoomTrigger roomTrigger in _roomTriggers)
            {
                if (roomTrigger.Location != definition.RoomLocation)
                {
                    continue;
                }

                roomTrigger.ConfigureSituation(dialogueId, definition.Level);
                found = true;
            }

            if (!found)
            {
                Debug.LogWarning(
                    $"{name}: {definition.RoomLocation}에 해당하는 RoomTrigger를 찾을 수 없습니다.",
                    this);
            }
        }

        private static string GetSituationDialogueId(SituationLevel level)
        {
            return level switch
            {
                SituationLevel.Level0 => "Situation0",
                SituationLevel.Level1 => "Situation1",
                SituationLevel.Level2 => "Situation2",
                _ => string.Empty
            };
        }

        private void HandleSituationEntryDialoguePlayed(
            RoomTrigger source,
            SituationLevel situationLevel)
        {
            if (_roomDialoguesSuppressed ||
                !ShouldSuppressAfterSituationEntry(situationLevel))
            {
                return;
            }

            _discoveryTracker?.MarkDiscovered(situationLevel);
            SuppressEntryDialogues();
        }

        private static bool ShouldSuppressAfterSituationEntry(
            SituationLevel situationLevel)
        {
            return situationLevel == SituationLevel.Level1 ||
                   situationLevel == SituationLevel.Level2;
        }
    }
}
