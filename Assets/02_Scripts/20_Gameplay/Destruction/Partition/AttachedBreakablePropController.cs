using UnityEngine;
using VirtualRescue.Destruction;

namespace VirtualRescue.Missions08
{
    [DisallowMultipleComponent]
    public sealed class AttachedBreakablePropController : MonoBehaviour
    {
        [SerializeField] private Transform _sourceFragments;
        [SerializeField] private GameObject[] _textAttachments;
        [SerializeField] private Vector2 _sourceImpactAreaSize = new Vector2(1.25f, 1.25f);

        private UnfreezeFragment[] _sourcePieces;
        private LocalizedFracturePiece[] _attachedPieces;
        private bool _hasDetachedFromWall;
        private bool _areTextAttachmentsHidden;

        private void Awake()
        {
            _sourcePieces = _sourceFragments != null
                ? _sourceFragments.GetComponentsInChildren<UnfreezeFragment>(true)
                : System.Array.Empty<UnfreezeFragment>();
            _attachedPieces = GetComponentsInChildren<LocalizedFracturePiece>(true);

            foreach (LocalizedFracturePiece attachedPiece in _attachedPieces)
            {
                if (attachedPiece != null &&
                    attachedPiece.GetComponent<DestructionBatImpactReceiver>() == null)
                {
                    attachedPiece.gameObject.AddComponent<DestructionBatImpactReceiver>();
                }
            }
        }

        private void Update()
        {
            if (!_hasDetachedFromWall && HasSourceFragmentReleased())
            {
                HideTextAttachments();
                ReleaseAttachedPieces();
                _hasDetachedFromWall = true;
            }

            if (!_areTextAttachmentsHidden && HasAttachedFragmentReleased())
            {
                HideTextAttachments();
            }
        }

        private bool HasSourceFragmentReleased()
        {
            foreach (UnfreezeFragment sourcePiece in _sourcePieces)
            {
                Rigidbody sourceRigidbody = sourcePiece != null
                    ? sourcePiece.GetComponent<Rigidbody>()
                    : null;
                if (sourceRigidbody != null &&
                    IsWithinSourceImpactArea(sourcePiece.transform.position) &&
                    sourceRigidbody.constraints != RigidbodyConstraints.FreezeAll)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsWithinSourceImpactArea(Vector3 sourcePiecePosition)
        {
            if (_sourceFragments == null)
            {
                return false;
            }

            Vector3 localStickerPosition = _sourceFragments.InverseTransformPoint(transform.position);
            Vector3 localPiecePosition = _sourceFragments.InverseTransformPoint(sourcePiecePosition);
            Vector2 halfAreaSize = _sourceImpactAreaSize * 0.5f;

            return Mathf.Abs(localPiecePosition.x - localStickerPosition.x) <= halfAreaSize.x &&
                   Mathf.Abs(localPiecePosition.y - localStickerPosition.y) <= halfAreaSize.y;
        }

        private bool HasAttachedFragmentReleased()
        {
            foreach (LocalizedFracturePiece attachedPiece in _attachedPieces)
            {
                if (attachedPiece != null && attachedPiece.IsReleased)
                {
                    return true;
                }
            }

            return false;
        }

        private void HideTextAttachments()
        {
            if (_textAttachments == null)
            {
                return;
            }

            foreach (GameObject textAttachment in _textAttachments)
            {
                if (textAttachment != null)
                {
                    textAttachment.SetActive(false);
                }
            }

            _areTextAttachmentsHidden = true;
        }

        private void ReleaseAttachedPieces()
        {
            Vector3 releasePoint = transform.position;
            foreach (LocalizedFracturePiece attachedPiece in _attachedPieces)
            {
                if (attachedPiece == null || attachedPiece.IsReleased)
                {
                    continue;
                }

                attachedPiece.Release(Vector3.zero, releasePoint);
            }
        }
    }
}
