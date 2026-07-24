using UnityEngine;

namespace VirtualRescue.Interaction
{
    public class DoorHandleTemperature : MonoBehaviour
    {
        [SerializeField] private float _temperature = 25f;
        [SerializeField] private float _dangerTemperature = 50f;

        public float Temperature
        {
            get
            {
                return _temperature;
            }
        }

        public bool IsDangerous
        {
            get
            {
                return _temperature >= _dangerTemperature;
            }
        }
    }
}