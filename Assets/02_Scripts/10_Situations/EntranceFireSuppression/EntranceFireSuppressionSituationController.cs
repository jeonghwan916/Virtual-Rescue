using System.Collections.Generic;
using UnityEngine;
using VirtualRescue.Situations.FireSuppression;

namespace VirtualRescue.Situations.EntranceFireSuppression
{
    [DisallowMultipleComponent]
    public sealed class EntranceFireSuppressionSituationController :
        FireSuppressionSituationController
    {
        private const int MaximumFireCount = 3;

        [Header("Fire Candidates")]
        [SerializeField] private List<FireObject> _fireCandidates = new();
        [Range(1, MaximumFireCount)]
        [SerializeField] private int _activeFireCount = MaximumFireCount;

        public int ActiveFireCount => _activeFireCount;

        protected override void PrepareActiveFireObjects(
            List<FireObject> activeFireObjects)
        {
            int availableFireCount = 0;

            foreach (FireObject fireObject in _fireCandidates)
            {
                if (fireObject != null)
                {
                    availableFireCount++;
                }
            }

            int activeCount = Mathf.Min(
                _activeFireCount,
                Mathf.Min(availableFireCount, MaximumFireCount));

            for (int index = 0; index < _fireCandidates.Count; index++)
            {
                FireObject fireObject = _fireCandidates[index];

                if (fireObject == null)
                {
                    continue;
                }

                bool shouldBeActive = activeFireObjects.Count < activeCount;
                fireObject.gameObject.SetActive(shouldBeActive);

                if (shouldBeActive)
                {
                    activeFireObjects.Add(fireObject);
                }
            }
        }

        private void OnValidate()
        {
            _activeFireCount = Mathf.Clamp(_activeFireCount, 1, MaximumFireCount);
        }
    }
}
