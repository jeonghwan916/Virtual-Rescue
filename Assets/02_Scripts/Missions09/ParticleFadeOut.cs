using System.Collections;
using UnityEngine;

namespace VirtualRescue.Effects
{
    public sealed class ParticleFadeOut : MonoBehaviour
    {
        [Tooltip("약해질 대상 파티클입니다. 비워두면 같은 오브젝트의 ParticleSystem을 사용합니다.")]
        [SerializeField] private ParticleSystem _particle;

        [Tooltip("파티클 방출량과 생존 시간이 0까지 줄어드는 시간입니다.")]
        [SerializeField] private float _fadeDuration = 3f;

        [Tooltip("파티클이 줄어드는 속도 곡선입니다. X는 시간, Y는 감소 진행도입니다.")]
        [SerializeField] private AnimationCurve _fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("완전히 사라진 뒤 파티클 오브젝트를 비활성화합니다.")]
        [SerializeField] private bool _disableParticleObjectOnComplete = true;

        private ParticleSystem.MinMaxCurve _initialStartLifetime;
        private ParticleSystem.MinMaxCurve _initialEmissionRate;
        private Coroutine _fadeRoutine;
        private bool _hasInitialParticleValues;

        private void Awake()
        {
            if (_particle == null)
            {
                _particle = GetComponent<ParticleSystem>();
            }

            CacheInitialParticleValues();
        }

        private void OnDisable()
        {
            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }
        }

        public void FadeOut()
        {
            if (_particle == null)
            {
                Debug.LogWarning("ParticleFadeOut에 ParticleSystem이 연결되어 있지 않습니다.", this);
                return;
            }

            if (!_hasInitialParticleValues)
            {
                CacheInitialParticleValues();
            }

            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
            }

            _fadeRoutine = StartCoroutine(FadeOutRoutine());
        }

        public void StopImmediately()
        {
            if (_particle == null)
            {
                Debug.LogWarning("ParticleFadeOut에 ParticleSystem이 연결되어 있지 않습니다.", this);
                return;
            }

            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }

            _particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (_disableParticleObjectOnComplete)
            {
                _particle.gameObject.SetActive(false);
            }
        }

        private void CacheInitialParticleValues()
        {
            if (_particle == null)
            {
                return;
            }

            ParticleSystem.MainModule main = _particle.main;
            ParticleSystem.EmissionModule emission = _particle.emission;

            _initialStartLifetime = main.startLifetime;
            _initialEmissionRate = emission.rateOverTime;
            _hasInitialParticleValues = true;
        }

        private IEnumerator FadeOutRoutine()
        {
            float duration = Mathf.Max(0.01f, _fadeDuration);
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;

                float normalizedTime = Mathf.Clamp01(elapsedTime / duration);
                float fadeAmount = Mathf.Clamp01(_fadeCurve.Evaluate(normalizedTime));
                float remainingScale = 1f - fadeAmount;

                ApplyParticleScale(remainingScale);

                yield return null;
            }

            ApplyParticleScale(0f);
            _particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (_disableParticleObjectOnComplete)
            {
                _particle.gameObject.SetActive(false);
            }

            _fadeRoutine = null;
        }

        private void ApplyParticleScale(float scale)
        {
            if (_particle == null)
            {
                return;
            }

            ParticleSystem.MainModule main = _particle.main;
            ParticleSystem.EmissionModule emission = _particle.emission;

            main.startLifetime = ScaleMinMaxCurve(_initialStartLifetime, scale);
            emission.rateOverTime = ScaleMinMaxCurve(_initialEmissionRate, scale);
        }

        private static ParticleSystem.MinMaxCurve ScaleMinMaxCurve(ParticleSystem.MinMaxCurve curve, float scale)
        {
            switch (curve.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    return new ParticleSystem.MinMaxCurve(curve.constant * scale);

                case ParticleSystemCurveMode.TwoConstants:
                    return new ParticleSystem.MinMaxCurve(curve.constantMin * scale, curve.constantMax * scale);

                case ParticleSystemCurveMode.Curve:
                    return new ParticleSystem.MinMaxCurve(curve.curveMultiplier * scale, curve.curve);

                case ParticleSystemCurveMode.TwoCurves:
                    return new ParticleSystem.MinMaxCurve(curve.curveMultiplier * scale, curve.curveMin, curve.curveMax);

                default:
                    return curve;
            }
        }
    }
}
