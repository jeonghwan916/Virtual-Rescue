using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualRescue.QuestGuide
{
    [Serializable]
    public sealed class GuideOption
    {
        [SerializeField] private string _label;
        [SerializeField] private string _actionId;

        public string Label => _label;
        public string ActionId => _actionId;
        public bool IsValid => !string.IsNullOrWhiteSpace(_label) &&
                               !string.IsNullOrWhiteSpace(_actionId);
    }

    [Serializable]
    public sealed class GuidePage
    {
        [SerializeField] private string _title;
        [SerializeField] private Sprite _image;
        [SerializeField] private string _explain;
        [SerializeField] private List<GuideOption> _options = new();

        public string Title => _title;
        public Sprite Image => _image;
        public string Explain => _explain;
        public IReadOnlyList<GuideOption> Options => _options;
    }

    [CreateAssetMenu(
        fileName = "GuideSequence",
        menuName = "Virtual Rescue/Quest Guide/Guide Sequence")]
    public sealed class GuideSequence : ScriptableObject
    {
        private const int MaxOptionCount = 3;

        [SerializeField] private List<GuidePage> _pages = new();

        public IReadOnlyList<GuidePage> Pages => _pages;

        private void OnValidate()
        {
            if (_pages == null || _pages.Count == 0)
            {
                Debug.LogWarning($"{name} has no guide pages.", this);
                return;
            }

            for (int pageIndex = 0; pageIndex < _pages.Count; pageIndex++)
            {
                GuidePage page = _pages[pageIndex];
                if (page == null)
                {
                    Debug.LogWarning($"{name} has a missing page at index {pageIndex}.", this);
                    continue;
                }

                IReadOnlyList<GuideOption> options = page.Options;
                if (options == null)
                {
                    continue;
                }

                if (options.Count > MaxOptionCount)
                {
                    Debug.LogWarning(
                        $"{name} page {pageIndex} has {options.Count} options. " +
                        $"Only the first {MaxOptionCount} valid options will be displayed.",
                        this);
                }

                for (int optionIndex = 0; optionIndex < options.Count; optionIndex++)
                {
                    GuideOption option = options[optionIndex];
                    if (option == null || !option.IsValid)
                    {
                        Debug.LogWarning(
                            $"{name} page {pageIndex} has an invalid option at index {optionIndex}. " +
                            "Both Label and Action ID are required.",
                            this);
                    }
                }
            }
        }
    }
}
