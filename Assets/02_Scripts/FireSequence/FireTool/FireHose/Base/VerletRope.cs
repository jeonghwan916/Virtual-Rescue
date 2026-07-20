using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerletRope : MonoBehaviour
{
    public int pointsNb = 20;
    
    [System.Serializable]
    public class AttachedPoint
    {
        public int id = 0;
        public Transform transform;
        [HideInInspector] public Vector3 force = Vector3.zero;

        public AttachedPoint(int _id, Transform _transform) {
            id = _id;
            transform = _transform;
        }

        public bool IsValid(int maxPoints = 0) {
            return !(id < 0 || id >= maxPoints || transform == null);
        }
    }

    [System.Serializable]
    private class StorageAnchor
    {
        public int pointId;
        public Transform anchor;
        public float radius = 0.05f;
        public float stiffness = 1.0f;
        public bool released;
    }
    
    [Header("Rope")]
    [SerializeField] private float attachedBodiesDamping = 0.5f;

    [SerializeField] private List<AttachedPoint> attachedPoints = new List<AttachedPoint>();
    
    [HideInInspector] public Vector3[] pos;
    [HideInInspector] public Vector3[] prevPos;
    [HideInInspector] public float[] mass;

    [Header("Constraints")]

    public float constraintHeightMin = 0.01f;
    public float constraintHeightFriction = 0.5f;

    [Space]
    public float constraintDistance = 0.1f;
    public int constraintDistanceIterations = 20;

    [Header("Length Limit")]
    [SerializeField] private bool enableLengthLimit = false;
    [SerializeField] private Transform lengthLimitAnchor;
    [SerializeField] private float maxRopeLength = 1.0f;
    [SerializeField] private int lengthLimitPointId = -1;
    [SerializeField, Range(0.0f, 1.0f)] private float lengthLimitDamping = 0.75f;

    [Header("Collision")]
    [SerializeField] private bool enableCollision = false;
    [SerializeField] private LayerMask collisionMask = ~0;
    [SerializeField] private float collisionPointRadius = 0.05f;
    [SerializeField] private float collisionFriction = 0.5f;
    [SerializeField] private int collisionIterations = 1;

    [Header("Storage Anchors")]
    [SerializeField] private bool enableStorageAnchors = false;
    [SerializeField] private StorageAnchor[] storageAnchors;
    [SerializeField] private int storageAnchorIterations = 1;
    [SerializeField] private float storageAnchorStiffness = 0.35f;
    [SerializeField] private float storageAnchorReleaseDistance = 0.35f;
    [SerializeField] private bool releaseStorageAnchorsByDistance = true;

    private LineRenderer line;
    private SphereCollider collisionProbe;
    private readonly Collider[] collisionHits = new Collider[16];
    private bool[] storageAnchorReleaseArmed;

    private void Awake() {
        line = GetComponent<LineRenderer>();
        CreateCollisionProbe();
    }

    private void Start() {
        CreatePoints();
        InitializeStorageAnchorRuntimeState();
    }

    private void OnDestroy() {
        if (collisionProbe != null)
            Destroy(collisionProbe.gameObject);
    }

    private void FixedUpdate() {
        if (pointsNb > 1) {
            ApplyForces();
            ApplyAttach();
            
            ApplyVerlet();
            ApplyConstraints();
        }

        if (line)
            line.SetPositions(pos);
    }

    private void CreatePoints() {
        pos = new Vector3[pointsNb];
        prevPos = new Vector3[pointsNb];
        mass = new float[pointsNb];

        Vector3 targetPos = transform.position + Physics.gravity.normalized * (constraintDistance * pointsNb);
        if (attachedPoints.Count > 1) {
            AttachedPoint lastPoint = attachedPoints[attachedPoints.Count - 1];
            if (lastPoint.IsValid(pointsNb)) targetPos = lastPoint.transform.position;
        }
        for (int i = 0; i < pointsNb; i ++) {
            pos[i] = Vector3.Lerp(transform.position, targetPos, (float)i / (pointsNb - 1));
            prevPos[i] = pos[i];
            mass[i] = 1.0f;
        }

        if (line) line.positionCount = pointsNb;
    }

    public AttachedPoint AttachPoint(int id, Transform attach) {
        AttachedPoint newPoint = new AttachedPoint(id, attach);
        AttachedPoint point;
        for (int i = 0; i < attachedPoints.Count; i++)
        {
            point = attachedPoints[i];
            if (point.id == id) {
                point.transform = attach;
                point.force = Vector3.zero;
                return point;
            } else if (point.id > id) {
                attachedPoints.Insert(i, newPoint);
                return newPoint;
            }
        }
        attachedPoints.Add(newPoint);
        return newPoint;
    }

    public void DetachPoint(AttachedPoint point) {
        attachedPoints.Remove(point);
        mass[point.id] = 1.0f;
    }

    public void DetachPoint(int id) {
        foreach (AttachedPoint point in attachedPoints)
        {
            if (point.id == id) {
                DetachPoint(point);
                return;
            }
        }
    }

    public int GetClosestPoint(Vector3 targetPos, float range = float.PositiveInfinity) {
        float distance;
        float distanceMin = range;
        int pointMin = -1;
        for (int i = 0; i < pointsNb; i++)
        {
            distance = Vector3.Distance(targetPos, pos[i]);
            if (distance < distanceMin) {
                distanceMin = distance;
                pointMin = i;
            }
        }
        return pointMin;
    }

    private float GetConstraint(int p1, int p2, float distance, Vector3[] constraint, bool useMass = true) {
        Vector3 delta = pos[p2] - pos[p1];
        float length = delta.magnitude;
        if (length <= 0.0001f) {
            constraint[0] = Vector3.zero;
            constraint[1] = Vector3.zero;
            return 0.0f;
        }

        float difference;
        if (useMass) {
            float invmass1 = InverseMass(mass[p1]);
            float invmass2 = InverseMass(mass[p2]);
            difference = (length - distance) / (length * (invmass1 + invmass2));
            constraint[0] = delta * difference * invmass1;
            constraint[1] = -delta * difference * invmass2;
            return difference;
        } else {
            difference = (length - distance) / length;
            constraint[0] = delta * difference * 0.5f;
            constraint[1] = -delta * difference * 0.5f;
            return difference;
        }
    }

    private void ApplyVerlet() {
        Vector3 temp;
        for (int i = 0; i < pointsNb; i++) {
            temp = pos[i];
            pos[i] += pos[i] - prevPos[i];
            pos[i] += mass[i] * Physics.gravity * Time.fixedDeltaTime * Time.fixedDeltaTime;
            prevPos[i] = temp;
        }
    }

    private void ApplyConstraints() {
        Vector3[] constraint = new Vector3[2];
        for (int iteration = 0; iteration < constraintDistanceIterations; iteration++) {
            for (int i = 1; i < pointsNb; i++) {
                if (pos[i].y < constraintHeightMin) {
                    prevPos[i] = Vector3.Lerp(prevPos[i], pos[i], constraintHeightFriction);
                    pos[i].y = constraintHeightMin;
                }

                float diff = GetConstraint(i-1, i, constraintDistance, constraint);
                pos[i - 1] += constraint[0];
                pos[i] += constraint[1];
            }

            ApplyStorageAnchors();
            ApplyCollisions();
            ApplyLengthLimit();
        }
    }

    private void ApplyLengthLimit() {
        if (!enableLengthLimit || lengthLimitAnchor == null || pointsNb <= 0) return;

        int pointId = lengthLimitPointId < 0 ? pointsNb - 1 : Mathf.Clamp(lengthLimitPointId, 0, pointsNb - 1);
        float maxLength = Mathf.Max(0.0f, maxRopeLength);
        Vector3 offset = pos[pointId] - lengthLimitAnchor.position;
        float distance = offset.magnitude;

        if (distance <= maxLength || distance <= 0.0001f) return;

        pos[pointId] = lengthLimitAnchor.position + offset.normalized * maxLength;
        prevPos[pointId] = Vector3.Lerp(prevPos[pointId], pos[pointId], lengthLimitDamping);
    }

    private void ApplyStorageAnchors() {
        if (!enableStorageAnchors || storageAnchors == null || storageAnchors.Length == 0) return;

        EnsureStorageAnchorRuntimeState();

        int iterations = Mathf.Max(1, storageAnchorIterations);
        for (int iteration = 0; iteration < iterations; iteration++) {
            for (int i = 0; i < storageAnchors.Length; i++) {
                ApplyStorageAnchor(i);
            }
        }
    }

    private void ApplyStorageAnchor(int storageAnchorIndex) {
        StorageAnchor storageAnchor = storageAnchors[storageAnchorIndex];
        if (storageAnchor == null || storageAnchor.released || storageAnchor.anchor == null) return;
        if (storageAnchor.pointId < 0 || storageAnchor.pointId >= pointsNb) return;
        if (mass[storageAnchor.pointId] == 0.0f) return;

        float radius = Mathf.Max(0.0f, storageAnchor.radius);
        float releaseDistance = Mathf.Max(storageAnchorReleaseDistance, radius);
        Vector3 delta = storageAnchor.anchor.position - pos[storageAnchor.pointId];
        float distance = delta.magnitude;

        if (distance <= radius)
            storageAnchorReleaseArmed[storageAnchorIndex] = true;

        if (releaseStorageAnchorsByDistance && storageAnchorReleaseArmed[storageAnchorIndex] && distance > releaseDistance) {
            storageAnchor.released = true;
            return;
        }

        if (distance <= radius || distance <= 0.0001f) return;

        Vector3 target = storageAnchor.anchor.position - delta.normalized * radius;
        float stiffness = Mathf.Clamp01(storageAnchorStiffness * storageAnchor.stiffness);
        pos[storageAnchor.pointId] += (target - pos[storageAnchor.pointId]) * stiffness;
    }

    private void ApplyCollisions() {
        if (!enableCollision || collisionProbe == null || collisionPointRadius <= 0.0f) return;

        collisionProbe.radius = collisionPointRadius;
        int iterations = Mathf.Max(1, collisionIterations);
        for (int iteration = 0; iteration < iterations; iteration++) {
            for (int i = 0; i < pointsNb; i++) {
                if (mass[i] == 0.0f) continue;
                ResolvePointCollision(i);
            }
        }
    }

    private void ResolvePointCollision(int id) {
        int hits = Physics.OverlapSphereNonAlloc(pos[id], collisionPointRadius, collisionHits, collisionMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits; i++) {
            Collider hit = collisionHits[i];
            if (hit == collisionProbe) continue;

            Vector3 direction;
            float distance;
            bool isOverlapping = Physics.ComputePenetration(
                collisionProbe,
                pos[id],
                Quaternion.identity,
                hit,
                hit.transform.position,
                hit.transform.rotation,
                out direction,
                out distance
            );

            if (!isOverlapping) continue;

            pos[id] += direction * distance;
            prevPos[id] = Vector3.Lerp(prevPos[id], pos[id], collisionFriction);
        }
    }

    private void CreateCollisionProbe() {
        GameObject probeObject = new GameObject("Collision Probe");
        probeObject.hideFlags = HideFlags.HideAndDontSave;
        probeObject.transform.SetParent(transform, false);

        collisionProbe = probeObject.AddComponent<SphereCollider>();
        collisionProbe.isTrigger = true;
        collisionProbe.radius = collisionPointRadius;
    }

    private void InitializeStorageAnchorRuntimeState() {
        storageAnchorReleaseArmed = storageAnchors == null ? null : new bool[storageAnchors.Length];

        if (storageAnchors == null) return;

        for (int i = 0; i < storageAnchors.Length; i++) {
            if (storageAnchors[i] != null)
                storageAnchors[i].released = false;
        }
    }

    private void EnsureStorageAnchorRuntimeState() {
        if (storageAnchors == null) {
            storageAnchorReleaseArmed = null;
            return;
        }

        if (storageAnchorReleaseArmed == null || storageAnchorReleaseArmed.Length != storageAnchors.Length)
            storageAnchorReleaseArmed = new bool[storageAnchors.Length];
    }

    private void ApplyAttach() {
        Vector3[] constraint = new Vector3[2];
        AttachedPoint previousPoint = null;
        foreach (AttachedPoint point in attachedPoints){
            if (!point.IsValid(pointsNb)) continue;

            pos[point.id] = point.transform.position;
            prevPos[point.id] = point.transform.position;
            mass[point.id] = 0.0f;

            if (previousPoint != null) {
                int points = point.id - previousPoint.id;
                float diff = GetConstraint(previousPoint.id, point.id, constraintDistance * points, constraint);
                if (diff > 0.0f){
                    previousPoint.force += constraint[0];
                    point.force = constraint[1];
                }
            } else {
                point.force = Vector3.zero;
            }
            previousPoint = point;
        }
    }

    private void ApplyForces() {
        Rigidbody body = null;
        foreach (AttachedPoint point in attachedPoints) {
            if (!point.IsValid(pointsNb)) continue;

            body = point.transform.GetComponent<Rigidbody>();
            if (body != null && !body.isKinematic) {
                mass[point.id] = body.mass;
                body.linearVelocity += point.force * attachedBodiesDamping;
            }
        }
    }

    private static float InverseMass(float mass) {
        return mass == 0.0f ? 0.00000001f : 1.0f / mass;
    }
}
