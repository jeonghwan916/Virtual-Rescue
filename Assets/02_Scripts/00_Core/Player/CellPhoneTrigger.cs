using UnityEngine;

public class CellPhoneTrigger : MonoBehaviour
{
    [SerializeField] private GameObject _cellPhone;

    private Rigidbody _cellPhoneRigidbody;
    private Vector3 _respawnPosition;
    private Quaternion _respawnRotation;

    private void Awake()
    {
        _cellPhoneRigidbody = _cellPhone.GetComponent<Rigidbody>();
        _respawnPosition = _cellPhoneRigidbody.position;
        _respawnRotation = _cellPhoneRigidbody.rotation;
    }

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody targetRigidbody = other.attachedRigidbody;

        if (targetRigidbody != _cellPhoneRigidbody)
            return;

        targetRigidbody.linearVelocity = Vector3.zero;
        targetRigidbody.angularVelocity = Vector3.zero;
        targetRigidbody.position = _respawnPosition;
        targetRigidbody.rotation = _respawnRotation;
        targetRigidbody.Sleep();
    }
}
