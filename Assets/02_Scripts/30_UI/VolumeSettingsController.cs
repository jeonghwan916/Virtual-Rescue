using UnityEngine;
using UnityEngine.UI;

namespace VirtualRescue.UI
{
    public sealed class VolumeSettingsController : MonoBehaviour
    {
        private const string VolumePlayerPrefsKey = "VirtualRescue.MasterVolume";

        [SerializeField] private Slider _volumeSlider;
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _cancelButton;

        private MenuFollowerController _menuController;
        private float _savedVolume;
        private bool _hasLoadedSavedVolume;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplySavedVolumeOnStartup()
        {
            if (PlayerPrefs.HasKey(VolumePlayerPrefsKey))
            {
                ApplyVolume(PlayerPrefs.GetFloat(VolumePlayerPrefsKey));
            }
        }

        private void OnEnable()
        {
            if (_menuController == null)
            {
                _menuController = GetComponentInParent<MenuFollowerController>(true);
            }

            if (!HasRequiredReferences())
            {
                Debug.LogWarning("Volume setting UI references are missing.", this);
                enabled = false;
                return;
            }

            _volumeSlider.onValueChanged.AddListener(PreviewVolume);
            _saveButton.onClick.AddListener(SaveVolume);
            _cancelButton.onClick.AddListener(CancelVolume);

            if (!_hasLoadedSavedVolume)
            {
                LoadSavedVolume();
                _hasLoadedSavedVolume = true;
            }
            else
            {
                ApplySavedVolumeToSlider();
            }
        }

        private void OnDisable()
        {
            if (_volumeSlider != null)
            {
                _volumeSlider.onValueChanged.RemoveListener(PreviewVolume);
            }

            if (_saveButton != null)
            {
                _saveButton.onClick.RemoveListener(SaveVolume);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveListener(CancelVolume);
            }
        }

        private bool HasRequiredReferences()
        {
            return _volumeSlider != null && _saveButton != null && _cancelButton != null;
        }

        private void LoadSavedVolume()
        {
            _savedVolume = PlayerPrefs.HasKey(VolumePlayerPrefsKey)
                ? Mathf.Clamp01(PlayerPrefs.GetFloat(VolumePlayerPrefsKey))
                : _volumeSlider.normalizedValue;

            ApplySavedVolumeToSlider();
        }

        private void PreviewVolume(float value)
        {
            float normalizedVolume = Mathf.InverseLerp(
                _volumeSlider.minValue,
                _volumeSlider.maxValue,
                value);

            ApplyVolume(normalizedVolume);
        }

        private void SaveVolume()
        {
            _savedVolume = Mathf.Clamp01(_volumeSlider.normalizedValue);
            PlayerPrefs.SetFloat(VolumePlayerPrefsKey, _savedVolume);
            PlayerPrefs.Save();

            ApplyVolume(_savedVolume);

            if (_menuController != null)
            {
                _menuController.Close();
                return;
            }

            ClosePanel();
        }

        private void CancelVolume()
        {
            ApplySavedVolumeToSlider();

            if (_menuController != null)
            {
                _menuController.CloseSetting();
                return;
            }

            ClosePanel();
        }

        private void ClosePanel()
        {
            gameObject.SetActive(false);
        }

        private void ApplySavedVolumeToSlider()
        {
            _volumeSlider.value = Mathf.Lerp(
                _volumeSlider.minValue,
                _volumeSlider.maxValue,
                _savedVolume);

            ApplyVolume(_savedVolume);
        }

        private static void ApplyVolume(float volume)
        {
            AudioListener.volume = Mathf.Clamp01(volume);
        }
    }
}
