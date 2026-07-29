using UnityEngine;

namespace VirtualRescue.Missions03
{
    [RequireComponent(typeof(Collider))]
    public sealed class FireExtinguisherQuestTrigger : MonoBehaviour
    {
        [SerializeField] private FireExtinguisherQuestManager _questManager;

        private bool _hasTriggered;

        private void Awake()
        {
            if (_questManager == null)
            {
                _questManager = GetComponentInParent<FireExtinguisherQuestManager>();
            }

            Collider triggerCollider = GetComponent<Collider>();
            if (!triggerCollider.isTrigger)
            {
                Debug.LogWarning(
                    "03 소화기 미션 Start Trigger의 Collider에서 Is Trigger를 활성화하세요.",
                    this);
            }

            if (_questManager == null)
            {
                Debug.LogWarning(
                    "03 소화기 미션 Start Trigger에서 QuestManager를 찾을 수 없습니다.",
                    this);
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
