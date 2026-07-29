using UnityEngine;
using VirtualRescue.Missions09;

namespace VirtualRescue.Missions07
{
    public class Mission07References : MonoBehaviour
    {
        private static Mission07References Current { get; set; }
        
        public bool IsLoaded = false;
        
        [Header("References")]
        [SerializeField] private FireExitDoorController _fireExitDoorController;
        public static FireExitDoorController FireExitDoorController => Current != null ? Current._fireExitDoorController : null;
        
        private void OnEnable()
        {
            Current = this;
            IsLoaded = true;
        }

        private void OnDisable()
        {
            if (Current == this)
            {
                Current = null;
                IsLoaded = false;
            }
        }
    }
}
