namespace VirtualRescue.GameFlow
{
    public readonly struct DayResultContext
    {
        public DayResultContext(
            DayResultType resultType,
            SituationDefinition situationDefinition)
            : this(
                resultType,
                situationDefinition,
                DayFailureReason.None,
                ExitType.Elevator,
                false)
        {
        }

        private DayResultContext(
            DayResultType resultType,
            SituationDefinition situationDefinition,
            DayFailureReason failureReason,
            ExitType requestedExitType,
            bool hasRequestedExitType)
        {
            ResultType = resultType;
            SituationDefinition = situationDefinition;
            SituationId = situationDefinition != null
                ? situationDefinition.Id
                : string.Empty;
            FailureReason = failureReason;
            RequestedExitType = requestedExitType;
            HasRequestedExitType = hasRequestedExitType;
        }

        public DayResultType ResultType { get; }
        public string SituationId { get; }
        public SituationDefinition SituationDefinition { get; }
        public DayFailureReason FailureReason { get; }
        public ExitType RequestedExitType { get; }
        public bool HasRequestedExitType { get; }

        public static DayResultContext None =>
            new(DayResultType.None, null);

        public static DayResultContext Completed(
            SituationDefinition situationDefinition) =>
            new(DayResultType.Completed, situationDefinition);

        public static DayResultContext Failed(
            SituationDefinition situationDefinition) =>
            new(DayResultType.Failed, situationDefinition);

        public static DayResultContext Failed(
            SituationDefinition situationDefinition,
            DayFailureReason failureReason,
            ExitType requestedExitType) =>
            new(
                DayResultType.Failed,
                situationDefinition,
                failureReason,
                requestedExitType,
                true);
    }
}
