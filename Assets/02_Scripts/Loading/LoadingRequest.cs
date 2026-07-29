namespace VirtualRescue.Loading
{
    public static class LoadingRequest
    {
        public static string MainSceneKey { get; private set; }
        public static int MainSceneBuildIndex { get; private set; } = -1;
        public static string[] AdditiveSceneKeys { get; private set; } = new string[0];

        public static bool HasValidMainScene =>
            !string.IsNullOrWhiteSpace(MainSceneKey) || MainSceneBuildIndex >= 0;

        public static void Set(
            string mainSceneKey,
            int mainSceneBuildIndex,
            string[] additiveSceneKeys)
        {
            MainSceneKey = mainSceneKey;
            MainSceneBuildIndex = mainSceneBuildIndex;
            AdditiveSceneKeys = additiveSceneKeys ?? new string[0];
        }

        public static void Clear()
        {
            MainSceneKey = null;
            MainSceneBuildIndex = -1;
            AdditiveSceneKeys = new string[0];
        }
    }
}
