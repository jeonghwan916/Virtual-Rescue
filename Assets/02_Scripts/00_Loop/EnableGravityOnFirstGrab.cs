using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class EnableGravityOnFirstGrab : MonoBehaviour
{
    [SerializeField] private XRGrabInteractable grabInteractable;
    [SerializeField] private Rigidbody rb;

    private bool hasBeenGrabbed;

    private void Awake()
    {
        if (!grabInteractable) grabInteractable = GetComponent<XRGrabInteractable>();
        if (!rb) rb = GetComponent<Rigidbody>();

        grabInteractable.selectEntered.AddListener(OnGrabbed);
    }

    private void OnDestroy()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (hasBeenGrabbed) return;

        hasBeenGrabbed = true;
        rb.useGravity = true;
    }
}