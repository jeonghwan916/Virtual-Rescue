using UnityEngine;

namespace VirtualRescue.Missions09
{
    [RequireComponent(typeof(Collider))]
    public sealed class RefugeAreaQuestTrigger : MonoBehaviour
    {
        [SerializeField] private RefugeAreaQuestManager _questManager;
        [SerializeField] private RefugeAreaQuestTriggerType _triggerType;

        private bool _hasTriggered;

        private void Awake()
        {
            if (_questManager == null)
            {
                _questManager = GetComponentInParent<RefugeAreaQuestManager>();
            }

            if (_questManager == null)
            {
                _questManager = FindFirstObjectByType<RefugeAreaQuestManager>();
            }

            Collider triggerCollider = GetComponent<Collider>();
            if (!triggerCollider.isTrigger)
            {
                Debug.LogWarning("대피공간 퀘스트 트리거의 Collider에서 Is Trigger를 활성화하세요.", this);
            }

            if (_questManager == null)
            {
                Debug.LogWarning("대피공간 퀘스트 매니저를 찾을 수 없습니다.", this);
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
