using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using VirtualRescue.DialogueSystem;
using VirtualRescue.Effects;
using VirtualRescue.GameFlow;
using VirtualRescue.Locations;
using VirtualRescue.Player;

public class DaySceneCoordinator : MonoBehaviour
{
    [Header("Flow")]
    [SerializeField] private DayFlowController _dayFlowController;

    [Header("Home")]
    [SerializeField] private HomeModuleLoader _homeModuleLoader;
    [SerializeField] private HomeLayoutDefinition _homeLayout;

    [Header("Player")]
    [SerializeField] private PersistentPlayerRoot _playerRoot;
    [SerializeField] private Transform _dayStartSpawnPoint;

    [Header("Fade")]
    [SerializeField] private ScreenFader _screenFader;
    [SerializeField] private float _fadeInDuration = 1.5f;
    [SerializeField] private float _fadeOutDuration = 1f;

    [Header("Radio")]
    [SerializeField] private RadioController _radioController;
    [SerializeField] private float _radioStartDelay = 2f;

    [Header("Dialogue")]
    [SerializeField] private DialogueManager _dialogueManager;

    [Header("Situation")]
    [SerializeField] private SituationSelector _situationSelector;
    [SerializeField] private SituationSceneLoader _situationSceneLoader;
    [SerializeField] private RoomSituationController _roomSituationController;

    [Header("Ending Transition")]
    [SerializeField] private string _endingSceneName = "EndingScene";

    private Level2TimePressureEffect _level2TimePressureEffect;
    private GravityProvider _playerGravityProvider;
    private bool _gravityWasEnabled;
    private bool _isPlayerGravitySuspended;
    private bool _sceneReadyNotified;
    private bool _isProcessing;

    private void OnEnable()
    {
        if (_dayFlowController == null)
        {
            return;
        }

        _dayFlowController.DayStarted += HandleDayStarted;
        _dayFlowController.TransitionRequested += HandleTransitionRequested;
    }

    private void OnDisable()
    {
        if (_dayFlowController == null)
        {
            return;
        }

        _dayFlowController.DayStarted -= HandleDayStarted;
        _dayFlowController.TransitionRequested -= HandleTransitionRequested;
    }

    private void OnDestroy()
    {
        RestorePlayerGravity();

        if (_level2TimePressureEffect != null)
        {
            _level2TimePressureEffect.UnbindSceneDependencies(
                _dayFlowController);
        }
    }

    private async void HandleDayStarted(int currentDay)
    {
        if (_isProcessing)
        {
            return;
        }

        _isProcessing = true;

        try
        {
            if (!TryResolvePlayerDependencies())
            {
                return;
            }

            _screenFader?.ShowBlack();
            SuspendPlayerGravity();
            _roomSituationController?.PrepareDayEntryDialogues();

            bool homeLoaded = await _homeModuleLoader.LoadAsync(_homeLayout);
            if (!homeLoaded)
            {
                ReportError(_homeModuleLoader.LastError);
                return;
            }

            _playerRoot.ApplySpawn(_dayStartSpawnPoint);
            RestorePlayerGravity();

            bool selectionSucceeded = _situationSelector.TrySelect(
                currentDay,
                _dayFlowController.SeenSituationIds,
                out SituationDefinition selectedSituation);

            if (!selectionSucceeded)
            {
                ReportError(_situationSelector.LastError);
                return;
            }

            if (selectedSituation != null)
            {
                bool situationLoaded =
                    await _situationSceneLoader.LoadAsync(selectedSituation);

                if (!situationLoaded)
                {
                    ReportError(_situationSceneLoader.LastError);
                    return;
                }

                if (!_dayFlowController.TryRegisterSituation(selectedSituation))
                {
                    ReportError(
                        $"상황 ID를 등록하지 못했습니다: {selectedSituation.Id}");
                    return;
                }
            }
            else
            {
                Debug.Log($"{currentDay}일차는 무상황입니다.", this);
            }

            _roomSituationController?.Configure(
                selectedSituation,
                _situationSceneLoader.CurrentController);

            NotifySceneReadyOnce();

            await RunFadeAsync(_screenFader?.FadeIn(_fadeInDuration));

            if (!_dayFlowController.NotifyHomeLoaded())
            {
                ReportError("하루를 Playing 상태로 전환하지 못했습니다.");
                return;
            }

            _roomSituationController?.ActivateDayEntryDialogues();
            ScheduleRadioBroadcast(currentDay);
        }
        catch (Exception exception)
        {
            ReportError(exception.Message);
        }
        finally
        {
            RestorePlayerGravity();
            _isProcessing = false;
        }
    }

