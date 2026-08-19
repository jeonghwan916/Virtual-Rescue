using UnityEngine;

namespace VirtualRescue.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class SituationDiscoveryTracker : MonoBehaviour
    {
        private bool _hasDiscoveredCurrentSituation;

        public bool HasDiscoveredCurrentSituation =>
            _hasDiscoveredCurrentSituation;

        public void MarkDiscovered(SituationLevel situationLevel)
        {
            if (situationLevel == SituationLevel.Level1 ||
                situationLevel == SituationLevel.Level2)
            {
                _hasDiscoveredCurrentSituation = true;
            }
        }

        public void ResetCurrentSituation()
        {
            _hasDiscoveredCurrentSituation = false;
        }
    }
}
