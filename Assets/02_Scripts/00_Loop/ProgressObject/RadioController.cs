using System;
using System.Collections;
using UnityEngine;
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

    [Header("Music")]
    [SerializeField] private AudioClip _musicClip;

    private Coroutine _playRoutine;
    
    public void PlayForResult(DayResultContext resultContext)
    {
        AudioClip clip = SelectClip(resultContext);
        
        string clipName = clip != null ? clip.name : "None";
        Debug.Log(
            $"Result={resultContext.ResultType}, " +
            $"SituationId={resultContext.SituationId}, " +
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

        if (broadcastClip != null)
        {
            _radioAudioSource.clip = broadcastClip;
            _radioAudioSource.Play();

            yield return new WaitWhile(() => _radioAudioSource.isPlaying);
        }

        if (_musicClip != null)
        {
            _radioAudioSource.clip = _musicClip;
            _radioAudioSource.Play();
        }

        _playRoutine = null;
    }

    private AudioClip SelectClip(DayResultContext resultContext)
    {
        if (resultContext.ResultType == DayResultType.Failed)
        {
            AudioClip failClip = FindFailClip(resultContext.SituationId);
            return failClip != null
                ? failClip
                : _fallbackFailBroadcastClip;
        }

        return SelectRandomCommonClip();
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
