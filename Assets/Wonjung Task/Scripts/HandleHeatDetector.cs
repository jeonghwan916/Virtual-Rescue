using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

namespace VirtualRescue.Interaction
{
    public class HandleHeatDetector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DoorHandleTemperature _handleTemperature;
        [SerializeField] private Renderer _handleRenderer;

        [Header("Color Feedback")]
        [SerializeField] private Color _dangerColor = new Color(1f, 0.3f, 0f);

        [Header("Haptic Feedback")]
        [SerializeField, Range(0f, 1f)] private float _hapticAmplitude = 0.7f;
        [SerializeField, Min(0f)] private float _hapticDuration = 0.3f;

        private Material _handleMaterial;
        private Color _originalColor;

        private void Awake()
        {
            InitializeHandleMaterial();
        }

        private void OnTriggerEnter(Collider other)
        {
            HapticImpulsePlayer hapticPlayer =
                other.GetComponentInParent<HapticImpulsePlayer>();

            // XR 햅틱 장치가 없는 오브젝트에는 반응하지 않도록 한다.
            if (hapticPlayer == null)
            {
                return;
            }

            // 온도 참조가 없으면 위험 여부를 판단할 수 없다.
            if (_handleTemperature == null)
            {
                Debug.LogWarning(
                    $"{gameObject.name}: 손잡이 온도 컴포넌트가 연결되지 않았습니다."
                );

                return;
            }

            if (_handleTemperature.IsDangerous == false)
            {
                Debug.Log($"{gameObject.name}: 안전한 온도의 문손잡이입니다.");
                return;
            }

            ShowDangerColor();
            SendHapticFeedback(hapticPlayer);
        }

        private void OnTriggerExit(Collider other)
        {
            HapticImpulsePlayer hapticPlayer =
                other.GetComponentInParent<HapticImpulsePlayer>();

            // 다른 오브젝트가 나갔을 때 색상이 복원되는 것을 방지한다.
            if (hapticPlayer == null)
            {
                return;
            }

            RestoreOriginalColor();
        }

        private void InitializeHandleMaterial()
        {
            // 색상 변경 대상이 없으면 색상 기능만 건너뛰도록 한다.
            if (_handleRenderer == null)
            {
                Debug.LogWarning(
                    $"{gameObject.name}: 손잡이 Renderer가 연결되지 않았습니다."
                );

                return;
            }

            // 에셋의 공유 Material이 직접 변경되는 것을 방지한다.
            _handleMaterial = _handleRenderer.material;
            _originalColor = _handleMaterial.color;
        }

        private void ShowDangerColor()
        {
            if (_handleMaterial == null)
            {
                return;
            }

            _handleMaterial.color = _dangerColor;
        }

        private void RestoreOriginalColor()
        {
            if (_handleMaterial == null)
            {
                return;
            }

            _handleMaterial.color = _originalColor;
        }

        private void SendHapticFeedback(HapticImpulsePlayer hapticPlayer)
        {
            hapticPlayer.SendHapticImpulse(
                _hapticAmplitude,
                _hapticDuration
            );
        }
    }
}