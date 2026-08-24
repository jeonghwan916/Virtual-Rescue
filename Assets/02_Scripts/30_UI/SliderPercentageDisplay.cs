using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VirtualRescue.UI
{
    [RequireComponent(typeof(Slider))]
    public sealed class SliderPercentageDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text _percentageText;

        private Slider _slider;

        private void Awake()
        {
            _slider = GetComponent<Slider>();

            if (_percentageText == null)
            {
                _percentageText = GetComponentInChildren<TMP_Text>(true);
            }
        }

        private void OnEnable()
        {
            if (_slider == null || _percentageText == null)
            {
                Debug.LogWarning("Slider percentage display references are missing.", this);
                enabled = false;
                return;
            }

            _slider.onValueChanged.AddListener(UpdatePercentage);
            UpdatePercentage(_slider.value);
        }

        private void OnDisable()
        {
            if (_slider != null)
            {
                _slider.onValueChanged.RemoveListener(UpdatePercentage);
            }
        }

        private void UpdatePercentage(float value)
        {
            float normalizedValue = Mathf.InverseLerp(_slider.minValue, _slider.maxValue, value);
            _percentageText.SetText("{0:0}%", normalizedValue * 100f);
        }
    }
}
