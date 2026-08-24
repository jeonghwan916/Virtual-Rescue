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
    [SerializeField] private AudioClip _endingAudioClip;
    [SerializeField] private float _holdSeconds = 2f;
    [SerializeField] private string _lobbySceneName = "LobbyScene";
    
    private static readonly int ButtonPressedHash = Animator.StringToHash("ButtonPressed");
    private static readonly int EleavatorAnimHash = Animator.StringToHash("EleavatorAnim");
    private const float DoorOpeningNormalizedTime = 3f / 7f;

    private bool _isEnding;


    private IEnumerator Start()
    {
        if (PersistentPlayerRoot.Instance != null && _playerSpawnPoint != null)
        {
            PersistentPlayerRoot.Instance.ApplySpawn(_playerSpawnPoint);
        }

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
        if (_endingAudioSource != null && _endingAudioClip != null)
        {
            _endingAudioSource.PlayOneShot(_endingAudioClip);
        }

        if (_holdSeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(_holdSeconds);
        }

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
