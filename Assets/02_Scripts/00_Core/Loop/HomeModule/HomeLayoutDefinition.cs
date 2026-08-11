using System.Collections.Generic;
using UnityEngine;

namespace VirtualRescue.GameFlow
{
    [CreateAssetMenu(
        fileName = "HomeLayoutDefinition",
        menuName = "Virtual Rescue/Game Flow/Home Layout Definition")]
    public sealed class HomeLayoutDefinition : ScriptableObject
    {
        [Header("Scene Modules")]
        [SerializeField] private List<string> _moduleSceneNames = new();

        public IReadOnlyList<string> ModuleSceneNames => _moduleSceneNames;
    }
}
