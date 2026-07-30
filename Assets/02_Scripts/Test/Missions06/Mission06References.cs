using UnityEngine;

namespace VirtualRescue.Missions07
{
    public class Mission06References : MonoBehaviour
    {
        private static Mission06References Current { get; set; }
        
        public bool IsLoaded = false;
        
        
        private void OnEnable()
        {
            Current = this;
        }

        private void OnDisable()
        {
            if (Current == this)
            {
                Current = null;
            }
        }
    }
}