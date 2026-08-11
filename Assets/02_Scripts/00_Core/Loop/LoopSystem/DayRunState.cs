using System;
using System.Collections.Generic;

namespace VirtualRescue.GameFlow
{
    public sealed class DayRunState
    {
        public const int FirstDay = 1;
        public const int ClearDay = 8;

        private readonly HashSet<string> _seenSituationIds = new(StringComparer.Ordinal);

        public int CurrentDay { get; private set; } = FirstDay;
        public bool IsEndingDay => CurrentDay == ClearDay;
        public IReadOnlyCollection<string> SeenSituationIds => _seenSituationIds;

        public bool AdvanceDay()
        {
            if (CurrentDay >= ClearDay)
            {
                return false;
            }

            CurrentDay++;
            return true;
        }

        public bool TrySetCurrentDay(int day)
        {
            if (day < FirstDay || day > ClearDay)
            {
                return false;
            }

            CurrentDay = day;
            return true;
        }

        public bool HasSeenSituation(string situationId)
        {
            if (string.IsNullOrWhiteSpace(situationId))
            {
                return false;
            }

            return _seenSituationIds.Contains(situationId.Trim());
        }

        public bool TryRegisterSituation(string situationId)
        {
            if (string.IsNullOrWhiteSpace(situationId))
            {
                return false;
            }

            return _seenSituationIds.Add(situationId.Trim());
        }

        public void ResetRun()
        {
            CurrentDay = FirstDay;
            _seenSituationIds.Clear();
        }
    }
}
