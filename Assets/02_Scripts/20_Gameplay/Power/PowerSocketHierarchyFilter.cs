using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VirtualRescue.Gameplay.Power
{
    [RequireComponent(typeof(XRSocketInteractor))]
    public sealed class PowerSocketHierarchyFilter : MonoBehaviour,
        IXRHoverFilter,
        IXRSelectFilter
    {
        private XRSocketInteractor _socketInteractor;

        public bool canProcess => isActiveAndEnabled;

        private void Awake()
        {
            _socketInteractor = GetComponent<XRSocketInteractor>();
        }

        private void OnEnable()
        {
            if (_socketInteractor == null)
            {
                _socketInteractor = GetComponent<XRSocketInteractor>();
            }

            _socketInteractor.hoverFilters.Add(this);
            _socketInteractor.selectFilters.Add(this);
        }

        private void OnDisable()
        {
            if (_socketInteractor == null)
            {
                return;
            }

            _socketInteractor.hoverFilters.Remove(this);
            _socketInteractor.selectFilters.Remove(this);
        }

        public bool Process(
            IXRHoverInteractor interactor,
            IXRHoverInteractable interactable)
        {
            return IsOutsideSocketHierarchy(interactable.transform);
        }

        public bool Process(
            IXRSelectInteractor interactor,
            IXRSelectInteractable interactable)
        {
            return IsOutsideSocketHierarchy(interactable.transform);
        }

        private bool IsOutsideSocketHierarchy(Transform interactableTransform)
        {
            // 자식 소켓이 자신의 부모 멀티탭을 플러그로 인식하면
            // 부모와 자식이 서로 끌어당기며 물리적으로 튀어 오를 수 있습니다.
            return interactableTransform == null ||
                   !_socketInteractor.transform.IsChildOf(interactableTransform);
        }
    }
}
