using System;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class NumPad : MonoBehaviour
{
    private enum KeyType
    {
        Digit,
        Star,
        Hash,
        Delete,
        Call
    }

    [Serializable]
    private sealed class KeyBinding
    {
        public XRSimpleInteractable Interactable;
        public KeyType Type;
        public int Digit;
    }

    [Header("Input")]
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private int _maxLength = 10;
    [SerializeField] private string _correctNumber = "119";

    [Header("Keys")]
    [SerializeField] private KeyBinding[] _keyBindings;

    [Header("Haptics")]
    [SerializeField] private HapticImpulsePlayer _hapticPlayer;
    [SerializeField] private float _hapticAmplitude = 0.3f;
    [SerializeField] private float _hapticDuration = 0.05f;

    public event Action OnCorrectNumber;
    public event Action OnWrongNumber;

    private void Awake()
    {
        ConfigureInputField();
    }

    private void OnEnable()
    {
        RegisterKeyListeners();
    }

    private void OnDisable()
    {
        UnregisterKeyListeners();
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
        PlayHaptic();

        if (number < 0 || number > 9)
        {
            Debug.LogWarning($"{nameof(NumPad)} only accepts single digit numbers.", this);
            return;
        }

        AppendKey(number.ToString());
    }

    public void AppendStar()
    {
        PlayHaptic();

        AppendKey("*");
    }

    public void AppendHash()
    {
        PlayHaptic();

        AppendKey("#");
    }

    public void DeleteLastDigit()
    {
        PlayHaptic();

        if (_inputField == null || string.IsNullOrEmpty(_inputField.text))
        {
            return;
        }

        _inputField.text = _inputField.text.Substring(0, _inputField.text.Length - 1);
    }

    public void Call()
    {
        PlayHaptic();

        if (_inputField == null)
        {
            Debug.LogWarning($"{nameof(NumPad)} needs an input field before calling.", this);
            IsNumberIsCorrect(true);
            return;
        }

        if (_inputField.text == _correctNumber)
        {
            IsNumberIsCorrect(true);
            return;
        }

        IsNumberIsCorrect(false);
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

    private void RegisterKeyListeners()
    {
        if (_keyBindings == null)
        {
            return;
        }

        foreach (KeyBinding binding in _keyBindings)
        {
            if (binding?.Interactable != null)
            {
                binding.Interactable.selectEntered.AddListener(HandleKeySelected);
            }
        }
    }

    private void UnregisterKeyListeners()
    {
        if (_keyBindings == null)
        {
            return;
        }

        foreach (KeyBinding binding in _keyBindings)
        {
            if (binding?.Interactable != null)
            {
                binding.Interactable.selectEntered.RemoveListener(HandleKeySelected);
            }
        }
    }

    private void HandleKeySelected(SelectEnterEventArgs args)
    {
        if (_keyBindings == null || args.interactableObject == null)
        {
            return;
        }

        foreach (KeyBinding binding in _keyBindings)
        {
            if (binding?.Interactable == null || !ReferenceEquals(binding.Interactable, args.interactableObject))
            {
                continue;
            }

            ExecuteKey(binding);
            return;
        }
    }

    private void ExecuteKey(KeyBinding binding)
    {
        switch (binding.Type)
        {
            case KeyType.Digit:
                AppendNumber(binding.Digit);
                break;
            case KeyType.Star:
                AppendStar();
                break;
            case KeyType.Hash:
                AppendHash();
                break;
            case KeyType.Delete:
                DeleteLastDigit();
                break;
            case KeyType.Call:
                Call();
                break;
            default:
                Debug.LogWarning($"{nameof(NumPad)} received an unsupported key type.", this);
                break;
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

    private void PlayHaptic()
    {
        if (_hapticPlayer == null)
        {
            return;
        }

        _hapticPlayer.SendHapticImpulse(_hapticAmplitude, _hapticDuration);
    }

    public void IsNumberIsCorrect(bool flag)
    {
        if (flag)
        {
            Debug.Log("OnCorrectNumber");

            if (_inputField != null && _inputField.textComponent != null)
            {
                _inputField.textComponent.color = Color.green;
            }
            // todo : 인풋필드 내 숫자 초록색으로 변경
            OnCorrectNumber?.Invoke();
        }
        else
        {
            Debug.Log("OnWrongNumber");
            OnWrongNumber?.Invoke();
        }
    }
    
}
