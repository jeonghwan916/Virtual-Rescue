using UnityEngine;

namespace VirtualRescue.Missions07
{
    [RequireComponent(typeof(Collider))]
    public sealed class StairEvacuationQuestTrigger : MonoBehaviour
    {
        [SerializeField] private StairEvacuationQuestManager _questManager;

        private bool _hasTriggered;

        private void Awake()
        {
            if (_questManager == null)
            {
                _questManager = GetComponentInParent<StairEvacuationQuestManager>();
            }

            Collider triggerCollider = GetComponent<Collider>();
            if (!triggerCollider.isTrigger)
            {
                Debug.LogWarning("Mission 07 start collider must be configured as a trigger.", this);
            }

            if (_questManager == null)
            {
                Debug.LogWarning("Mission 07 start trigger could not find its parent quest manager.", this);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasTriggered || _questManager == null)
            {
                return;
            }

            if (other.GetComponentInParent<CharacterController>() == null)
            {
                return;
            }

            if (_questManager.TryStartQuest())
            {
                _hasTriggered = true;
            }
        }
    }
}
