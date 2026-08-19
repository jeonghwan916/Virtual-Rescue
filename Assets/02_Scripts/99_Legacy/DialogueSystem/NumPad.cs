using System;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using VirtualRescue.GameFlow;

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
        public BoxCollider TouchArea;
        public KeyType Type;
        public int Digit;
    }

    [Header("Input")]
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private int _maxLength = 10;
    [SerializeField] private string _correctNumber = "119";

    [Header("Keys")]
    [SerializeField] private KeyBinding[] _keyBindings;
    [SerializeField] private LayerMask _touchLayerMask = 1 << 12;
    [SerializeField] private float _touchDebounceTime = 0.15f;

    [Header("Haptics")]
    [SerializeField] private HapticImpulsePlayer _hapticPlayer;
    [SerializeField] private float _hapticAmplitude = 0.3f;
    [SerializeField] private float _hapticDuration = 0.05f;

    [Header("Exit Controller")]
    [SerializeField] private ExitController _exitController;

    private readonly Collider[] _touchHits = new Collider[8];
    private bool _isInputLocked;
    private bool _isTouchActive;
    private float _nextTouchTime;

    public event Action OnCorrectNumber;
    public event Action OnWrongNumber;

    private void Awake()
    {
        ConfigureInputField();
    }

    private void Update()
    {
        UpdateTouchKeys();
    }

    private void OnValidate()
    {
        _maxLength = Mathf.Max(0, _maxLength);
        _touchDebounceTime = Mathf.Max(0f, _touchDebounceTime);

        if (_inputField != null)
        {
            ConfigureInputField();
        }
    }

    public void AppendNumber(int number)
    {
        if (_isInputLocked)
        {
            return;
        }

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
        if (_isInputLocked)
        {
            return;
        }

        PlayHaptic();

        AppendKey("*");
    }

    public void AppendHash()
    {
        if (_isInputLocked)
        {
            return;
        }

        PlayHaptic();

        AppendKey("#");
    }

    public void DeleteLastDigit()
    {
        if (_isInputLocked)
        {
            return;
        }

        PlayHaptic();

        if (_inputField == null || string.IsNullOrEmpty(_inputField.text))
        {
            return;
        }

        _inputField.text = _inputField.text.Substring(0, _inputField.text.Length - 1);
    }

    public void Call()
    {
        if (_isInputLocked)
        {
            return;
        }

        PlayHaptic();

        if (_inputField == null)
        {
            Debug.LogWarning($"{nameof(NumPad)} needs an input field before calling.", this);
            IsNumberCorrect(false);
            return;
        }

        if (_inputField.text == _correctNumber)
        {
            IsNumberCorrect(true);
            return;
        }

        IsNumberCorrect(false);
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

    private void UpdateTouchKeys()
    {
        if (_isInputLocked || _keyBindings == null)
        {
            return;
        }

        KeyBinding touchedBinding = null;

        foreach (KeyBinding binding in _keyBindings)
        {
            if (binding?.TouchArea == null)
            {
                continue;
            }

            if (IsTouching(binding.TouchArea) && touchedBinding == null)
            {
                touchedBinding = binding;
            }
        }

        if (touchedBinding == null)
        {
            _isTouchActive = false;
            return;
        }

        if (_isTouchActive || Time.time < _nextTouchTime)
        {
            _isTouchActive = true;
            return;
        }

        ExecuteKey(touchedBinding);
        _isTouchActive = true;
        _nextTouchTime = Time.time + _touchDebounceTime;
    }

    private bool IsTouching(BoxCollider touchArea)
    {
        Transform touchTransform = touchArea.transform;
        Vector3 center = touchTransform.TransformPoint(touchArea.center);
        Vector3 halfExtents = Vector3.Scale(touchArea.size * 0.5f, Abs(touchTransform.lossyScale));
        int hitCount = Physics.OverlapBoxNonAlloc(
            center,
            halfExtents,
            _touchHits,
            touchTransform.rotation,
            _touchLayerMask,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < hitCount; i++)
        {
            if (_touchHits[i] != touchArea)
            {
                return true;
            }
        }

        return false;
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

    private static Vector3 Abs(Vector3 value)
    {
        return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
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

    public void IsNumberCorrect(bool flag)
    {
        if (flag)
        {
            _isInputLocked = true;
            Debug.Log("OnCorrectNumber");

            if (_inputField != null && _inputField.textComponent != null)
            {
                _inputField.textComponent.color = Color.green;
            }

            OnCorrectNumber?.Invoke();
            
            _exitController.RequestExit(); // 고민중
        }
        else
        {
            Debug.Log("OnWrongNumber");
            OnWrongNumber?.Invoke();
        }
    }
    
}
