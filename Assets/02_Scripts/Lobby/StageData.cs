using UnityEngine;

namespace VirtualRescue.Lobby
{
    [CreateAssetMenu(fileName = "StageData", menuName = "Virtual Rescue/Lobby/Stage Data")]
    public class StageData : ScriptableObject
    {
        [Header("UI")]
        [SerializeField] private Sprite _image;
        [SerializeField] private string _primaryText;
        [SerializeField] private string _secondaryText;

        [Header("Scene")]
        [SerializeField] private string _sceneKey;
        [SerializeField] private int _sceneBuildIndex = -1;
        [SerializeField] private bool _loadMainGameAdditiveScenes = true;
        [SerializeField] private bool _disableLeftNearFarInteractor;

        public Sprite Image => _image;
        public string PrimaryText => _primaryText;
        public string SecondaryText => _secondaryText;
        public string SceneKey => _sceneKey;
        public int SceneBuildIndex => _sceneBuildIndex;
        public bool LoadMainGameAdditiveScenes => _loadMainGameAdditiveScenes;
        public bool DisableLeftNearFarInteractor => _disableLeftNearFarInteractor;
    }
}
