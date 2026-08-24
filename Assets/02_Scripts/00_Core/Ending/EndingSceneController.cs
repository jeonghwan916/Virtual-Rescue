using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using VirtualRescue.Effects;
using VirtualRescue.Player;

public class EndingSceneController : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform _playerSpawnPoint;

    [Header("Eleavator")]
    [SerializeField] private Animator _eleavatorAnimator;
    [SerializeField] private AudioSource _eleavatorAudioSrc;
    [SerializeField] private AudioClip _eleavatorOpeningClip;
    [SerializeField] private float _eleavatorTimeoutSeconds = 10f;

    [Header("Fade")]
    [SerializeField] private float _fadeInDuration = 1.5f;
    [SerializeField] private float _fadeOutDuration = 1f;

    [Header("Ending")]
    [SerializeField] private AudioSource _endingAudioSource;
    [SerializeField] private AudioClip _endingMusicClip;
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private float _bgmFadeOutDuration = 2f;
    [SerializeField] private float _holdSeconds = 2f;
    [SerializeField] private string _lobbySceneName = "LobbyScene";
    
    private static readonly int ButtonPressedHash = Animator.StringToHash("ButtonPressed");
    private static readonly int EleavatorAnimHash = Animator.StringToHash("EleavatorAnim");
    private const float DoorOpeningNormalizedTime = 3f / 7f;

    private bool _isEnding;
    private float _bgmInitialVolume;

    [Header("Text")]
    [SerializeField] private GameObject _titleObject;
    [SerializeField] private GameObject _teamNameObject;
    [SerializeField] private GameObject _teammateObject1;
    [SerializeField] private GameObject _teammateObject2;
    [SerializeField] private GameObject _teammateObject3;
    
    [Header("After Effects")]
    [SerializeField] private AudioSource _sprinklerSource;
    [SerializeField] private ParticleSystem _sprinklerEffect;
    [SerializeField] private AudioSource _alarmSource;
    [SerializeField] private ParticleSystem _smokeEffect;
    


    private IEnumerator Start()
    {
        if (PersistentPlayerRoot.Instance != null && _playerSpawnPoint != null)
        {
            PersistentPlayerRoot.Instance.ApplySpawn(_playerSpawnPoint);
        }

        PlayBgm();
        OpeningEleavator();

        ScreenFader screenFader = FindScreenFader();
        if (screenFader == null)
        {
            yield break;
        }

        screenFader.ShowBlack();
        yield return screenFader.FadeIn(_fadeInDuration);
    }

    public bool RequestEnding()
    {
        if (_isEnding)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_lobbySceneName) ||
            !Application.CanStreamedLevelBeLoaded(_lobbySceneName))
        {
            Debug.LogError(
                $"{nameof(EndingSceneController)}: " +
                $"Lobby scene is not available: {_lobbySceneName}",
                this);
            return false;
        }

        _isEnding = true;
        StartCoroutine(EndingRoutine());
        return true;
    }

    private void OpeningEleavator()
    {
        StartCoroutine(RequestExitAfterElevatorAnimation());
    }
    
    private IEnumerator RequestExitAfterElevatorAnimation()
    {
        if (_eleavatorAnimator == null)
        {
            Debug.LogWarning(
                $"{nameof(EndingSceneController)}: Elevator animator is not assigned.",
                this);
            yield break;
        }

        _eleavatorAnimator.SetTrigger(ButtonPressedHash);

        yield return null;

        float elapsed = 0f;
        while (_eleavatorAnimator.IsInTransition(0) ||
               _eleavatorAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash != EleavatorAnimHash)
        {
            elapsed += Time.unscaledDeltaTime;
            if (elapsed >= _eleavatorTimeoutSeconds)
            {
                ReportElevatorTimeout("enter animation state");
                yield break;
            }

            yield return null;
        }

        elapsed = 0f;
        while (_eleavatorAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < DoorOpeningNormalizedTime)
        {
            elapsed += Time.unscaledDeltaTime;
            if (elapsed >= _eleavatorTimeoutSeconds)
            {
                ReportElevatorTimeout("reach the door opening point");
                yield break;
            }

            yield return null;
        }

        //_eleavatorAudioSrc.PlayOneShot(_eleavatorOpeningClip);

        elapsed = 0f;
        while (_eleavatorAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            elapsed += Time.unscaledDeltaTime;
            if (elapsed >= _eleavatorTimeoutSeconds)
            {
                ReportElevatorTimeout("finish animation");
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator EndingRoutine()
    {
        // 플레이어 조작 막아버리기 : 컨트롤러를 통한 이동
        
        // 씬 시작부터 재생 중인 BGM을 페이드아웃한다.
        if (_bgmSource != null)
        {
            StartCoroutine(FadeOutBgm());
        }

        // 엔딩 연출 음악은 BGM과 별도의 AudioSource에서 재생한다.
        if (_endingAudioSource != null && _endingMusicClip != null)
        {
            _endingAudioSource.PlayOneShot(_endingMusicClip);
        }

        // 잠시 대기
        if (_holdSeconds > 0f) { yield return new WaitForSecondsRealtime(_holdSeconds); }
        
        // 게임 제목 Text 띄우기
        _titleObject.SetActive(true);
        
        if (_holdSeconds > 0f) { yield return new WaitForSecondsRealtime(_holdSeconds); }
        
        // 팀명 Text 띄우기
        _titleObject.SetActive(false);
        _teammateObject1.SetActive(true);
        
        if (_holdSeconds > 0f) { yield return new WaitForSecondsRealtime(_holdSeconds); }
        
        // 각자 역할 띄우기
        _teammateObject1.SetActive(false);
        _teammateObject2.SetActive(true);
        
        if (_sprinklerSource != null) _sprinklerSource.Play();
        if (_sprinklerEffect != null) _sprinklerEffect.Play();
        
        if (_holdSeconds > 0f) { yield return new WaitForSecondsRealtime(_holdSeconds); }
        
        _teammateObject2.SetActive(false);
        _teammateObject3.SetActive(true);
        
        if (_alarmSource != null) _alarmSource.Play();
        
        if (_holdSeconds > 0f) { yield return new WaitForSecondsRealtime(_holdSeconds); }
        
        _teammateObject3.SetActive(false);
        _teamNameObject.SetActive(true);
        
        if (_smokeEffect != null) _smokeEffect.Play();
        
        if (_holdSeconds > 0f) { yield return new WaitForSecondsRealtime(_holdSeconds); }
        
        // 화면 암전
        ScreenFader screenFader = FindScreenFader();
        if (screenFader != null)
        {
            yield return screenFader.FadeOut(_fadeOutDuration);
        }

        if (_holdSeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(_holdSeconds);
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(
            _lobbySceneName,
            LoadSceneMode.Single);

        if (operation == null)
        {
            Debug.LogError(
                $"{nameof(EndingSceneController)}: " +
                $"Failed to load lobby scene: {_lobbySceneName}",
                this);
            _isEnding = false;
            yield break;
        }

        yield return operation;
    }

    private void PlayBgm()
    {
        if (_bgmSource == null)
        {
            return;
        }

        _bgmInitialVolume = _bgmSource.volume;

        if (_bgmSource.clip == null)
        {
            return;
        }

        _bgmSource.loop = true;
        _bgmSource.Play();
    }

    private IEnumerator FadeOutBgm()
    {
        if (_bgmSource == null || !_bgmSource.isPlaying)
        {
            yield break;
        }

        float startVolume = _bgmSource.volume;

        if (_bgmFadeOutDuration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < _bgmFadeOutDuration && _bgmSource != null)
            {
                elapsed += Time.unscaledDeltaTime;
                _bgmSource.volume = Mathf.Lerp(
                    startVolume,
                    0f,
                    Mathf.Clamp01(elapsed / _bgmFadeOutDuration));
                yield return null;
            }
        }

        if (_bgmSource != null)
        {
            _bgmSource.Stop();
            _bgmSource.volume = _bgmInitialVolume;
        }
    }

    private void ReportElevatorTimeout(string phase)
    {
        Debug.LogWarning(
            $"{nameof(EndingSceneController)}: " +
            $"Elevator animation timed out while waiting to {phase}.",
            this);
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
}
