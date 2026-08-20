using System;
using UnityEngine;

namespace VirtualRescue.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class ExitController : MonoBehaviour
    {
        [SerializeField] private ExitType _exitType = ExitType.Elevator;

        public static event Action<ExitType> ExitRequested;
        public static event Func<ExitType, bool> ExitAnimationBlocked;

        public ExitType Type => _exitType;

        public void RequestExit()
        {
            ExitRequested?.Invoke(_exitType);
        }

        public static bool ShouldBlockExitAnimation(ExitType exitType)
        {
            if (ExitAnimationBlocked == null)
            {
                return false;
            }

            foreach (Func<ExitType, bool> handler in
                     ExitAnimationBlocked.GetInvocationList())
            {
                if (handler(exitType))
                {
                    return true;
                }
            }

            return false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticEvents()
        {
            ExitRequested = null;
            ExitAnimationBlocked = null;
        }
    }
}
