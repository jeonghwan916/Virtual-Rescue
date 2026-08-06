using System;
using UnityEngine;

namespace VirtualRescue.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class ExitController : MonoBehaviour
    {
        [SerializeField] private ExitType _exitType = ExitType.Elevator;

        public static event Action<ExitType> ExitRequested;

        public ExitType Type => _exitType;

        public void RequestExit()
        {
            ExitRequested?.Invoke(_exitType);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticEvents()
        {
            ExitRequested = null;
        }
    }
}
