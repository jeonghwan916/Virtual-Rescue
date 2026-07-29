using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VirtualRescue.Effects;
using VirtualRescue.Loading;
using VirtualRescue.Player;

public class MenuFollowerController : MonoBehaviour
{
    [SerializeField] private string _returnToLobbyButtonName = "Button-ReturnToLobby";
    [SerializeField] private string _settingButtonName = "Button-Setting";
    [SerializeField] private string _closeButtonName = "Button-Close";
    [SerializeField] private string _leftControllerXButtonPath = "<XRController>{LeftHand}/{PrimaryButton}";
    [SerializeField] private string _lobbySceneName = "BuildTest_Lobby";
    [SerializeField] private string _loadingSceneName = "LoadingScene";
    [SerializeField] private float _fadeDuration = 1f;
    [SerializeField] private bool _hideOnStart = true;

    private Coroutine _returnRoutine;

    private void Awake()
    {
        BindButton(_returnToLobbyButtonName, ReturnToLobby);
        BindButton(_settingButtonName, OpenSetting);
        BindButton(_closeButtonName, Close);
        MenuFollowerInputDriver.Register(this, _leftControllerXButtonPath);
    }

    private void Start()
    {
        if (_hideOnStart)
        {
            Hide();
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
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

        _returnRoutine = StartCoroutine(ReturnToLobbyRoutine());
    }

    public void OpenSetting()
    {
    }

    private IEnumerator ReturnToLobbyRoutine()
    {
        LoadingRequest.Set(_lobbySceneName, -1, null);

        ScreenFader screenFader = FindScreenFader();
        if (screenFader != null)
        {
            yield return screenFader.FadeOut(_fadeDuration);
        }

        AsyncOperation loadingOperation = SceneManager.LoadSceneAsync(_loadingSceneName, LoadSceneMode.Single);
        if (loadingOperation == null)
        {
            Debug.LogWarning($"Failed to load loading scene: {_loadingSceneName}", this);
            LoadingRequest.Clear();
            _returnRoutine = null;
            yield break;
        }

        yield return loadingOperation;
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private void BindButton(string buttonName, UnityEngine.Events.UnityAction action)
    {
        Transform buttonTransform = FindChildTransform(transform, buttonName);
        if (buttonTransform == null)
        {
            Debug.LogWarning($"{nameof(MenuFollowerController)} could not find {buttonName}.", this);
            return;
        }

        Button button = buttonTransform.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogWarning($"{buttonName} does not have a Button component.", this);
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

    private static Transform FindChildTransform(Transform root, string transformName)
    {
        if (root.name == transformName)
        {
            return root;
        }

        foreach (Transform child in root)
        {
            Transform found = FindChildTransform(child, transformName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
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

        _menuFollower.Show();
    }
}
