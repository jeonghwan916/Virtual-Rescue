using TMPro;
using UnityEngine;

namespace VirtualRescue.Situations.AnomalyObservation
{
    [DisallowMultipleComponent]
    public sealed class DescenderLengthObservationTarget
        : AnomalyTextureTarget
    {
        [Header("Length Display")]
        [SerializeField] private TMP_Text _lengthText;
        [SerializeField] private string _anomalyLengthText = "0m";
        [SerializeField] private string _normalLengthText = "15m";

        public override bool TryApplyAnomalyTexture()
        {
            return TryApplyLengthText(_anomalyLengthText, "anomaly");
        }

        public override bool TryApplyNormalTexture()
        {
            return TryApplyLengthText(_normalLengthText, "normal");
        }

        private void OnValidate()
        {
            if (_lengthText == null)
            {
                _lengthText = GetComponentInChildren<TMP_Text>(true);
            }

            if (string.IsNullOrWhiteSpace(_anomalyLengthText))
            {
                _anomalyLengthText = "0m";
            }

            if (string.IsNullOrWhiteSpace(_normalLengthText))
            {
                _normalLengthText = "15m";
            }
        }

        private bool TryApplyLengthText(string lengthText, string stateName)
        {
            if (_lengthText == null)
            {
                Debug.LogError(
                    $"The descender {stateName} state requires a TMP length text.",
                    this);
                return false;
            }

            _lengthText.text = lengthText;
            _lengthText.ForceMeshUpdate();
            return true;
        }
    }
}
