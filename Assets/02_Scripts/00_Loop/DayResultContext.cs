namespace VirtualRescue.GameFlow
{
    public readonly struct DayResultContext
    {
        public DayResultContext(
            DayResultType resultType,
            SituationDefinition situationDefinition)
        {
            ResultType = resultType;
            SituationDefinition = situationDefinition;
            SituationId = situationDefinition != null
                ? situationDefinition.Id
                : string.Empty;
        }

        public DayResultType ResultType { get; }
        public string SituationId { get; }
        public SituationDefinition SituationDefinition { get; }

        public static DayResultContext None =>
            new(DayResultType.None, null);

        public static DayResultContext Completed(
            SituationDefinition situationDefinition) =>
            new(DayResultType.Completed, situationDefinition);

        public static DayResultContext Failed(
            SituationDefinition situationDefinition) =>
            new(DayResultType.Failed, situationDefinition);
    }
}
