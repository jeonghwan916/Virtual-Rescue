using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class VerletCloth : MonoBehaviour
{
    [System.Serializable]
    public class AttachedArea
    {
        public Transform transform;
        public int[] pointIds;
        public Vector3[] localOffsets;
        public float[] originalMass;
    }

    [Header("Cloth")]
    [SerializeField] private int columns = 16;
    [SerializeField] private int rows = 16;
    [SerializeField] private float spacing = 0.05f;
    [SerializeField] private float gravityScale = 1.0f;

    [Header("Constraints")]
    [SerializeField] private int constraintIterations = 12;
    [SerializeField] private bool enableBendingConstraints = true;

    [Header("Collision")]
    [SerializeField] private bool enableCollision = true;
    [SerializeField] private LayerMask collisionMask = ~0;
    [SerializeField] private float collisionPointRadius = 0.025f;
    [SerializeField] private float collisionFriction = 0.5f;
    [SerializeField] private int collisionIterations = 1;

    [HideInInspector] public Vector3[] pos;
    [HideInInspector] public Vector3[] prevPos;
    [HideInInspector] public float[] mass;

    private Mesh mesh;
    private MeshFilter meshFilter;
    private Vector3[] vertices;
    private Vector2[] uvs;
    private int[] triangles;
    private SphereCollider collisionProbe;
    private readonly Collider[] collisionHits = new Collider[16];
    private readonly List<AttachedArea> attachedAreas = new List<AttachedArea>();
    private readonly List<int> pointBuffer = new List<int>();

    public int Columns => Mathf.Max(2, columns);
    public int Rows => Mathf.Max(2, rows);
    public int PointCount => pos == null ? 0 : pos.Length;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        CreateMesh();
        CreateCollisionProbe();
    }

    private void Start()
    {
        CreatePoints();
        BuildMeshLayout();
        UpdateMeshData();
    }

    private void OnDestroy()
    {
        if (collisionProbe != null)
            Destroy(collisionProbe.gameObject);
    }

    private void FixedUpdate()
    {
        if (PointCount == 0)
            return;

        ApplyAttachedAreas();
        ApplyVerlet();

        int iterations = Mathf.Max(1, constraintIterations);
        for (int i = 0; i < iterations; i++)
        {
            ApplyStructuralConstraints();
            ApplyShearConstraints();

            if (enableBendingConstraints)
                ApplyBendingConstraints();

            ApplyCollisions();
        }
    }

    private void LateUpdate()
    {
        if (PointCount == 0 || mesh == null)
            return;

        UpdateMeshData();
    }

    public int GetIndex(int x, int y)
    {
        return y * Columns + x;
    }

    public int GetClosestPoint(Vector3 targetPos, float range = float.PositiveInfinity)
    {
        if (pos == null)
            return -1;

        float distanceMin = range;
        int pointMin = -1;
        for (int i = 0; i < pos.Length; i++)
        {
            float distance = Vector3.Distance(targetPos, pos[i]);
            if (distance < distanceMin)
            {
                distanceMin = distance;
                pointMin = i;
            }
        }

        return pointMin;
    }

    public int GetPointsInRadius(Vector3 center, float radius, List<int> results)
    {
        results.Clear();

        if (pos == null)
            return 0;

        float radiusSqr = radius * radius;
        for (int i = 0; i < pos.Length; i++)
        {
            if ((pos[i] - center).sqrMagnitude <= radiusSqr)
                results.Add(i);
        }

        return results.Count;
    }

    public AttachedArea AttachArea(Transform attach, Vector3 center, float radius)
    {
        if (attach == null || pos == null)
            return null;

        pointBuffer.Clear();
        GetPointsInRadius(center, Mathf.Max(0.0f, radius), pointBuffer);

        if (pointBuffer.Count == 0)
        {
            int closestPoint = GetClosestPoint(center);
            if (closestPoint >= 0)
                pointBuffer.Add(closestPoint);
        }

        if (pointBuffer.Count == 0)
            return null;

        AttachedArea area = new AttachedArea
        {
            transform = attach,
            pointIds = pointBuffer.ToArray(),
            localOffsets = new Vector3[pointBuffer.Count],
            originalMass = new float[pointBuffer.Count]
        };

        for (int i = 0; i < area.pointIds.Length; i++)
        {
            int id = area.pointIds[i];
            area.localOffsets[i] = attach.InverseTransformPoint(pos[id]);
            area.originalMass[i] = mass[id];
            mass[id] = 0.0f;
        }

        attachedAreas.Add(area);
        ApplyAttachedArea(area);
        return area;
    }

    public void DetachArea(AttachedArea area)
    {
        if (area == null)
            return;

        attachedAreas.Remove(area);

        for (int i = 0; i < area.pointIds.Length; i++)
        {
            int id = area.pointIds[i];
            if (id >= 0 && id < mass.Length)
                mass[id] = area.originalMass[i] <= 0.0f ? 1.0f : area.originalMass[i];
        }
    }

    private void CreateMesh()
    {
        mesh = new Mesh { name = "Verlet Cloth Mesh" };
        mesh.MarkDynamic();
        meshFilter.sharedMesh = mesh;
    }

    private void CreatePoints()
    {
        int pointCount = Columns * Rows;
        pos = new Vector3[pointCount];
        prevPos = new Vector3[pointCount];
        mass = new float[pointCount];

        Vector3 offset = new Vector3((Columns - 1) * spacing * 0.5f, 0.0f, (Rows - 1) * spacing * 0.5f);
        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                int id = GetIndex(x, y);
                Vector3 localPos = new Vector3(x * spacing, 0.0f, y * spacing) - offset;
                pos[id] = transform.TransformPoint(localPos);
                prevPos[id] = pos[id];
                mass[id] = 1.0f;
            }
        }
    }

    private void BuildMeshLayout()
    {
        vertices = new Vector3[PointCount];
        uvs = new Vector2[PointCount];
        triangles = new int[(Columns - 1) * (Rows - 1) * 6];

        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                int id = GetIndex(x, y);
                uvs[id] = new Vector2((float)x / (Columns - 1), (float)y / (Rows - 1));
            }
        }

        int triangleIndex = 0;
        for (int y = 0; y < Rows - 1; y++)
        {
            for (int x = 0; x < Columns - 1; x++)
            {
                int bottomLeft = GetIndex(x, y);
                int bottomRight = GetIndex(x + 1, y);
                int topLeft = GetIndex(x, y + 1);
                int topRight = GetIndex(x + 1, y + 1);

                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = bottomRight;
                triangles[triangleIndex++] = bottomRight;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = topRight;
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
    }

    private void UpdateMeshData()
    {
        for (int i = 0; i < pos.Length; i++)
            vertices[i] = transform.InverseTransformPoint(pos[i]);

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private void ApplyVerlet()
    {
        float dtSqr = Time.fixedDeltaTime * Time.fixedDeltaTime;
        Vector3 gravity = Physics.gravity * gravityScale * dtSqr;

        for (int i = 0; i < pos.Length; i++)
        {
            if (mass[i] == 0.0f)
                continue;

            Vector3 temp = pos[i];
            pos[i] += pos[i] - prevPos[i];
            pos[i] += gravity;
            prevPos[i] = temp;
        }
    }

    private void ApplyStructuralConstraints()
    {
        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                if (x < Columns - 1)
                    ApplyDistanceConstraint(GetIndex(x, y), GetIndex(x + 1, y), spacing);

                if (y < Rows - 1)
                    ApplyDistanceConstraint(GetIndex(x, y), GetIndex(x, y + 1), spacing);
            }
        }
    }

    private void ApplyShearConstraints()
    {
        float diagonalDistance = spacing * Mathf.Sqrt(2.0f);
        for (int y = 0; y < Rows - 1; y++)
        {
            for (int x = 0; x < Columns - 1; x++)
            {
                ApplyDistanceConstraint(GetIndex(x, y), GetIndex(x + 1, y + 1), diagonalDistance);
                ApplyDistanceConstraint(GetIndex(x + 1, y), GetIndex(x, y + 1), diagonalDistance);
            }
        }
    }

    private void ApplyBendingConstraints()
    {
        float bendDistance = spacing * 2.0f;
        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                if (x < Columns - 2)
                    ApplyDistanceConstraint(GetIndex(x, y), GetIndex(x + 2, y), bendDistance);

                if (y < Rows - 2)
                    ApplyDistanceConstraint(GetIndex(x, y), GetIndex(x, y + 2), bendDistance);
            }
        }
    }

    private void ApplyDistanceConstraint(int p1, int p2, float targetDistance)
    {
        Vector3 delta = pos[p2] - pos[p1];
        float length = delta.magnitude;
        if (length <= 0.0001f)
            return;

        float invMass1 = InverseMass(mass[p1]);
        float invMass2 = InverseMass(mass[p2]);
        float invMassSum = invMass1 + invMass2;
        if (invMassSum <= 0.0f)
            return;

        Vector3 correction = delta * ((length - targetDistance) / length);
        pos[p1] += correction * (invMass1 / invMassSum);
        pos[p2] -= correction * (invMass2 / invMassSum);
    }

    private void ApplyAttachedAreas()
    {
        for (int i = attachedAreas.Count - 1; i >= 0; i--)
        {
            AttachedArea area = attachedAreas[i];
            if (area == null || area.transform == null)
            {
                attachedAreas.RemoveAt(i);
                continue;
            }

            ApplyAttachedArea(area);
        }
    }

    private void ApplyAttachedArea(AttachedArea area)
    {
        for (int i = 0; i < area.pointIds.Length; i++)
        {
            int id = area.pointIds[i];
            if (id < 0 || id >= pos.Length)
                continue;

            Vector3 target = area.transform.TransformPoint(area.localOffsets[i]);
            pos[id] = target;
            prevPos[id] = target;
            mass[id] = 0.0f;
        }
    }

    private void ApplyCollisions()
    {
        if (!enableCollision || collisionProbe == null || collisionPointRadius <= 0.0f)
            return;

        collisionProbe.radius = collisionPointRadius;
        int iterations = Mathf.Max(1, collisionIterations);
        for (int iteration = 0; iteration < iterations; iteration++)
        {
            for (int i = 0; i < pos.Length; i++)
            {
                if (mass[i] == 0.0f)
                    continue;

                ResolvePointCollision(i);
            }
        }
    }

    private void ResolvePointCollision(int id)
    {
        int hits = Physics.OverlapSphereNonAlloc(pos[id], collisionPointRadius, collisionHits, collisionMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits; i++)
        {
            Collider hit = collisionHits[i];
            if (hit == collisionProbe)
                continue;

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

            if (!isOverlapping)
                continue;

            pos[id] += direction * distance;
            prevPos[id] = Vector3.Lerp(prevPos[id], pos[id], collisionFriction);
        }
    }

    private void CreateCollisionProbe()
    {
        GameObject probeObject = new GameObject("Cloth Collision Probe");
        probeObject.hideFlags = HideFlags.HideAndDontSave;
        probeObject.transform.SetParent(transform, false);

        collisionProbe = probeObject.AddComponent<SphereCollider>();
        collisionProbe.isTrigger = true;
        collisionProbe.radius = collisionPointRadius;
    }

    private static float InverseMass(float pointMass)
    {
        return pointMass <= 0.0f ? 0.0f : 1.0f / pointMass;
    }
}
