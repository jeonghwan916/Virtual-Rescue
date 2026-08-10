using UnityEngine;
using VirtualRescue.GameFlow;

namespace VirtualRescue.Test.Missions11
{
    [DisallowMultipleComponent]
    public sealed class SituationTestActivator : MonoBehaviour
    {
        [SerializeField] private SituationController _situationController;
        [SerializeField] private SituationDefinition _testDefinition;

        private void Start()
        {
            if (_situationController == null)
            {
                Debug.LogError("A situation controller is required for the test.", this);
                return;
            }

            if (_testDefinition == null)
            {
                Debug.LogError("A situation definition is required for the test.", this);
                return;
            }

            if (!_situationController.Activate(_testDefinition))
            {
                Debug.LogError("The test situation could not be activated.", this);
            }
        }
    }
}
