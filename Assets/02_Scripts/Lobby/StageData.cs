using UnityEngine;

namespace VirtualRescue.Lobby
{
    [CreateAssetMenu(fileName = "StageData", menuName = "Virtual Rescue/Lobby/Stage Data")]
    public class StageData : ScriptableObject
    {
        [Header("Button")]
        [SerializeField] private int _buttonNumber;

        [Header("UI")]
        [SerializeField] private Sprite _image;
        [SerializeField] private string _primaryText;
        [SerializeField] private string _secondaryText;

        [Header("Scene")]
        [SerializeField] private string _sceneKey;

        public int ButtonNumber => _buttonNumber;
        public Sprite Image => _image;
        public string PrimaryText => _primaryText;
        public string SecondaryText => _secondaryText;
        public string SceneKey => _sceneKey;
    }
}
