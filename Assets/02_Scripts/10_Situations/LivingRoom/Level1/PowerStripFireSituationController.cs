using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using VirtualRescue.Effects;
using VirtualRescue.Player;
using VirtualRescue.Situations.FireSuppression;

namespace VirtualRescue.Situations.PowerStripFire
{
    [DisallowMultipleComponent]
    public sealed class PowerStripFireSituationController :
        FireSuppressionSituationController
    {
        [Header("References")]
        [SerializeField] private FireObject _fireObject;
        [SerializeField] private AudioClip _electrocutionAudioClip;
        [SerializeField] private GameObject _electrocutionEffectPrefab;

        [Header("Electrocution")]
        [SerializeField, Min(0f)] private float _failureDelay = 1f;
        [SerializeField, Range(0f, 3f)] private float _electrocutionVolumeScale = 2.5f;
        [SerializeField, Min(0f)] private float _whiteFlashDuration = 0.55f;
        [SerializeField, Min(1)] private int _whiteFlashCount = 3;
        [SerializeField, Range(0f, 1f)] private float _whiteFlashPeakAlpha = 0.75f;
        [SerializeField] private Vector3 _electrocutionEffectOffset = new(0f, 0f, 0.6f);
        [SerializeField, Min(0.1f)] private float _electrocutionEffectScale = 0.8f;
        [SerializeField, Min(0f)] private float _electrocutionEffectHorizontalSpacing = 0.24f;
        [SerializeField, Min(0f)] private float _electrocutionEffectVerticalSpacing = 0.12f;

        private readonly List<GameObject> _electrocutionEffectInstances = new();
        private bool _hasTriggeredElectrocution;
        private Coroutine _electrocutionRoutine;

        protected override void PrepareActiveFireObjects(
            List<FireObject> activeFireObjects)
        {
            _hasTriggeredElectrocution = false;

            if (_fireObject == null)
            {
                Debug.LogError("A power strip fire object must be assigned.", this);
                return;
            }

            activeFireObjects.Add(_fireObject);
        }

        protected override void OnFireSuppressionActivated()
        {
            _fireObject.SuppressantApplied += HandleSuppressantApplied;
        }

        protected override void OnFireSuppressionDeactivated()
        {
            if (_fireObject != null)
            {
                _fireObject.SuppressantApplied -= HandleSuppressantApplied;
            }

            if (_electrocutionRoutine != null)
            {
                StopCoroutine(_electrocutionRoutine);
                _electrocutionRoutine = null;
            }

            // 실패 직후에는 게임오버 전환이 끝날 때까지 감전 효과를 유지한다.
            if (!IsFailed || !gameObject.activeInHierarchy)
            {
                DestroyElectrocutionEffects();
            }
        }

        private void HandleSuppressantApplied(
            FireSuppressantType suppressantType)
        {
            if (!IsActive ||
                _hasTriggeredElectrocution ||
                suppressantType != FireSuppressantType.ClassK)
            {
                return;
            }

            _hasTriggeredElectrocution = true;
            _electrocutionRoutine = StartCoroutine(PlayElectrocutionAndFail());
        }

        private IEnumerator PlayElectrocutionAndFail()
        {
            PlayElectrocutionAudio();
            PlayElectrocutionEffect();

            ScreenFader screenFader = FindScreenFader();

            if (screenFader == null)
            {
                Debug.LogWarning(
                    "ScreenFader was not found for the electrocution flash.",
                    this);
                yield return new WaitForSecondsRealtime(_failureDelay);
            }
            else
            {
                float flashDuration = Mathf.Min(
                    _whiteFlashDuration,
                    _failureDelay);
                yield return screenFader.FlashWhite(
                    flashDuration,
                    _whiteFlashCount,
                    _whiteFlashPeakAlpha);

                float remainingDelay = Mathf.Max(
                    0f,
                    _failureDelay - flashDuration);

                if (remainingDelay > 0f)
                {
                    yield return new WaitForSecondsRealtime(remainingDelay);
                }
            }

            _electrocutionRoutine = null;

            if (!FailSituation())
            {
                Debug.LogWarning(
                    "The power strip fire situation could not be failed after electrocution.",
                    this);
            }
        }

        private void PlayElectrocutionAudio()
        {
            if (_electrocutionAudioClip == null)
            {
                Debug.LogWarning(
                    "Electrocution audio clip is not assigned.",
                    this);
                return;
            }

            PlayerReferenceHub playerReferenceHub = PlayerReferenceHub.Instance;
            AudioSource xrAudioSource = playerReferenceHub?.XrAudioSource;

            if (xrAudioSource == null)
            {
                Debug.LogWarning(
                    "HMD AudioSource was not found on PlayerReferenceHub.",
                    this);
                return;
            }

            xrAudioSource.PlayOneShot(
                _electrocutionAudioClip,
                _electrocutionVolumeScale);
        }

        private void PlayElectrocutionEffect()
        {
            if (_electrocutionEffectPrefab == null)
            {
                Debug.LogWarning(
                    "Electrocution effect prefab is not assigned.",
                    this);
                return;
            }

            Camera hmdCamera = Camera.main;

            if (hmdCamera == null)
            {
                Debug.LogWarning(
                    "The HMD camera was not found for the electrocution effect.",
                    this);
                return;
            }

            for (int index = 0; index < 3; index++)
            {
                float offsetIndex = index - 1f;
                Vector3 positionOffset = new(
                    offsetIndex * _electrocutionEffectHorizontalSpacing,
                    -offsetIndex * _electrocutionEffectVerticalSpacing,
                    0f);
                GameObject effectInstance = Instantiate(
                    _electrocutionEffectPrefab,
                    hmdCamera.transform);
                effectInstance.SetActive(false);

                effectInstance.transform.localPosition =
                    _electrocutionEffectOffset + positionOffset;
                effectInstance.transform.localRotation = Quaternion.identity;
                effectInstance.transform.localScale =
                    Vector3.one * _electrocutionEffectScale;
                _electrocutionEffectInstances.Add(effectInstance);

                VisualEffect[] visualEffects =
                    effectInstance.GetComponentsInChildren<VisualEffect>(true);

                foreach (VisualEffect visualEffect in visualEffects)
                {
                    visualEffect.resetSeedOnPlay = false;
                    visualEffect.startSeed = (uint)(100 + index);
                }

                effectInstance.SetActive(true);

                foreach (VisualEffect visualEffect in visualEffects)
                {
                    visualEffect.Reinit();
                }
            }
        }

        private static ScreenFader FindScreenFader()
        {
            if (PersistentPlayerRoot.Instance != null)
            {
                ScreenFader playerFader =
                    PersistentPlayerRoot.Instance.GetComponentInChildren<ScreenFader>(true);

                if (playerFader != null)
                {
                    return playerFader;
                }
            }

            return FindFirstObjectByType<ScreenFader>(FindObjectsInactive.Include);
        }

        private void DestroyElectrocutionEffects()
        {
            foreach (GameObject effectInstance in _electrocutionEffectInstances)
            {
                if (effectInstance != null)
                {
                    Destroy(effectInstance);
                }
            }

            _electrocutionEffectInstances.Clear();
        }

        private void OnDestroy()
        {
            DestroyElectrocutionEffects();
        }
    }
}
