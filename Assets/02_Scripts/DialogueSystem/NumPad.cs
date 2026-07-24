using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class NumPad : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private int _maxLength = 10;
    [SerializeField] private string _correctNumber = "119";

    [Header("Buttons")]
    [Tooltip("Assign buttons in digit order: 0, 1, 2, ... 9.")]
    [SerializeField] private Button[] _numberButtons = new Button[10];
    [SerializeField] private Button _starButton;
    [SerializeField] private Button _hashButton;
    [SerializeField] private Button _deleteButton;
    [SerializeField] private Button _callButton;

    [Header("Events")]
    [SerializeField] private UnityEvent _onCorrectNumber;
    [SerializeField] private UnityEvent _onWrongNumber;

    private UnityAction[] _numberButtonActions;

    private void Awake()
    {
        ConfigureInputField();
        CreateNumberButtonActions();
    }

    private void OnEnable()
    {
        RegisterButtonListeners();
    }

    private void OnDisable()
    {
        UnregisterButtonListeners();
    }

    private void OnValidate()
    {
        _maxLength = Mathf.Max(0, _maxLength);

        if (_inputField != null)
        {
            ConfigureInputField();
        }
    }

    public void AppendNumber(int number)
    {
        if (number < 0 || number > 9)
        {
            Debug.LogWarning($"{nameof(NumPad)} only accepts single digit numbers.", this);
            return;
        }

        AppendKey(number.ToString());
    }

    public void AppendStar()
    {
        AppendKey("*");
    }

    public void AppendHash()
    {
        AppendKey("#");
    }

    public void DeleteLastDigit()
    {
        if (_inputField == null || string.IsNullOrEmpty(_inputField.text))
        {
            return;
        }

        _inputField.text = _inputField.text.Substring(0, _inputField.text.Length - 1);
    }

    public void Call()
    {
        if (_inputField == null)
        {
            Debug.LogWarning($"{nameof(NumPad)} needs an input field before calling.", this);
            _onWrongNumber?.Invoke();
            return;
        }

        if (_inputField.text == _correctNumber)
        {
            _onCorrectNumber?.Invoke();
            return;
        }

        _onWrongNumber?.Invoke();
    }

    private void ConfigureInputField()
    {
        if (_inputField == null)
        {
            return;
        }

        _inputField.characterLimit = _maxLength;
        _inputField.readOnly = true;
    }

    private void CreateNumberButtonActions()
    {
        if (_numberButtons == null)
        {
            _numberButtons = new Button[10];
        }

        _numberButtonActions = new UnityAction[_numberButtons.Length];

        for (int i = 0; i < _numberButtons.Length; i++)
        {
            int digit = i;
            _numberButtonActions[i] = () => AppendNumber(digit);
        }
    }

    private void RegisterButtonListeners()
    {
        if (_numberButtons == null)
        {
            Debug.LogWarning($"{nameof(NumPad)} needs number buttons assigned in 0-9 order.", this);
            return;
        }

        if (_numberButtonActions == null || _numberButtonActions.Length != _numberButtons.Length)
        {
            CreateNumberButtonActions();
        }

        if (_numberButtons.Length != 10)
        {
            Debug.LogWarning($"{nameof(NumPad)} expects exactly 10 number buttons assigned in 0-9 order.", this);
        }

        for (int i = 0; i < _numberButtons.Length && i < _numberButtonActions.Length; i++)
        {
            if (_numberButtons[i] != null)
            {
                _numberButtons[i].onClick.AddListener(_numberButtonActions[i]);
            }
        }

        if (_deleteButton != null)
        {
            _deleteButton.onClick.AddListener(DeleteLastDigit);
        }

        if (_starButton != null)
        {
            _starButton.onClick.AddListener(AppendStar);
        }

        if (_hashButton != null)
        {
            _hashButton.onClick.AddListener(AppendHash);
        }

        if (_callButton != null)
        {
            _callButton.onClick.AddListener(Call);
        }
    }

    private void UnregisterButtonListeners()
    {
        if (_numberButtons != null && _numberButtonActions != null)
        {
            for (int i = 0; i < _numberButtons.Length && i < _numberButtonActions.Length; i++)
            {
                if (_numberButtons[i] != null)
                {
                    _numberButtons[i].onClick.RemoveListener(_numberButtonActions[i]);
                }
            }
        }

        if (_deleteButton != null)
        {
            _deleteButton.onClick.RemoveListener(DeleteLastDigit);
        }

        if (_starButton != null)
        {
            _starButton.onClick.RemoveListener(AppendStar);
        }

        if (_hashButton != null)
        {
            _hashButton.onClick.RemoveListener(AppendHash);
        }

        if (_callButton != null)
        {
            _callButton.onClick.RemoveListener(Call);
        }
    }

    private void AppendKey(string key)
    {
        if (_inputField == null)
        {
            Debug.LogWarning($"{nameof(NumPad)} needs an input field before entering numbers.", this);
            return;
        }

        if (_inputField.text.Length >= _maxLength)
        {
            return;
        }

        _inputField.text += key;
    }
}
