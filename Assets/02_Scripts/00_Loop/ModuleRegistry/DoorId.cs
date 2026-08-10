using UnityEngine;

namespace VirtualRescue.GameFlow
{
    [CreateAssetMenu(
        fileName = "DoorId",
        menuName = "Virtual Rescue/Game Flow/Door Id")]
    public sealed class DoorId : ScriptableObject
    {
        [SerializeField] private string _id;

        public string Id => DoorRegistry.NormalizeId(_id);
        public bool IsValid => !string.IsNullOrEmpty(Id);

        private void OnValidate()
        {
            _id = DoorRegistry.NormalizeId(_id);
        }
    }
}
