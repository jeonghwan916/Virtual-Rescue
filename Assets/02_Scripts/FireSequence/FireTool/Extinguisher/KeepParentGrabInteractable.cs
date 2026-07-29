using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VirtualRescue.FireSequence
{
    public sealed class KeepParentGrabInteractable : XRGrabInteractable
    {
        [SerializeField] private Transform m_ParentOverride;
        [SerializeField] private bool m_KeepWorldPosition = true;

        private Transform _cachedParent;

        protected override void OnSelectEntering(SelectEnterEventArgs args)
        {
            _cachedParent = m_ParentOverride != null ? m_ParentOverride : transform.parent;

            base.OnSelectEntering(args);

            RestoreParent();
        }

        protected override void OnSelectExiting(SelectExitEventArgs args)
        {
            base.OnSelectExiting(args);

            RestoreParent();
        }

        private void RestoreParent()
        {
            if (_cachedParent == null || transform.parent == _cachedParent)
            {
                return;
            }

            transform.SetParent(_cachedParent, m_KeepWorldPosition);
        }
    }
}