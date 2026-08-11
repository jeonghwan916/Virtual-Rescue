using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class ClothGrab : MonoBehaviour
{
    [Header("Cloth")]
    [SerializeField] private VerletCloth cloth;
    [SerializeField] private Transform probeTarget;

    [Header("Grab Area")]
    [SerializeField] private float searchRange = 0.25f;
    [SerializeField] private float grabRadius = 0.08f;
    [SerializeField] private bool followClosestPointWhenIdle = true;

    private XRGrabInteractable grabInteractable;
    private VerletCloth.AttachedArea grabbedArea;
    private int closestPointId = -1;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrabEntered);
        grabInteractable.selectExited.AddListener(OnGrabExited);
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrabEntered);
        grabInteractable.selectExited.RemoveListener(OnGrabExited);

        ReleaseCloth();
    }

    private void Start()
    {
        if (cloth == null)
            cloth = GetComponentInParent<VerletCloth>();
    }

    private void Update()
    {
        if (cloth == null || cloth.pos == null || grabbedArea != null)
            return;

        Vector3 targetPosition = probeTarget != null ? probeTarget.position : transform.position;
        closestPointId = cloth.GetClosestPoint(targetPosition, searchRange);

        if (followClosestPointWhenIdle && closestPointId >= 0)
            transform.position = cloth.pos[closestPointId];
    }

    private void OnGrabEntered(SelectEnterEventArgs args)
    {
        if (cloth == null || cloth.pos == null)
            return;

        Vector3 grabCenter = closestPointId >= 0 ? cloth.pos[closestPointId] : transform.position;
        grabbedArea = cloth.AttachArea(transform, grabCenter, grabRadius);
    }

    private void OnGrabExited(SelectExitEventArgs args)
    {
        ReleaseCloth();
    }

    private void ReleaseCloth()
    {
        if (cloth == null || grabbedArea == null)
            return;

        cloth.DetachArea(grabbedArea);
        grabbedArea = null;
    }
}
