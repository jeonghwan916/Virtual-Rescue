using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using VirtualRescue.Effects;
using VirtualRescue.Player;

public class MenuFollowerController : MonoBehaviour
{
    [SerializeField] private Button _returnToLobbyButton;
    [SerializeField] private Button _settingButton;
    [SerializeField] private Button _closeButton;
    [SerializeField] private GameObject _menuFollowerPanel;
    [SerializeField] private GameObject _settingUI;
    [SerializeField] private string _leftControllerXButtonPath = "<XRController>{LeftHand}/{PrimaryButton}";
    [SerializeField] private string _lobbySceneName = "LobbyScene_Unchecked";
    [SerializeField] private float _fadeDuration = 1f;
    [SerializeField] private bool _hideOnStart = true;

    private const float MenuOpenFarDistance = 10f;
    private const float MenuClosedFarDistance = 0.2f;

    private Coroutine _returnRoutine;

    private void Awake()
    {
        BindButton(_returnToLobbyButton, ReturnToLobby);
        BindButton(_settingButton, OpenSetting);
        BindButton(_closeButton, Close);
        MenuFollowerInputDriver.Register(this, _leftControllerXButtonPath);
    }

    private void Start()
    {
        if (_hideOnStart)
        {
            Hide();
            return;
        }

        ApplyMenuInteraction(true);
    }

    private void OnDisable()
    {
        ApplyMenuInteraction(false);
    }

    public void Show()
    {
        if (_returnRoutine != null)
        {
            return;
        }

        SetMenuOpen(true);
    }

    public void Toggle()
    {
        if (_returnRoutine != null)
        {
            return;
        }

        SetMenuOpen(!gameObject.activeSelf);
    }

    public void Close()
    {
        Hide();
    }

    public void ReturnToLobby()
    {
        if (_returnRoutine != null)
        {
            return;
        }

        _returnRoutine = MenuFollowerInputDriver.StartRoutine(ReturnToLobbyRoutine());
        Hide();
    }

    public void OpenSetting()
    {
        if (_returnRoutine != null)
        {
            return;
        }

        SetActive(_menuFollowerPanel, false);
        SetActive(_settingUI, true);
    }

    public void CloseSetting()
    {
        if (_returnRoutine != null)
        {
            return;
        }

        SetActive(_settingUI, false);
        SetActive(_menuFollowerPanel, true);
        ApplyMenuInteraction(true);
    }

    private IEnumerator ReturnToLobbyRoutine()
    {
        ScreenFader screenFader = FindScreenFader();
        if (screenFader != null)
        {
            yield return screenFader.FadeOut(_fadeDuration);
        }

        AsyncOperation loadingOperation = SceneManager.LoadSceneAsync(_lobbySceneName, LoadSceneMode.Single);
        if (loadingOperation == null)
        {
            Debug.LogWarning($"Failed to load lobby scene: {_lobbySceneName}", this);
            _returnRoutine = null;
            yield break;
        }

        yield return loadingOperation;
    }

    private void Hide()
    {
        SetMenuOpen(false);
    }

    private void SetMenuOpen(bool isOpen)
    {
        ApplyMenuInteraction(isOpen);

        if (isOpen)
        {
            gameObject.SetActive(true);
            SetActive(_menuFollowerPanel, true);
            SetActive(_settingUI, false);
            return;
        }

        SetActive(_menuFollowerPanel, false);
        SetActive(_settingUI, false);
        gameObject.SetActive(false);
    }

    private void ApplyMenuInteraction(bool isOpen)
    {
        PlayerReferenceHub referenceHub = PlayerReferenceHub.Instance;
        if (referenceHub == null)
        {
            Debug.LogWarning(
                $"{nameof(MenuFollowerController)} could not find {nameof(PlayerReferenceHub)}.",
                this);
            return;
        }

        float farDistance = isOpen
            ? MenuOpenFarDistance
            : MenuClosedFarDistance;

        SetFarDistance(referenceHub.LeftNearFarInteractor, farDistance);
        SetFarDistance(referenceHub.RightNearFarInteractor, farDistance);
        SetActive(referenceHub.LeftLineVisual, isOpen);
        SetActive(referenceHub.RightLineVisual, isOpen);
    }

    private static void SetFarDistance(NearFarInteractor interactor, float distance)
    {
        if (interactor != null &&
            interactor.farInteractionCaster is CurveInteractionCaster caster)
        {
            caster.castDistance = distance;
        }
    }

    private static void SetActive(GameObject target, bool isActive)
    {
        if (target != null)
        {
            target.SetActive(isActive);
        }
    }

    private void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            Debug.LogWarning($"{nameof(MenuFollowerController)} has a missing button reference.", this);
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static ScreenFader FindScreenFader()
    {
        if (PersistentPlayerRoot.Instance != null)
        {
            ScreenFader playerFader = PersistentPlayerRoot.Instance.GetComponentInChildren<ScreenFader>(true);
            if (playerFader != null)
            {
                return playerFader;
            }
        }

        return FindFirstObjectByType<ScreenFader>(FindObjectsInactive.Include);
    }
}

internal sealed class MenuFollowerInputDriver : MonoBehaviour
{
    private static MenuFollowerInputDriver _instance;

    private MenuFollowerController _menuFollower;
    private InputAction _toggleAction;
    private string _buttonPath;

    public static void Register(MenuFollowerController menuFollower, string buttonPath)
    {
        if (menuFollower == null)
        {
            return;
        }

        if (_instance == null)
        {
            GameObject driverObject = new GameObject(nameof(MenuFollowerInputDriver));
            DontDestroyOnLoad(driverObject);
            _instance = driverObject.AddComponent<MenuFollowerInputDriver>();
        }

        _instance.SetMenuFollower(menuFollower, buttonPath);
    }

    public static Coroutine StartRoutine(IEnumerator routine)
    {
        if (_instance == null || routine == null)
        {
            return null;
        }

        return _instance.StartCoroutine(routine);
    }

    private void OnDestroy()
    {
        if (_toggleAction != null)
        {
            _toggleAction.performed -= OnTogglePerformed;
            _toggleAction.Dispose();
        }

        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void SetMenuFollower(MenuFollowerController menuFollower, string buttonPath)
    {
        _menuFollower = menuFollower;

        if (_buttonPath == buttonPath && _toggleAction != null)
        {
            return;
        }

        if (_toggleAction != null)
        {
            _toggleAction.performed -= OnTogglePerformed;
            _toggleAction.Dispose();
        }

        _buttonPath = buttonPath;
        _toggleAction = new InputAction("Toggle MenuFollower", InputActionType.Button, _buttonPath);
        _toggleAction.performed += OnTogglePerformed;
        _toggleAction.Enable();
    }

    private void OnTogglePerformed(InputAction.CallbackContext context)
    {
        if (_menuFollower == null)
        {
            return;
        }

        _menuFollower.Toggle();
    }
}