    private bool TryResolvePlayerDependencies()
    {
        PersistentPlayerRoot persistentPlayer =
            PersistentPlayerRoot.Instance;

        if (persistentPlayer == null)
        {
            ReportError("PersistentPlayerRoot를 찾을 수 없습니다.");
            return false;
        }

        if (_dayStartSpawnPoint == null)
        {
            ReportError("PlayerSpawnPoint가 설정되지 않았습니다.");
            return false;
        }

        _playerRoot = persistentPlayer;
        _screenFader =
            _playerRoot.GetComponentInChildren<ScreenFader>(true);

        if (_screenFader == null)
        {
            Debug.LogWarning(
                $"{nameof(DaySceneCoordinator)}: " +
                "Persistent Player에서 ScreenFader를 찾을 수 없습니다. " +
                "Fade 없이 계속합니다.",
                this);
        }

        _level2TimePressureEffect =
            _playerRoot.GetComponentInChildren<Level2TimePressureEffect>(true);

        if (_level2TimePressureEffect == null)
        {
            Debug.LogWarning(
                $"{nameof(DaySceneCoordinator)}: " +
                "Persistent Player에서 Level2TimePressureEffect를 " +
                "찾을 수 없습니다.",
                this);
        }
        else
        {
            _level2TimePressureEffect.BindSceneDependencies(
                _dayFlowController,
                _situationSceneLoader);
        }

        return true;
    }

    private void NotifySceneReadyOnce()
    {
        if (_sceneReadyNotified)
        {
            return;
        }

        PlayerReferenceHub playerReferenceHub =
            PlayerReferenceHub.Instance;

        if (playerReferenceHub == null)
        {
            Debug.LogWarning(
                $"{nameof(DaySceneCoordinator)}: " +
                "PlayerReferenceHub를 찾을 수 없습니다.",
                this);
            return;
        }

        playerReferenceHub.NotifySceneReady();
        _sceneReadyNotified = true;
    }

    private async void HandleTransitionRequested(
        DayTransitionReason reason,
        int targetDay)
    {
        if (_isProcessing)
        {
            return;
        }

        _isProcessing = true;
        _dialogueManager?.Stop();
        bool shouldStartNextDay = false;
        bool isEndingTransition = targetDay == DayRunState.ClearDay;

        try
        {
            if (isEndingTransition && string.IsNullOrWhiteSpace(_endingSceneName))
            {
                ReportError("Ending scene name is required.");
                return;
            }

            if (!isEndingTransition)
            {
                SuspendPlayerGravity();
            }

            await RunFadeAsync(_screenFader?.FadeOut(_fadeOutDuration));

            if (!await UnloadCurrentDayAsync())
            {
                return;
            }

            if (isEndingTransition)
            {
                AsyncOperation operation = SceneManager.LoadSceneAsync(
                    _endingSceneName,
                    LoadSceneMode.Single);

                if (operation == null)
                {
                    ReportError($"Failed to load ending scene: {_endingSceneName}");
                    return;
                }

                await AwaitOperationAsync(operation);
                return;
            }

            Debug.Log(
                $"{reason}: {targetDay}일차로 전환합니다.",
                this);
            shouldStartNextDay = true;
        }
        catch (Exception exception)
        {
            ReportError(exception.Message);
        }
        finally
        {
            if (!shouldStartNextDay)
            {
                RestorePlayerGravity();
            }

            // NotifyTransitionCompleted()가 즉시 다음 DayStarted를 발생시키므로
            // 먼저 처리 잠금을 해제한다.
            _isProcessing = false;
        }

        if (shouldStartNextDay &&
            !_dayFlowController.NotifyTransitionCompleted())
        {
            RestorePlayerGravity();
            ReportError("다음 날을 시작하지 못했습니다.");
        }
    }

