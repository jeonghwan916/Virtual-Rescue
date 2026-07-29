using UnityEngine;
using VirtualRescue.SmokeStairs;

namespace VirtualRescue.Missions02
{
    public class Mission02References : MonoBehaviour
    {
        private static Mission02References Current { get; set; }

        public bool IsLoaded = false;
        
        [Header("References")]
        [SerializeField] private SmokeStairsQuestManager _smokeStairsQuestManager;
        public static SmokeStairsQuestManager SmokeStairsQuestManager => Current != null ? Current._smokeStairsQuestManager : null;
        
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
