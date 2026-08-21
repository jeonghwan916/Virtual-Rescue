using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;

public class FireObject : MonoBehaviour
{
    // 랜덤으로 사용할 이징 목록 미리 정의
    private readonly Ease[] randomEases = new Ease[]
    {
        Ease.OutQuad,
        Ease.OutBounce,
        Ease.OutBack,
        Ease.OutElastic
    };

    private struct ParticleInitialState
    {
        public ParticleSystem Particle;
        public ParticleSystem.MinMaxCurve RateOverTime;
        public ParticleSystem.MinMaxCurve StartSize;
        public ParticleSystem.MinMaxCurve StartLifetime;
    }

    [Header("Particle System")]
    [SerializeField] private ParticleSystem[] _fireParticles;

    [Header("Extinguish : Stages")]
    [SerializeField] private float _extinguishDuration = 4f;
    [SerializeField] private int _extinguishStageCount = 4;
    [SerializeField] private bool _disableWhenExtinguished = true;

    [Header("Temporary Suppression")]
    [SerializeField] private FireSuppressantType[] _temporaryOnlySuppressants =
        Array.Empty<FireSuppressantType>();
    [Range(0f, 0.99f)]
    [SerializeField] private float _maximumTemporarySuppression = 0.75f;
    [Min(0f)]
    [SerializeField] private float _temporaryRecoveryDelay = 0.15f;
    [Min(0.01f)]
    [SerializeField] private float _temporaryRecoveryDuration = 3f;

    private ParticleInitialState[] _particleInitialStates;
    private float _accumulatedExtinguishTime;
    private float _temporarySuppressionTime;
    private float _lastTemporarySuppressionTime = float.NegativeInfinity;
    private int _currentStage;
    private bool _isExtinguished;
    private Coroutine _temporaryRecoveryRoutine;

    // 불이 꺼졌을때의 후속 동작이 필요하다면 여기 이벤트 구독하면 됨
    public event Action OnExtinguished;
    public event Action<FireSuppressantType> TemporarySuppressionLimitReached;

    // 기존 필드들 아래에 추가
    private AudioSource _audioSource;
    private float _initialVolume;

