using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VirtualRescue.Interaction
{
    public class HandleHeatDetector : MonoBehaviour
    {
        [SerializeField] private DoorHandleTemperature _handleTemperature;
        [SerializeField] private ParticleSystem _heatHazeParticle;
        [SerializeField, Range(0f, 1f)] private float _hapticAmplitude = 0.7f;
        [SerializeField, Min(0f)] private float _hapticDuration = 0.3f;

        private void OnTriggerEnter(Collider other)
        {
            XRBaseController controller = other.GetComponentInParent<XRBaseController>();

            // XR 컨트롤러가 아닌 오브젝트에 반응하지 않도록 한다.
            if (controller == null)
            {
                return;
            }

            // Inspector 참조 누락으로 잘git 못 판정되는 것을 방지한다.
            if (_handleTemperature == null)
            {
                Debug.LogWarning("손잡이 온도 컴포넌트가 연결되지 않았습니다.");
                return;
            }

            if (_handleTemperature.IsDangerous == false)
            {
                Debug.Log("안전한 온도의 문손잡이입니다.");
                return;
            }

            PlayHeatParticle();
            SendHapticFeedback(controller);
        }

        private void OnTriggerExit(Collider other)
        {
            XRBaseController controller = other.GetComponentInParent<XRBaseController>();

            // 다른 오브젝트가 나갔을 때 위험 효과가 종료되는 것을 방지한다.
            if (controller == null)
            {
                return;
            }

            StopHeatParticle();
        }

        private void PlayHeatParticle()
        {
            // 파티클이 없어도 햅틱 판정은 계속 동작하도록 한다.
            if (_heatHazeParticle == null)
            {
                return;
            }

            // 반복 진입으로 파티클이 계속 초기화되는 것을 방지한다.
            if (_heatHazeParticle.isPlaying == false)
            {
                _heatHazeParticle.Play();
            }
        }

        private void StopHeatParticle()
        {
            if (_heatHazeParticle == null)
            {
                return;
            }

            if (_heatHazeParticle.isPlaying)
            {
                _heatHazeParticle.Stop();
            }
        }

        private void SendHapticFeedback(XRBaseController controller)
        {
            controller.SendHapticImpulse(_hapticAmplitude, _hapticDuration);
        }
    }
}
