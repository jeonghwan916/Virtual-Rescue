using UnityEngine;

namespace VirtualRescue.Missions06
{
    [RequireComponent(typeof(Collider))]
    public sealed class SmokeEvacuationQuestTrigger : MonoBehaviour
    {
        [SerializeField] private SmokeEvacuationQuestManager _questManager;
        [SerializeField] private SmokeEvacuationQuestTriggerType _triggerType;

        private bool _hasTriggered;

        private void Awake()
        {
            if (_questManager == null)
            {
                _questManager = GetComponentInParent<SmokeEvacuationQuestManager>();
            }

            Collider triggerCollider = GetComponent<Collider>();
            if (!triggerCollider.isTrigger)
            {
                Debug.LogWarning("06 연기 대피 퀘스트 트리거의 Collider에서 Is Trigger를 활성화하세요.", this);
            }

            if (_questManager == null)
            {
                Debug.LogWarning("06 연기 대피 트리거의 상위 오브젝트에서 전용 QuestManager를 찾을 수 없습니다.", this);
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

            if (_questManager.TryTrigger(_triggerType))
            {
                _hasTriggered = true;
            }
        }
    }
}
