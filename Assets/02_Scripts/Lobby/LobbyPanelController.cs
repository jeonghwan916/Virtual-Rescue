using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VirtualRescue.Lobby
{
    public class LobbyPanelController : MonoBehaviour
    {
        [Header("Panels")]
        [Tooltip("Panels used in the lobby UI.")]
        [SerializeField] private GameObject _mainPanel;
        [SerializeField] private GameObject _stageSelectPanel;
        [SerializeField] private GameObject _stagePanel;
        [SerializeField] private GameObject _settingPanel;

        [Header("Main Buttons")]
        [Tooltip("Buttons in the main panel.")]
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _settingButton;
        [SerializeField] private Button _exitButton;

        [Header("Stage Select Buttons")]
        [Tooltip("Buttons used to select a stage.")]
        [SerializeField] private Button[] _stageSelectButtons;
        [SerializeField] private StageData[] _stageData;

        [Header("Stage Panel UI")]
        [SerializeField] private Image _stageImage;
        [SerializeField] private TMP_Text _stagePrimaryText;
        [SerializeField] private TMP_Text _stageSecondaryText;
        [SerializeField] private SceneController _sceneController;

        [Header("Back Or Progress Buttons")]
        [Tooltip("Buttons used to go back or progress to the selected scene.")]
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _progressButton;
        
        [Header("Return to Main Panel Button")]
        [SerializeField] private Button _returnToMainPanelButton;


        private void Awake()
        {
            RegisterStageSelectButtons();
            RegisterProgressButton();
            
            _startButton.onClick.AddListener(() => SwitchPanel(_mainPanel, _stageSelectPanel));
            for (int i = 0; i < _stageSelectButtons.Length; i++)
            {
                _stageSelectButtons[i].onClick.AddListener(() => SwitchPanel(_stageSelectPanel, _stagePanel));
            }
            // todo : 나중에 세팅 버튼 리스너도 따로 넣기
            // todo : 나중에 종료 버튼 리스너도 따로 넣기
            
            _backButton.onClick.AddListener(() => SwitchPanel(_stagePanel, _stageSelectPanel));
            _returnToMainPanelButton.onClick.AddListener(() => SwitchPanel(_stageSelectPanel, _mainPanel));
        }

        private void RegisterStageSelectButtons()
        {
            int buttonCount = _stageSelectButtons == null ? 0 : _stageSelectButtons.Length;
            int dataCount = _stageData == null ? 0 : _stageData.Length;
            int count = Mathf.Min(buttonCount, dataCount);

            if (buttonCount != dataCount)
            {
                Debug.LogWarning($"Stage button count({buttonCount}) and StageData count({dataCount}) do not match.");
            }

            for (int i = 0; i < count; i++)
            {
                int index = i;
                Button button = _stageSelectButtons[index];

                if (button == null)
                {
                    Debug.LogWarning($"Stage button is missing. Index: {index}");
                    continue;
                }

                button.onClick.AddListener(() => ApplyStageData(_stageData[index]));
            }
        }

        private void RegisterProgressButton()
        {
            if (_progressButton == null)
            {
                Debug.LogWarning("Progress button is not assigned.");
                return;
            }

            if (_sceneController == null)
            {
                Debug.LogWarning("SceneController is not assigned.");
                return;
            }

            _progressButton.onClick.AddListener(_sceneController.LoadSelectedScene);
        }

        private void ApplyStageData(StageData data)
        {
            if (data == null)
            {
                Debug.LogWarning("StageData is missing.");
                return;
            }

            if (_stageImage != null)
            {
                _stageImage.sprite = data.Image;
            }

            if (_stagePrimaryText != null)
            {
                _stagePrimaryText.text = data.PrimaryText;
            }

            if (_stageSecondaryText != null)
            {
                _stageSecondaryText.text = data.SecondaryText;
            }

            if (_sceneController != null)
            {
                _sceneController.SetSelectedScene(data.SceneKey, data.SceneBuildIndex, data.LoadMainGameAdditiveScenes);
            }

            if (_stagePanel != null)
            {
                _stagePanel.SetActive(true);
            }
        }

        public void SwitchPanel(GameObject currentPanel, GameObject nextPanel)
        {
            currentPanel.SetActive(false);
            nextPanel.SetActive(true);
        }
    }
}
