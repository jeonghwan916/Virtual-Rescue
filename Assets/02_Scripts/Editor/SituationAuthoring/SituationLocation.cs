namespace VirtualRescue.EditorTools.SituationAuthoring
{
    public enum SituationLocation
    {
        Balcony,
        EntireHouse,
        Entrance,
        Kitchen,
        LivingRoom,
        Room
    }

    public static class SituationLocationPathMap
    {
        public const string SceneRoot = "Assets/01_Scenes/Situation";
        public const string ControllerRoot = "Assets/02_Scripts/10_Situations";
        public const string DefinitionRoot =
            "Assets/02_Scripts/00_Loop/Situation/SituationDefinition_SO";

        public static string GetSceneFolder(
            SituationLocation location,
            VirtualRescue.GameFlow.SituationLevel level)
        {
            return $"{SceneRoot}/{location}/{level}";
        }

        public static string GetDefaultControllerFolder(
            SituationLocation location)
        {
            return $"{ControllerRoot}/{location}";
        }
    }
}
