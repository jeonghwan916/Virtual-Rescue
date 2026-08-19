using System;
using UnityEngine;

namespace VirtualRescue.GameFlow
{
    public static class CellPhoneEndpointRegistry
    {
        public static event Action<CellPhoneEndpoint> EndpointRegistered;
        public static event Action<CellPhoneEndpoint> EndpointUnregistered;

        public static void Register(CellPhoneEndpoint endpoint)
        {
            if (endpoint == null)
            {
                return;
            }

            EndpointRegistered?.Invoke(endpoint);
        }

        public static void Unregister(CellPhoneEndpoint endpoint)
        {
            if (endpoint == null)
            {
                return;
            }

            EndpointUnregistered?.Invoke(endpoint);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            EndpointRegistered = null;
            EndpointUnregistered = null;
        }
    }
}
