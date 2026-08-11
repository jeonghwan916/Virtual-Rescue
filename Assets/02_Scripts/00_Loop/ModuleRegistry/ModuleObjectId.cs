using UnityEngine;

namespace VirtualRescue.GameFlow
{
    [CreateAssetMenu(
        fileName = "ModuleObjectId",
        menuName = "Virtual Rescue/Game Flow/Module Object Id")]
    public sealed class ModuleObjectId : ScriptableObject
    {
        [SerializeField] private string _id;

        public string Id => ModuleObjectRegistry.NormalizeId(_id);
        public bool IsValid => !string.IsNullOrEmpty(Id);

        private void OnValidate()
        {
            _id = ModuleObjectRegistry.NormalizeId(_id);
        }
    }
}
