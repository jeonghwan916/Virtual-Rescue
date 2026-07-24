using UnityEngine;

namespace VirtualRescue.QuestGuide
{
    [DisallowMultipleComponent]
    public sealed class QuestGuideActionRouter : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour[] _handlers;

        public void HandleGuideAction(string actionId)
        {
            if (string.IsNullOrWhiteSpace(actionId))
            {
                return;
            }

            if (_handlers == null || _handlers.Length == 0)
            {
                Debug.LogWarning($"No quest guide action handlers are registered for Action ID: {actionId}", this);
                return;
            }

            for (int index = 0; index < _handlers.Length; index++)
            {
                MonoBehaviour handlerComponent = _handlers[index];
                if (handlerComponent == null)
                {
                    continue;
                }

                if (handlerComponent is not IQuestGuideActionHandler handler)
                {
                    Debug.LogWarning(
                        $"{handlerComponent.GetType().Name} does not implement IQuestGuideActionHandler.",
                        handlerComponent);
                    continue;
                }

                if (!handler.CanHandle(actionId))
                {
                    continue;
                }

                handler.Handle(actionId);
                return;
            }

            Debug.LogWarning($"Unhandled guide Action ID: {actionId}", this);
        }
    }
}
