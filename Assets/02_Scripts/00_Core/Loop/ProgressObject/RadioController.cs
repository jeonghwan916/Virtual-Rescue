using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using VirtualRescue.GameFlow;

public class RadioController : MonoBehaviour
{
    [Serializable]
    private struct FailBroadcastEntry
    {
        [SerializeField] private string _situationId;
        [SerializeField] private AudioClip _clip;

        public string SituationId => _situationId?.Trim() ?? string.Empty;
        public AudioClip Clip => _clip;
    }

    [Header("Radio")]
    [SerializeField] private AudioSource _radioAudioSource;
    
    [Header("Common Broad Cast")]
    [SerializeField] private AudioClip[] _commonBroadcastAudioClips;
    
    [Header("Failed Broad Cast")]
    [SerializeField] private FailBroadcastEntry[] _failBroadcastEntries;
    [SerializeField] private AudioClip _fallbackFailBroadcastClip;
    
    [Header("Special Failed Broad Cast")]
    [SerializeField] private AudioClip _lightweightPartitionIncidentBroadcastClip;
    [SerializeField] private AudioClip _wrongCellPhoneCallBroadcastClip;

    [Header("Music")]
    [SerializeField] private AudioClip _musicClip;

    [Header("Radio Effect")]
    [SerializeField] private AudioSource _staticAudioSource;
    [SerializeField] private AudioClip _staticNoiseClip;
    [Min(0f)]
    [FormerlySerializedAs("_staticLeadInDuration")]
    [SerializeField] private float _introStaticDuration = 0.8f;
    [Min(0f)]
    [SerializeField] private float _introStaticOverlapDuration = 0.3f;
    [Min(0f)]
    [SerializeField] private float _outroStaticDuration = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float _staticVolume = 0.7f;

    [Header("Broadcast Fade")]
    [Min(0f)]
    [FormerlySerializedAs("_fadeInDuration")]
    [SerializeField] private float _broadcastFadeInDuration = 0.5f;
    [Min(0f)]
    [FormerlySerializedAs("_fadeOutDuration")]
    [SerializeField] private float _broadcastFadeOutDuration = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float _broadcastVolume = 1f;

    private Coroutine _playRoutine;

    private void Awake()
    {
        EnsureStaticAudioSource();
        EnsureStaticNoiseLoaded();
    }
    
    public void PlayForResult(DayResultContext resultContext)
    {
        EnsureStaticAudioSource();
        EnsureStaticNoiseLoaded();

        AudioClip clip = SelectClip(resultContext);
        
        string clipName = clip != null ? clip.name : "None";
        Debug.Log(
            $"Result={resultContext.ResultType}, " +
            $"SituationId={resultContext.SituationId}, " +
            $"FailureReason={resultContext.FailureReason}, " +
            $"Clip={clipName}",
            this);
        
        if (_radioAudioSource == null)
        {
            return;
        }

        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
        }

