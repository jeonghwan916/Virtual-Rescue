using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

namespace VirtualRescue.Interaction
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class HandleHeatDetector : MonoBehaviour
    {
        [Header("Temperature")]
        [SerializeField]
        private DoorHandleTemperature _handleTemperature;

        [Header("Haptic")]
        [SerializeField, Range(0f, 1f)]
        private float _hapticAmplitude = 1f;

        [SerializeField, Min(0.01f)]
        private float _hapticDuration = 0.15f;

        [SerializeField, Min(0.01f)]
        private float _hapticInterval = 0.3f;

        [Header("Fire Audio")]
        [SerializeField]
        private bool _enableFireAudio = true;

        [SerializeField]
        private AudioSource _fireAudio;

        [SerializeField, Min(0f)]
        private float _audioDelay = 1f;

        [Header("Handle Color")]
        [SerializeField]
        private Renderer _handleRenderer;

        [SerializeField]
        private Color _dangerBaseColor =
            new Color(1f, 0.18f, 0.02f, 1f);

        [SerializeField, ColorUsage(true, true)]
        private Color _dangerEmissionColor =
            new Color(4f, 0.6f, 0f, 1f);

        [SerializeField, Min(0.01f)]
        private float _colorFadeDuration = 1.5f;

        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");

        private static readonly int EmissionColorId =
            Shader.PropertyToID("_EmissionColor");

        // 불 소리 재생을 위한 플레이어 감지 목록
        private readonly HashSet<Collider>
            _audioDetectionColliders = new();

        // 햅틱과 손잡이 색상 변경을 위한 컨트롤러 감지 목록
        private readonly HashSet<Collider>
            _controllerColliders = new();

        private HapticImpulsePlayer _hapticPlayer;

        private Coroutine _hapticRoutine;
        private Coroutine _audioDelayRoutine;

        private MaterialPropertyBlock _propertyBlock;

        private Color _normalBaseColor;
        private float _currentColorAmount;
        private float _targetColorAmount;

        private void Awake()
        {
            InitializeAudio();
            InitializeHandleColor();
        }

        private void InitializeAudio()
        {
            if (_fireAudio == null)
            {
                _fireAudio = GetComponent<AudioSource>();
            }

            _fireAudio.playOnAwake = false;
            _fireAudio.loop = true;
            _fireAudio.Stop();
        }

        private void InitializeHandleColor()
        {
            _propertyBlock = new MaterialPropertyBlock();

            if (_handleRenderer != null &&
                _handleRenderer.sharedMaterial != null &&
                _handleRenderer.sharedMaterial.HasProperty(BaseColorId))
            {
                // 기존 머티리얼의 실버색 저장
                _normalBaseColor =
                    _handleRenderer.sharedMaterial.GetColor(BaseColorId);
            }
            else
            {
                _normalBaseColor = Color.gray;
            }

            _currentColorAmount = 0f;
            _targetColorAmount = 0f;

            ApplyHandleColor();
        }

        private void Update()
        {
            UpdateHandleColor();

            if (_handleTemperature == null)
            {
                StopAllFeedback();
                return;
            }

            // 안전 온도에서는 모든 피드백 정지
            if (!_handleTemperature.IsDangerous)
            {
                StopAllFeedback();
                return;
            }

            // 플레이어가 감지 영역에 있으면 오디오 지연 시작
            if (_enableFireAudio &&
                _audioDetectionColliders.Count > 0 &&
                _audioDelayRoutine == null &&
                !_fireAudio.isPlaying)
            {
                _audioDelayRoutine =
                    StartCoroutine(FireAudioDelayRoutine());
            }

            // 실제 컨트롤러가 감지되면 반복 햅틱 시작
            if (_hapticPlayer != null &&
                _controllerColliders.Count > 0 &&
                _hapticRoutine == null)
            {
                _hapticRoutine =
                    StartCoroutine(RepeatHaptic());
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_handleTemperature == null)
            {
                Debug.LogWarning(
                    $"[{name}] DoorHandleTemperature가 연결되지 않았습니다.",
                    this);

                return;
            }

            XROrigin xrOrigin =
                other.GetComponentInParent<XROrigin>();

            HapticImpulsePlayer detectedHapticPlayer =
                other.GetComponentInParent<HapticImpulsePlayer>();

            // XR 플레이어나 컨트롤러가 아니면 무시
            if (xrOrigin == null &&
                detectedHapticPlayer == null)
            {
                return;
            }

            Debug.Log(
                $"[{name}] 접근 감지 | " +
                $"대상: {other.name} | " +
                $"XR Origin: {xrOrigin != null} | " +
                $"Haptic Player: {detectedHapticPlayer != null} | " +
                $"온도: {_handleTemperature.Temperature}°C | " +
                $"위험: {_handleTemperature.IsDangerous}",
                this);

            // XR Origin 또는 컨트롤러 접근 기록
            _audioDetectionColliders.Add(other);

            if (!_handleTemperature.IsDangerous)
            {
                return;
            }

            // XR Origin만 감지되어도 3초 뒤 소리 재생
            if (_enableFireAudio &&
                _audioDelayRoutine == null &&
                !_fireAudio.isPlaying)
            {
                _audioDelayRoutine =
                    StartCoroutine(FireAudioDelayRoutine());
            }

            // HapticImpulsePlayer가 없으면 소리만 처리
            if (detectedHapticPlayer == null)
            {
                return;
            }

            // 다른 컨트롤러를 이미 처리하고 있으면 무시
            if (_hapticPlayer != null &&
                _hapticPlayer != detectedHapticPlayer)
            {
                return;
            }

            _hapticPlayer = detectedHapticPlayer;
            _controllerColliders.Add(other);

            Debug.Log(
                $"[{name}] 컨트롤러 햅틱 감지 | " +
                $"Haptic Player: {_hapticPlayer.name}",
                this);

            if (_hapticRoutine == null)
            {
                _hapticRoutine =
                    StartCoroutine(RepeatHaptic());
            }
        }

        private void OnTriggerExit(Collider other)
        {
            _audioDetectionColliders.Remove(other);

            // XR 플레이어의 모든 Collider가 빠졌을 때 소리 정지
            if (_audioDetectionColliders.Count == 0)
            {
                CancelAudioDelay();
                StopFireAudio();
            }

            HapticImpulsePlayer exitedHapticPlayer =
                other.GetComponentInParent<HapticImpulsePlayer>();

            if (exitedHapticPlayer == null ||
                exitedHapticPlayer != _hapticPlayer)
            {
                return;
            }

            _controllerColliders.Remove(other);

            if (_controllerColliders.Count > 0)
            {
                return;
            }

            StopHapticRoutine();
            _hapticPlayer = null;

            Debug.Log(
                $"[{name}] 컨트롤러가 감지 영역에서 나갔습니다.",
                this);
        }

        private IEnumerator FireAudioDelayRoutine()
        {
            Debug.Log(
                $"[{name}] 불 소리 재생까지 {_audioDelay}초 대기합니다.",
                this);

            yield return new WaitForSeconds(_audioDelay);

            bool canPlay =
                _enableFireAudio &&
                _handleTemperature != null &&
                _handleTemperature.IsDangerous &&
                _audioDetectionColliders.Count > 0;

            if (canPlay)
            {
                StartFireAudio();

                Debug.Log(
                    $"[{name}] 불 소리를 재생합니다.",
                    this);
            }

            _audioDelayRoutine = null;
        }

        private IEnumerator RepeatHaptic()
        {
            while (_hapticPlayer != null &&
                   _handleTemperature != null &&
                   _handleTemperature.IsDangerous &&
                   _controllerColliders.Count > 0)
            {
                _hapticPlayer.SendHapticImpulse(
                    _hapticAmplitude,
                    _hapticDuration);

                yield return new WaitForSeconds(
                    _hapticInterval);
            }

            _hapticRoutine = null;
        }

        private void UpdateHandleColor()
        {
            if (_handleRenderer == null ||
                _propertyBlock == null)
            {
                return;
            }

            bool shouldBeOrange =
                _handleTemperature != null &&
                _handleTemperature.IsDangerous &&
                _controllerColliders.Count > 0;

            _targetColorAmount =
                shouldBeOrange ? 1f : 0f;

            float fadeSpeed =
                1f / _colorFadeDuration;

            _currentColorAmount = Mathf.MoveTowards(
                _currentColorAmount,
                _targetColorAmount,
                fadeSpeed * Time.deltaTime);

            ApplyHandleColor();
        }

        private void ApplyHandleColor()
        {
            if (_handleRenderer == null ||
                _propertyBlock == null)
            {
                return;
            }

            Color baseColor = Color.Lerp(
                _normalBaseColor,
                _dangerBaseColor,
                _currentColorAmount);

            Color emissionColor = Color.Lerp(
                Color.black,
                _dangerEmissionColor,
                _currentColorAmount);

            _handleRenderer.GetPropertyBlock(
                _propertyBlock);

            _propertyBlock.SetColor(
                BaseColorId,
                baseColor);

            _propertyBlock.SetColor(
                EmissionColorId,
                emissionColor);

            _handleRenderer.SetPropertyBlock(
                _propertyBlock);
        }

        private void StartFireAudio()
        {
            if (!_enableFireAudio ||
                _fireAudio == null)
            {
                Debug.LogWarning(
                    $"[{name}] AudioSource가 없습니다.",
                    this);

                return;
            }

            if (_fireAudio.resource == null)
            {
                Debug.LogWarning(
                    $"[{name}] Audio Resource가 연결되지 않았습니다.",
                    this);

                return;
            }

            if (!_fireAudio.isPlaying)
            {
                _fireAudio.Play();
            }
        }

        private void StopFireAudio()
        {
            if (_fireAudio != null &&
                _fireAudio.isPlaying)
            {
                _fireAudio.Stop();
            }
        }

        private void StopHapticRoutine()
        {
            if (_hapticRoutine == null)
            {
                return;
            }

            StopCoroutine(_hapticRoutine);
            _hapticRoutine = null;
        }

        private void CancelAudioDelay()
        {
            if (_audioDelayRoutine == null)
            {
                return;
            }

            StopCoroutine(_audioDelayRoutine);
            _audioDelayRoutine = null;
        }

        private void StopAllFeedback()
        {
            StopHapticRoutine();
            CancelAudioDelay();
            StopFireAudio();
        }

        private void OnDisable()
        {
            StopAllFeedback();

            _audioDetectionColliders.Clear();
            _controllerColliders.Clear();

            _hapticPlayer = null;

            // 손잡이 색상을 즉시 원래 실버로 복구
            _currentColorAmount = 0f;
            _targetColorAmount = 0f;

            ApplyHandleColor();
        }

        private void OnValidate()
        {
            _hapticDuration =
                Mathf.Max(0.01f, _hapticDuration);

            _hapticInterval =
                Mathf.Max(
                    _hapticDuration,
                    _hapticInterval);

            _audioDelay =
                Mathf.Max(0f, _audioDelay);

            _colorFadeDuration =
                Mathf.Max(0.01f, _colorFadeDuration);
        }
    }
}
