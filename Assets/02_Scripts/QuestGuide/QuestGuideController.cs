using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VirtualRescue.QuestGuide
{
    [DisallowMultipleComponent]
    public sealed class QuestGuideController : MonoBehaviour
    {
        private const int MaxOptionCount = 3;

        [Header("Content")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private Image _guideImage;
        [SerializeField] private TMP_Text _explainText;

        [Header("Options")]
        [SerializeField] private GameObject _optionsPanel;
        [SerializeField] private Button[] _optionButtons = new Button[MaxOptionCount];
        [SerializeField] private TMP_Text[] _optionTexts = new TMP_Text[MaxOptionCount];

        [Header("Navigation")]
        [SerializeField] private GameObject _waysPanel;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _forwardButton;
        [SerializeField] private TMP_Text _forwardText;

        [Header("Actions")]
        [SerializeField] private QuestButtonOnClickEventAdder _actionHandler;

        private readonly GuideOption[] _visibleOptions = new GuideOption[MaxOptionCount];
        private UnityAction[] _optionButtonActions;
        private GuideSequence _sequence;
        private int _currentPageIndex;
        private bool _listenersRegistered;

        private void Awake()
        {
            RegisterButtonListeners();
        }

        private void OnDestroy()
        {
            UnregisterButtonListeners();
        }

        public void Show(GuideSequence sequence)
        {
            if (sequence == null || sequence.Pages == null || sequence.Pages.Count == 0)
            {
                Debug.LogWarning("Quest guide cannot be shown because its sequence is empty.", this);
                Hide();
                return;
            }

            RegisterButtonListeners();
            _sequence = sequence;
            _currentPageIndex = 0;
            gameObject.SetActive(true);
            RenderCurrentPage();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void RegisterButtonListeners()
        {
            if (_listenersRegistered)
            {
                return;
            }

            _backButton.onClick.AddListener(ShowPreviousPage);
            _forwardButton.onClick.AddListener(ShowNextPageOrEnd);

            int optionSlotCount = GetOptionSlotCount();
            _optionButtonActions = new UnityAction[optionSlotCount];

            for (int index = 0; index < optionSlotCount; index++)
            {
                int slotIndex = index;
                _optionButtonActions[index] = () => SelectOption(slotIndex);
                _optionButtons[index].onClick.AddListener(_optionButtonActions[index]);
            }

            _listenersRegistered = true;
        }

        private void UnregisterButtonListeners()
        {
            if (!_listenersRegistered)
            {
                return;
            }

            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(ShowPreviousPage);
            }

            if (_forwardButton != null)
            {
                _forwardButton.onClick.RemoveListener(ShowNextPageOrEnd);
            }

            if (_optionButtonActions == null || _optionButtons == null)
            {
                return;
            }

            int count = Mathf.Min(_optionButtonActions.Length, _optionButtons.Length);
            for (int index = 0; index < count; index++)
            {
                if (_optionButtons[index] != null && _optionButtonActions[index] != null)
                {
                    _optionButtons[index].onClick.RemoveListener(_optionButtonActions[index]);
                }
            }

            _listenersRegistered = false;
        }

        private void RenderCurrentPage()
        {
            if (_sequence == null ||
                _currentPageIndex < 0 ||
                _currentPageIndex >= _sequence.Pages.Count)
            {
                Debug.LogWarning("Quest guide page index is invalid.", this);
                Hide();
                return;
            }

            GuidePage page = _sequence.Pages[_currentPageIndex];
            if (page == null)
            {
                Debug.LogWarning(
                    $"Quest guide page {_currentPageIndex} is missing.",
                    _sequence);
                Hide();
                return;
            }

            ApplyText(_titleText, page.Title);
            ApplyImage(_guideImage, page.Image);
            ApplyText(_explainText, page.Explain);
            RenderOptions(page);
            RenderNavigation();
        }

        private static void ApplyText(TMP_Text textComponent, string value)
        {
            bool hasValue = !string.IsNullOrWhiteSpace(value);
            textComponent.gameObject.SetActive(hasValue);

            if (hasValue)
            {
                textComponent.text = value;
            }
        }

        private static void ApplyImage(Image imageComponent, Sprite sprite)
        {
            imageComponent.sprite = sprite;
            imageComponent.gameObject.SetActive(sprite != null);
        }

        private void RenderOptions(GuidePage page)
        {
            Array.Clear(_visibleOptions, 0, _visibleOptions.Length);

            int visibleCount = 0;
            IReadOnlyList<GuideOption> options = page.Options;
            if (options != null)
            {
                for (int index = 0;
                     index < options.Count && visibleCount < GetOptionSlotCount();
                     index++)
                {
                    GuideOption option = options[index];
                    if (option == null || !option.IsValid)
                    {
                        continue;
                    }

                    _visibleOptions[visibleCount] = option;
                    _optionTexts[visibleCount].text = option.Label;
                    _optionButtons[visibleCount].gameObject.SetActive(true);
                    visibleCount++;
                }
            }

            for (int index = visibleCount; index < GetOptionSlotCount(); index++)
            {
                _optionButtons[index].gameObject.SetActive(false);
            }

            _optionsPanel.SetActive(visibleCount > 0);
        }

        private void RenderNavigation()
        {
            _waysPanel.SetActive(true);
            _backButton.gameObject.SetActive(_currentPageIndex > 0);

            bool isLastPage = _currentPageIndex == _sequence.Pages.Count - 1;
            _forwardText.text = isLastPage ? "End" : "Forward";
        }

        private void SelectOption(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _visibleOptions.Length)
            {
                return;
            }

            GuideOption option = _visibleOptions[slotIndex];
            if (option != null)
            {
                if (_actionHandler == null)
                {
                    Debug.LogWarning("Quest guide action handler is not assigned.", this);
                    return;
                }

                _actionHandler.HandleGuideAction(option.ActionId);
            }
        }

        private void ShowPreviousPage()
        {
            if (_currentPageIndex <= 0)
            {
                return;
            }

            _currentPageIndex--;
            RenderCurrentPage();
        }

        private void ShowNextPageOrEnd()
        {
            if (_sequence == null)
            {
                return;
            }

            if (_currentPageIndex >= _sequence.Pages.Count - 1)
            {
                Hide();
                return;
            }

            _currentPageIndex++;
            RenderCurrentPage();
        }

        private int GetOptionSlotCount()
        {
            int buttonCount = _optionButtons == null ? 0 : _optionButtons.Length;
            int textCount = _optionTexts == null ? 0 : _optionTexts.Length;
            return Mathf.Min(MaxOptionCount, buttonCount, textCount);
        }
    }
}