        _playRoutine = StartCoroutine(PlayBroadcastThenMusic(clip));
    }

    private IEnumerator PlayBroadcastThenMusic(AudioClip broadcastClip)
    {
        _radioAudioSource.Stop();
        StopStatic();

        if (broadcastClip != null)
        {
            float fadeInDuration = Mathf.Min(
                _broadcastFadeInDuration,
                broadcastClip.length * 0.5f);

            bool playIntroStatic =
                CanPlayStaticEffect() && _introStaticDuration > 0f;
            float staticOverlapDuration = 0f;

            if (playIntroStatic)
            {
                PlayStatic();

                staticOverlapDuration = Mathf.Min(
                    _introStaticOverlapDuration,
                    _introStaticDuration);
                float staticOnlyDuration = Mathf.Max(
                    0f,
                    _introStaticDuration - staticOverlapDuration);

                if (staticOnlyDuration > 0f)
                {
                    yield return new WaitForSeconds(staticOnlyDuration);
                }
            }

            _radioAudioSource.clip = broadcastClip;
            _radioAudioSource.volume = 0f;
            _radioAudioSource.Play();

            float fadeOutDuration = Mathf.Min(
                _broadcastFadeOutDuration,
                broadcastClip.length - fadeInDuration);

            if (playIntroStatic)
            {
                yield return FadeInBroadcastAndOutStatic(
                    fadeInDuration,
                    staticOverlapDuration);
            }
            else
            {
                yield return FadeVolume(
                    0f,
                    _broadcastVolume,
                    fadeInDuration);
            }

            float holdDuration = Mathf.Max(
                0f,
                broadcastClip.length -
                fadeInDuration -
                fadeOutDuration);

            if (holdDuration > 0f)
            {
                yield return new WaitForSeconds(holdDuration);
            }

            yield return FadeVolume(
                _radioAudioSource.volume,
                0f,
                fadeOutDuration);

            _radioAudioSource.Stop();

            yield return PlayOutroStatic();
        }

        if (_musicClip != null)
        {
            _radioAudioSource.volume = _broadcastVolume;
            _radioAudioSource.clip = _musicClip;
            _radioAudioSource.Play();
        }

        _playRoutine = null;
    }

    private void EnsureStaticAudioSource()
    {
        if (_staticAudioSource != null || _radioAudioSource == null)
        {
            return;
        }

        _staticAudioSource = gameObject.AddComponent<AudioSource>();
        _staticAudioSource.playOnAwake = false;
        _staticAudioSource.loop = false;
        _staticAudioSource.bypassEffects = _radioAudioSource.bypassEffects;
        _staticAudioSource.bypassListenerEffects =
            _radioAudioSource.bypassListenerEffects;
        _staticAudioSource.bypassReverbZones =
            _radioAudioSource.bypassReverbZones;
        _staticAudioSource.dopplerLevel = _radioAudioSource.dopplerLevel;
        _staticAudioSource.mute = _radioAudioSource.mute;
        _staticAudioSource.outputAudioMixerGroup =
            _radioAudioSource.outputAudioMixerGroup;
        _staticAudioSource.panStereo = _radioAudioSource.panStereo;
        _staticAudioSource.pitch = _radioAudioSource.pitch;
        _staticAudioSource.priority = _radioAudioSource.priority;
        _staticAudioSource.reverbZoneMix = _radioAudioSource.reverbZoneMix;
        _staticAudioSource.spatialBlend = _radioAudioSource.spatialBlend;
        _staticAudioSource.spatialize = _radioAudioSource.spatialize;
        _staticAudioSource.spatializePostEffects =
            _radioAudioSource.spatializePostEffects;
        _staticAudioSource.spread = _radioAudioSource.spread;
        _staticAudioSource.volume = _radioAudioSource.volume;
        _staticAudioSource.rolloffMode = _radioAudioSource.rolloffMode;
        _staticAudioSource.minDistance = _radioAudioSource.minDistance;
        _staticAudioSource.maxDistance = _radioAudioSource.maxDistance;
        _staticAudioSource.SetCustomCurve(
            AudioSourceCurveType.CustomRolloff,
            _radioAudioSource.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
        _staticAudioSource.SetCustomCurve(
            AudioSourceCurveType.SpatialBlend,
            _radioAudioSource.GetCustomCurve(AudioSourceCurveType.SpatialBlend));
        _staticAudioSource.SetCustomCurve(
            AudioSourceCurveType.Spread,
            _radioAudioSource.GetCustomCurve(AudioSourceCurveType.Spread));
        _staticAudioSource.SetCustomCurve(
            AudioSourceCurveType.ReverbZoneMix,
            _radioAudioSource.GetCustomCurve(AudioSourceCurveType.ReverbZoneMix));
    }

    private void EnsureStaticNoiseLoaded()
    {
        if (_staticNoiseClip != null &&
            _staticNoiseClip.loadState == AudioDataLoadState.Unloaded)
        {
            _staticNoiseClip.LoadAudioData();
        }
    }

    private bool CanPlayStaticEffect()
    {
        return _staticAudioSource != null &&
               _staticAudioSource != _radioAudioSource &&
               _staticNoiseClip != null;
    }

    private void PlayStatic()
    {
        _staticAudioSource.Stop();
        _staticAudioSource.clip = _staticNoiseClip;
        _staticAudioSource.loop = true;
        _staticAudioSource.volume = _staticVolume;
        _staticAudioSource.Play();
    }

    private void StopStatic()
    {
        if (_staticAudioSource == null)
        {
            return;
        }

        _staticAudioSource.Stop();
        _staticAudioSource.loop = false;
    }

    private IEnumerator FadeInBroadcastAndOutStatic(
        float broadcastFadeDuration,
        float staticFadeDuration)
    {
        float duration = Mathf.Max(
            broadcastFadeDuration,
            staticFadeDuration);

        float elapsed = 0f;
        float startStaticVolume = _staticAudioSource.volume;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float broadcastRatio = broadcastFadeDuration <= 0f
                ? 1f
                : Mathf.Clamp01(elapsed / broadcastFadeDuration);
            float staticRatio = staticFadeDuration <= 0f
                ? 1f
                : Mathf.Clamp01(elapsed / staticFadeDuration);

            _radioAudioSource.volume = Mathf.Lerp(
                0f,
                _broadcastVolume,
                broadcastRatio);
            _staticAudioSource.volume = Mathf.Lerp(
                startStaticVolume,
                0f,
                staticRatio);

            yield return null;
        }

        _radioAudioSource.volume = _broadcastVolume;
        StopStatic();
    }

    private IEnumerator PlayOutroStatic()
    {
        if (!CanPlayStaticEffect() || _outroStaticDuration <= 0f)
        {
            yield break;
        }

        PlayStatic();
        yield return new WaitForSeconds(_outroStaticDuration);
        StopStatic();
    }

    private IEnumerator FadeVolume(
        float startVolume,
        float targetVolume,
        float duration)
    {
        if (duration <= 0f)
        {
            _radioAudioSource.volume = targetVolume;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float ratio = Mathf.Clamp01(elapsed / duration);

            _radioAudioSource.volume = Mathf.Lerp(
                startVolume,
                targetVolume,
                ratio);

            yield return null;
        }

        _radioAudioSource.volume = targetVolume;
    }

    private AudioClip SelectClip(DayResultContext resultContext)
    {
        if (resultContext.ResultType == DayResultType.Failed)
        {
            if (IsLightweightPartitionIncident(resultContext))
            {
                Debug.Log("Light weight partition failed");
                return _lightweightPartitionIncidentBroadcastClip;
            }

            if (resultContext.FailureReason == DayFailureReason.WrongCellPhoneCall)
            {
                Debug.Log("Wrong Cell Phone Call failed");
                return _wrongCellPhoneCallBroadcastClip;
            }

            AudioClip failClip = FindFailClip(resultContext.SituationId);
            return failClip != null
                ? failClip
                : _fallbackFailBroadcastClip;
        }

        return SelectRandomCommonClip();
    }

    private static bool IsLightweightPartitionIncident(
        DayResultContext resultContext)
    {
        return resultContext.FailureReason ==
                   DayFailureReason.InvalidLightweightPartitionExit ||
               resultContext.FailureReason ==
                   DayFailureReason.NoDiscoveryLightweightPartitionExit;
    }

    private AudioClip FindFailClip(string situationId)
    {
        if (string.IsNullOrWhiteSpace(situationId) ||
            _failBroadcastEntries == null)
        {
            return null;
        }

        string normalizedSituationId = situationId.Trim();
        foreach (FailBroadcastEntry entry in _failBroadcastEntries)
        {
            if (string.Equals(
                    entry.SituationId,
                    normalizedSituationId,
                    StringComparison.Ordinal))
            {
                return entry.Clip;
            }
        }

        return null;
    }

    private AudioClip SelectRandomCommonClip()
    {
        if (_commonBroadcastAudioClips == null ||
            _commonBroadcastAudioClips.Length == 0)
        {
            return null;
        }

        int index = UnityEngine.Random.Range(
            0,
            _commonBroadcastAudioClips.Length);
        return _commonBroadcastAudioClips[index];
    }
}