    private void SuspendPlayerGravity()
    {
        if (_isPlayerGravitySuspended)
        {
            return;
        }

        PersistentPlayerRoot playerRoot =
            _playerRoot != null ? _playerRoot : PersistentPlayerRoot.Instance;

        if (playerRoot == null)
        {
            return;
        }

        _playerGravityProvider =
            playerRoot.GetComponentInChildren<GravityProvider>(true);

        if (_playerGravityProvider == null)
        {
            return;
        }

        _gravityWasEnabled = _playerGravityProvider.useGravity;
        _playerGravityProvider.useGravity = false;
        _playerGravityProvider.ResetFallForce();
        _isPlayerGravitySuspended = true;
    }

    private void RestorePlayerGravity()
    {
        if (!_isPlayerGravitySuspended)
        {
            return;
        }

        if (_playerGravityProvider != null)
        {
            _playerGravityProvider.ResetFallForce();
            _playerGravityProvider.useGravity = _gravityWasEnabled;
        }

        _playerGravityProvider = null;
        _isPlayerGravitySuspended = false;
    }

    private async Task<bool> UnloadCurrentDayAsync()
    {
        bool situationUnloaded =
            await _situationSceneLoader.UnloadAsync();

        if (!situationUnloaded)
        {
            ReportError(_situationSceneLoader.LastError);
            return false;
        }

        await _homeModuleLoader.UnloadAllAsync();

        if (_homeModuleLoader.LoadedModuleSceneNames.Count > 0)
        {
            ReportError(_homeModuleLoader.LastError);
            return false;
        }

        return true;
    }

    private void ScheduleRadioBroadcast(int day)
    {
        if (_radioController == null)
        {
            return;
        }

        StartCoroutine(PlayRadioBroadcastAfterDelay(day));
    }

    private IEnumerator PlayRadioBroadcastAfterDelay(int day)
    {
        if (_radioStartDelay > 0f)
        {
            yield return new WaitForSeconds(_radioStartDelay);
        }

        if (_dayFlowController == null ||
            _dayFlowController.CurrentState != DayFlowState.Playing ||
            _dayFlowController.CurrentDay != day)
        {
            yield break;
        }

        _radioController.PlayForResult(_dayFlowController.LastDayResult);
    }

    private Task RunFadeAsync(IEnumerator fadeRoutine)
    {
        if (fadeRoutine == null)
        {
            return Task.CompletedTask;
        }

        TaskCompletionSource<bool> completionSource = new();
        StartCoroutine(RunFadeCoroutine(fadeRoutine, completionSource));
        return completionSource.Task;
    }

    private static Task AwaitOperationAsync(AsyncOperation operation)
    {
        if (operation.isDone)
        {
            return Task.CompletedTask;
        }

        TaskCompletionSource<bool> completionSource = new();
        operation.completed += _ => completionSource.TrySetResult(true);
        return completionSource.Task;
    }

    private static IEnumerator RunFadeCoroutine(
        IEnumerator fadeRoutine,
        TaskCompletionSource<bool> completionSource)
    {
        yield return fadeRoutine;
        completionSource.TrySetResult(true);
    }

    private void ReportError(string message)
    {
        Debug.LogError($"DaySceneCoordinator: {message}", this);
    }
}
