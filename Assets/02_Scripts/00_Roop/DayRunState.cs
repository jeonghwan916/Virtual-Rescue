namespace VirtualRescue.GameFlow
{
    public sealed class DayRunState
    {
        public const int FirstDay = 1;
        public const int ClearDay = 8;

        public int CurrentDay { get; private set; } = FirstDay;
        public bool IsGameCleared => CurrentDay >= ClearDay;

        public bool AdvanceDay()
        {
            if (IsGameCleared)
            {
                return false;
            }

            CurrentDay++;
            return true;
        }

        public void ResetRun()
        {
            CurrentDay = FirstDay;
        }
    }
}
