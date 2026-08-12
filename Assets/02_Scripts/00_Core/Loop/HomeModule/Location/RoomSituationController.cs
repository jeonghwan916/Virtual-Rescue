using UnityEngine;
using VirtualRescue.GameFlow;

namespace VirtualRescue.Locations
{
    [DisallowMultipleComponent]
    public sealed class RoomSituationController : MonoBehaviour
    {
        private RoomTrigger[] _roomTriggers;

        private void Awake()
        {
            _roomTriggers = GetComponentsInChildren<RoomTrigger>(true);
        }

        public void ResetDayState()
        {
            foreach (RoomTrigger roomTrigger in _roomTriggers)
            {
                roomTrigger.ResetDayState();
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

                roomTrigger.ConfigureSituation(dialogueId);
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
    }
}