    // 2026.07.30 / HyungJun / 불 이펙트 -> 라이팅 두트윈 로직 추가
    private Light _light;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource != null)
            _initialVolume = _audioSource.volume;

        if (_fireParticles == null || _fireParticles.Length == 0)
            _fireParticles = GetComponentsInChildren<ParticleSystem>();

        _particleInitialStates = new ParticleInitialState[_fireParticles.Length];

        for (int i = 0; i < _fireParticles.Length; i++)
        {
            ParticleSystem ps = _fireParticles[i];
            if (ps == null) continue;

            var emission = ps.emission;
            var main = ps.main;

            _particleInitialStates[i] = new ParticleInitialState
            {
                Particle = ps,
                RateOverTime = emission.rateOverTime,
                StartSize = main.startSize,
                StartLifetime = main.startLifetime
            };
        }

        // 2026.07.30 / HyungJun / 불 이펙트 -> 라이팅 두트윈 로직 추가
        foreach (Transform child in transform)
        {
            if (child.TryGetComponent(out _light))
            {
                Ease selectedEase = randomEases[UnityEngine.Random.Range(0, randomEases.Length)];

                var rand = UnityEngine.Random.Range(0.5f, 1.0f);
                var rand_duration = UnityEngine.Random.Range(0.5f, 1.0f);
                _light.intensity = 0.5f;

                _light.DOIntensity(rand, rand_duration)
                        .SetEase(selectedEase)
                        .SetLoops(-1, LoopType.Yoyo);

            }
        }
    }

    //public float Light_Duration = 1.0f;

    public void TakeExtinguish(float deltaTime)
    {
        TakeExtinguish(FireSuppressantType.GeneralPurpose, deltaTime);
    }

    public void TakeExtinguish(
        FireSuppressantType suppressantType,
        float deltaTime)
    {
        if (_isExtinguished || deltaTime <= 0f)
        {
            return;
        }

        if (IsTemporaryOnlySuppressant(suppressantType))
        {
            ApplyTemporarySuppression(suppressantType, deltaTime);
            return;
        }

        _accumulatedExtinguishTime += deltaTime;

        float duration = GetExtinguishDuration();

        if (_accumulatedExtinguishTime >= duration)
        {
            _accumulatedExtinguishTime = duration;
            RefreshVisualStage();
            Extinguish();
            return;
        }

        RefreshVisualStage();
    }

    private void ApplyTemporarySuppression(
        FireSuppressantType suppressantType,
        float deltaTime)
    {
        float maximumSuppressionTime = GetMaximumTemporarySuppressionTime();
        float previousSuppressionTime = _temporarySuppressionTime;

        _temporarySuppressionTime = Mathf.Min(
            _temporarySuppressionTime + deltaTime,
            maximumSuppressionTime);
        _lastTemporarySuppressionTime = Time.time;
        RefreshVisualStage();

        if (previousSuppressionTime < maximumSuppressionTime &&
            _temporarySuppressionTime >= maximumSuppressionTime)
        {
            TemporarySuppressionLimitReached?.Invoke(suppressantType);
        }

        if (_temporaryRecoveryRoutine == null)
        {
            _temporaryRecoveryRoutine = StartCoroutine(
                TemporaryRecoveryRoutine());
        }
    }

    private IEnumerator TemporaryRecoveryRoutine()
    {
        while (!_isExtinguished && _temporarySuppressionTime > 0f)
        {
            bool canRecover =
                Time.time - _lastTemporarySuppressionTime >=
                _temporaryRecoveryDelay;

            if (canRecover)
            {
                float recoveryRate =
                    GetMaximumTemporarySuppressionTime() /
                    Mathf.Max(_temporaryRecoveryDuration, 0.01f);
                _temporarySuppressionTime = Mathf.MoveTowards(
                    _temporarySuppressionTime,
                    0f,
                    recoveryRate * Time.deltaTime);
                RefreshVisualStage();
            }

            yield return null;
        }

        _temporaryRecoveryRoutine = null;
    }

    private void RefreshVisualStage()
    {
        float duration = GetExtinguishDuration();
        int stageCount = Mathf.Max(_extinguishStageCount, 1);
        float secondsPerStage = duration / stageCount;
        float visualSuppressionTime = Mathf.Max(
            _accumulatedExtinguishTime,
            _temporarySuppressionTime);

        int nextStage = Mathf.FloorToInt(
            visualSuppressionTime / secondsPerStage);
        nextStage = Mathf.Clamp(nextStage, 0, stageCount);

        if (nextStage == _currentStage)
        {
            return;
        }

        _currentStage = nextStage;

        float intensity = 1f - ((float)_currentStage / stageCount);
        ApplyIntensity(intensity);
    }

    private bool IsTemporaryOnlySuppressant(
        FireSuppressantType suppressantType)
    {
        if (_temporaryOnlySuppressants == null)
        {
            return false;
        }

        return Array.IndexOf(_temporaryOnlySuppressants, suppressantType) >= 0;
    }

    private float GetExtinguishDuration()
    {
        return Mathf.Max(_extinguishDuration, 0.01f);
    }

    private float GetMaximumTemporarySuppressionTime()
    {
        return GetExtinguishDuration() *
               Mathf.Clamp01(_maximumTemporarySuppression);
    }

    private void ApplyIntensity(float intensity)
    {
        foreach (ParticleInitialState state in _particleInitialStates)
        {
            if (state.Particle == null) continue;

            var emission = state.Particle.emission;
            emission.rateOverTime = ScaleCurve(state.RateOverTime, intensity);

            var main = state.Particle.main;
            main.startSize = ScaleCurve(state.StartSize, intensity);
            main.startLifetime = ScaleCurve(state.StartLifetime, intensity);
        }

        if (_audioSource != null)
        {
            _audioSource.volume = _initialVolume * intensity;
        }
    }

    private ParticleSystem.MinMaxCurve ScaleCurve(ParticleSystem.MinMaxCurve curve, float scale)
    {
        switch (curve.mode)
        {
            case ParticleSystemCurveMode.Constant:
                curve.constant *= scale;
                break;

            case ParticleSystemCurveMode.TwoConstants:
                curve.constantMin *= scale;
                curve.constantMax *= scale;
                break;

            case ParticleSystemCurveMode.Curve:
            case ParticleSystemCurveMode.TwoCurves:
                curve.curveMultiplier *= scale;
                break;
        }

        return curve;
    }

    private void Extinguish()
    {
        _isExtinguished = true;

        OnExtinguished?.Invoke();

        foreach (ParticleSystem ps in _fireParticles)
        {
            if (ps == null) continue;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (_audioSource != null)
            _audioSource.volume = 0f;

        if (_disableWhenExtinguished)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (_temporaryRecoveryRoutine == null)
        {
            return;
        }

        StopCoroutine(_temporaryRecoveryRoutine);
        _temporaryRecoveryRoutine = null;
    }

    private void OnValidate()
    {
        _extinguishDuration = Mathf.Max(_extinguishDuration, 0.01f);
        _extinguishStageCount = Mathf.Max(_extinguishStageCount, 1);
        _maximumTemporarySuppression = Mathf.Clamp(
            _maximumTemporarySuppression,
            0f,
            0.99f);
        _temporaryRecoveryDelay = Mathf.Max(0f, _temporaryRecoveryDelay);
        _temporaryRecoveryDuration = Mathf.Max(
            0.01f,
            _temporaryRecoveryDuration);
    }

    #region 플레이어 불 진입 (피해 피드백)
    /*
    // todo : 나중에 플레이어 UI 관련 요소 생기면 여기서 피드백 연결
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("=====Player Enter=====");
            // 어기에서 데미지 코루틴
            // other.transform.GetComponent<PlayerUI>().ChangeScreenVignette(true);
            // other.transform.GetComponent<PlayerUI>().PlayDamageSFX(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("=====Player Exit=====");
            // other.transform.GetComponent<PlayerUI>().ChangeScreenVignette(false);
            // other.transform.GetComponent<PlayerUI>().PlayDamageSFX(false);
        }
    }
    */
    #endregion
}
