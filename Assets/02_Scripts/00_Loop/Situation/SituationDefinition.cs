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

        [Header("Level 2 Rules")]
        [Min(0f)]
        [SerializeField] private float _timeLimitSeconds = 60f;
        [SerializeField] private List<ExitType> _level2AllowedExits = new();

        public string Id => _id;
        public SituationLevel Level => _level;
        public int Weight => _weight;
        public int MinimumDay => _minimumDay;
        public string SceneName => _sceneName;
        public bool UsesTimeLimit => _level == SituationLevel.Level2;
        public float TimeLimitSeconds => UsesTimeLimit ? _timeLimitSeconds : 0f;
        public IReadOnlyList<ExitType> Level2AllowedExits => _level2AllowedExits;

        public bool IsExitAllowed(ExitType exitType)
        {
            if (_level != SituationLevel.Level2)
            {
                return exitType == ExitType.Elevator;
            }

            return _level2AllowedExits != null &&
                   exitType != ExitType.Elevator &&
                   _level2AllowedExits.Contains(exitType);
        }

        private void OnValidate()
        {
            _id = _id?.Trim() ?? string.Empty;
            _weight = Mathf.Max(1, _weight);
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

            if (_timeLimitSeconds <= 0f)
            {
                Debug.LogWarning(
                    $"{name}: Level 2 situation requires a positive time limit.",
                    this);
            }

            if (_level2AllowedExits == null || _level2AllowedExits.Count == 0)
            {
                Debug.LogWarning(
                    $"{name}: Level 2 situation requires at least one allowed exit.",
                    this);
            }

            if (_level2AllowedExits != null &&
                _level2AllowedExits.Contains(ExitType.Elevator))
            {
                Debug.LogWarning(
                    $"{name}: Elevator cannot be an allowed exit for a Level 2 situation.",
                    this);
            }
        }
    }
}
