using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualRescue.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class SituationSelector : MonoBehaviour
    {
        [SerializeField] private List<SituationDefinition> _candidates = new();

        [Range(0f, 1f)]
        [SerializeField] private float _noSituationChance = 0.2f;

        public IReadOnlyList<SituationDefinition> Candidates => _candidates;
        public float NoSituationChance => _noSituationChance;
        public string LastError { get; private set; } = string.Empty;

        public bool TrySelect(
            int currentDay,
            IReadOnlyCollection<string> seenSituationIds,
            out SituationDefinition selectedDefinition)
        {
            selectedDefinition = null;
            LastError = string.Empty;

            if (currentDay < DayRunState.FirstDay ||
                currentDay >= DayRunState.ClearDay)
            {
                return Fail($"Situation selection is not allowed on day {currentDay}.");
            }

            if (_candidates == null || _candidates.Count == 0)
            {
                return Fail("No situation definitions are configured.");
            }

            HashSet<string> seenIds = CreateNormalizedIdSet(seenSituationIds);
            HashSet<string> configuredIds = new(StringComparer.Ordinal);
            List<SituationDefinition> eligibleCandidates = new();

            foreach (SituationDefinition candidate in _candidates)
            {
                if (candidate == null)
                {
                    return Fail("Situation candidates contain a missing definition.");
                }

                string candidateId = candidate.Id?.Trim();
                if (string.IsNullOrEmpty(candidateId))
                {
                    return Fail($"Situation definition '{candidate.name}' has no valid ID.");
                }

                if (!configuredIds.Add(candidateId))
                {
                    return Fail($"Duplicate situation ID is configured: {candidateId}");
                }

                if (candidate.Weight <= 0)
                {
                    return Fail(
                        $"Situation '{candidateId}' must have a positive selection weight.");
                }

                if (candidate.MinimumDay > currentDay || seenIds.Contains(candidateId))
                {
                    continue;
                }

                eligibleCandidates.Add(candidate);
            }

            if (eligibleCandidates.Count == 0 ||
                UnityEngine.Random.value < _noSituationChance)
            {
                return true;
            }

            selectedDefinition = SelectByWeight(eligibleCandidates);
            return true;
        }

        private void OnValidate()
        {
            _noSituationChance = Mathf.Clamp01(_noSituationChance);
        }

        private static HashSet<string> CreateNormalizedIdSet(
            IReadOnlyCollection<string> situationIds)
        {
            HashSet<string> normalizedIds = new(StringComparer.Ordinal);

            if (situationIds == null)
            {
                return normalizedIds;
            }

            foreach (string situationId in situationIds)
            {
                if (!string.IsNullOrWhiteSpace(situationId))
                {
                    normalizedIds.Add(situationId.Trim());
                }
            }

            return normalizedIds;
        }

        private static SituationDefinition SelectByWeight(
            IReadOnlyList<SituationDefinition> candidates)
        {
            double totalWeight = 0d;

            foreach (SituationDefinition candidate in candidates)
            {
                totalWeight += candidate.Weight;
            }

            double randomWeight = UnityEngine.Random.value * totalWeight;

            foreach (SituationDefinition candidate in candidates)
            {
                randomWeight -= candidate.Weight;

                if (randomWeight <= 0d)
                {
                    return candidate;
                }
            }

            return candidates[candidates.Count - 1];
        }

        private bool Fail(string message)
        {
            LastError = message;
            Debug.LogError(message, this);
            return false;
        }
    }
}
