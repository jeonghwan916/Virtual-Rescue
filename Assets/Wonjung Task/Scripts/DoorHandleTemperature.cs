using UnityEngine;

namespace VirtualRescue
{
    public class DoorHandleTemperature : MonoBehaviour
    {
        [SerializeField] private float _temperature = 25f;
        [SerializeField] private float _dangerTemperature = 50f;
        [SerializeField] private ParticleSystem _heatHazeParticle;

        public bool IsDangerous
        {
            get
            {
                return _temperature >= _dangerTemperature;
            }
        }

        public void ShowDangerEffect()
        {
            // 안전한 손잡이에서는 위험 효과가 실행되지 않도록 차단한다.
            if (IsDangerous == false)
            {
                return;
            }

            // Inspector 연결 누락으로 인한 NullReferenceException을 방지한다.
            if (_heatHazeParticle == null)
            {
                return;
            }

            // 반복 호출될 때 파티클이 계속 처음부터 재생되는 것을 방지한다.
            if (_heatHazeParticle.isPlaying == false)
            {
                _heatHazeParticle.Play();
            }
        }

        public void StopDangerEffect()
        {
            // 파티클이 연결되지 않은 상태에서 Stop()이 호출되는 것을 방지한다.
            if (_heatHazeParticle == null)
            {
                return;
            }

            _heatHazeParticle.Stop();
        }
    }
}
