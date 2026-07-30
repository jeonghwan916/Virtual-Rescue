using UnityEngine;

namespace VirtualRescue.CallRefuge
{
    public class CallRefugeQuestTrigger : MonoBehaviour
    {
        [SerializeField] private CallRefugeQuestManager _questManager;
        [SerializeField] private CallRefugeQuestStep _questStep;

        private bool _hasTriggered;

        private void Awake()
        {
            if (_questManager == null)
            {
                _questManager = GetComponentInParent<CallRefugeQuestManager>();
            }

            Collider triggerCollider = GetComponent<Collider>();
            if (!triggerCollider.isTrigger)
            {
                Debug.LogWarning(
                    "Call Refuge 트리거의 Collider에서 Is Trigger를 활성화하세요.",
                    this);
            }

            if (_questManager == null)
            {
                Debug.LogWarning(
                    "Call Refuge 트리거에서 퀘스트 매니저를 찾을 수 없습니다.",
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

            if (_questManager.TryAdvance(_questStep))
            {
                _hasTriggered = true;
            }
        }
    }
}
