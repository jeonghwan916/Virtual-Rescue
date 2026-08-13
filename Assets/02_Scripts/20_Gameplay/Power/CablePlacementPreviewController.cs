using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Playables;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VirtualRescue.Gameplay.Power
{
    [ExecuteAlways]
    public sealed class CablePlacementPreviewController : MonoBehaviour
    {
        [SerializeField] private Transform _plugPlacementHandle;
        [SerializeField] private Transform _plug;
        [SerializeField] private RigBuilder _rigBuilder;
        [SerializeField] private ChainIKConstraint _freeCableConstraint;
        [SerializeField] private ChainIKConstraint[] _guidedConstraints;
        [SerializeField] private XRGrabInteractable[] _endpointGrabInteractables;

        private bool _hasAppliedGuidedMode;
        private bool _guidedMode;
        private bool _releasedForInteraction;

        private void OnEnable()
        {
            if (!HasValidConfiguration())
            {
                if (Application.isPlaying)
                {
                    enabled = false;
                }

                return;
            }

            _releasedForInteraction = false;
            ApplyMode(true);

            if (!Application.isPlaying)
            {
                BuildEditModePreview();
                UpdateEditModePreview();
            }
        }

        private void OnDisable()
        {
            if (!Application.isPlaying && _rigBuilder != null)
            {
                _rigBuilder.Clear();
            }
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying)
            {
                UpdateEditModePreview();
                return;
            }

            if (!_releasedForInteraction && IsSelectedOutsideSocket())
            {
                _releasedForInteraction = true;
            }

            ApplyMode(!_releasedForInteraction);
        }

        private void UpdateEditModePreview()
        {
            if (!HasValidConfiguration())
            {
                return;
            }

            _plug.SetPositionAndRotation(
                _plugPlacementHandle.position,
                _plugPlacementHandle.rotation);

            ApplyMode(true);

            if (!_rigBuilder.graph.IsValid())
            {
                BuildEditModePreview();
            }

            _rigBuilder.Evaluate(0f);
        }

        private void BuildEditModePreview()
        {
            if (_rigBuilder == null || _rigBuilder.graph.IsValid())
            {
                return;
            }

            if (_rigBuilder.Build())
            {
                _rigBuilder.graph.SetTimeUpdateMode(
                    DirectorUpdateMode.Manual);
            }
        }

        private bool IsSelectedOutsideSocket()
        {
            foreach (XRGrabInteractable grabInteractable in
                     _endpointGrabInteractables)
            {
                if (grabInteractable == null)
                {
                    continue;
                }

                foreach (IXRSelectInteractor interactor in
                         grabInteractable.interactorsSelecting)
                {
                    if (interactor is not XRSocketInteractor)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void ApplyMode(bool useGuides)
        {
            if (_hasAppliedGuidedMode && _guidedMode == useGuides)
            {
                return;
            }

            _freeCableConstraint.weight = useGuides ? 0f : 1f;

            foreach (ChainIKConstraint constraint in _guidedConstraints)
            {
                constraint.weight = useGuides ? 1f : 0f;
            }

            _guidedMode = useGuides;
            _hasAppliedGuidedMode = true;
        }

        private bool HasValidConfiguration()
        {
            if (_plugPlacementHandle == null ||
                _plug == null ||
                _rigBuilder == null ||
                _freeCableConstraint == null ||
                _guidedConstraints == null ||
                _guidedConstraints.Length != 2 ||
                _endpointGrabInteractables == null ||
                _endpointGrabInteractables.Length == 0)
            {
                return false;
            }

            foreach (ChainIKConstraint constraint in _guidedConstraints)
            {
                if (constraint == null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
