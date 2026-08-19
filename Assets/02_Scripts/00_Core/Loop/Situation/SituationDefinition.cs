using System.Collections.Generic;
using UnityEngine;

namespace VirtualRescue.GameFlow
{
    public enum SituationLevel
    {
        Level0 = 0,
        Level1 = 1,
        Level2 = 2
    }

    [CreateAssetMenu(
        fileName = "SituationDefinition",
        menuName = "Virtual Rescue/Game Flow/Situation Definition")]
    public sealed class SituationDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _id = string.Empty;
        [SerializeField] private SituationLevel _level = SituationLevel.Level0;

        [Header("Selection")]
        [Min(1)]
        [SerializeField] private int _weight = 1;
        [Range(DayRunState.FirstDay, DayRunState.ClearDay - 1)]
        [SerializeField] private int _minimumDay = DayRunState.FirstDay;

        [Header("Scene")]
        [SerializeField] private string _sceneName = string.Empty;
        [SerializeField] private RoomLocation _roomTrigger = RoomLocation.None;

        [Header("Dialogue")]
        [SerializeField] private string _resolvedDialogueId = string.Empty;
        [SerializeField] private string _failedDialogueId = string.Empty;
        [SerializeField] private string _beforeResolveCallingDialogueGroupId = string.Empty;
        [SerializeField] private string _afterResolveCallingDialogueGroupId = string.Empty;
        [SerializeField] private string _level2CallingDialogueGroupId = string.Empty;

        [Header("Level 2 Rules")]
        [SerializeField] private bool _usesTimeLimit;
        [Min(0f)]
        [SerializeField] private float _timeLimitSeconds = 60f;
        [SerializeField] private List<ExitType> _allowedExits = new();

        public string Id => _id;
        public SituationLevel Level => _level;
        public int Weight => _weight;
        public int MinimumDay => _minimumDay;
        public string SceneName => _sceneName;
        public RoomLocation RoomLocation => _roomTrigger;
        public string ResolvedDialogueId => _resolvedDialogueId;
        public string FailedDialogueId => _failedDialogueId;
        public string BeforeResolveCallingDialogueGroupId =>
            _beforeResolveCallingDialogueGroupId;
        public string AfterResolveCallingDialogueGroupId =>
            _afterResolveCallingDialogueGroupId;
        public string Level2CallingDialogueGroupId =>
            _level2CallingDialogueGroupId;
        public bool UsesTimeLimit =>
            _level == SituationLevel.Level2 && _usesTimeLimit;
        public float TimeLimitSeconds => UsesTimeLimit ? _timeLimitSeconds : 0f;
        public IReadOnlyList<ExitType> AllowedExits => _allowedExits;

        public bool IsExitAllowed(ExitType exitType)
        {
            return _allowedExits != null &&
                   _allowedExits.Contains(exitType);
        }

        private void OnValidate()
        {
            _id = _id?.Trim() ?? string.Empty;
            _weight = Mathf.Max(1, _weight);
            _resolvedDialogueId = _resolvedDialogueId?.Trim() ?? string.Empty;
            _failedDialogueId = _failedDialogueId?.Trim() ?? string.Empty;
            _beforeResolveCallingDialogueGroupId =
                _beforeResolveCallingDialogueGroupId?.Trim() ?? string.Empty;
            _afterResolveCallingDialogueGroupId =
                _afterResolveCallingDialogueGroupId?.Trim() ?? string.Empty;
            _level2CallingDialogueGroupId =
                _level2CallingDialogueGroupId?.Trim() ?? string.Empty;
            _minimumDay = Mathf.Clamp(
                _minimumDay,
                DayRunState.FirstDay,
                DayRunState.ClearDay - 1);

            if (string.IsNullOrEmpty(_id))
            {
                Debug.LogWarning(
                    $"{name}: Situation ID is required.",
                    this);
            }

            if (_level != SituationLevel.Level2)
            {
                return;
            }

            if (UsesTimeLimit && _timeLimitSeconds <= 0f)
            {
                Debug.LogWarning(
                    $"{name}: Timed Level 2 situation requires a positive time limit.",
                    this);
            }

            if (_allowedExits == null || _allowedExits.Count == 0)
            {
                Debug.LogWarning(
                    $"{name}: Level 2 situation requires at least one allowed exit.",
                    this);
            }

            if (_allowedExits != null &&
                _allowedExits.Contains(ExitType.Elevator))
            {
                Debug.LogWarning(
                    $"{name}: Elevator cannot be an allowed exit for a Level 2 situation.",
                    this);
            }
        }
    }
}
