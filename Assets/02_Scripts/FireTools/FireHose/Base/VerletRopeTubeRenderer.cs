using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class VerletRopeTubeRenderer : MonoBehaviour
{
    [SerializeField] private VerletRope rope;

    [Header("Tube")]
    [SerializeField] private float widthRadius = 0.03f;
    [SerializeField] private float heightRadius = 0.03f;
    [SerializeField] private int radialSegments = 12;
    [SerializeField] private float textureTilingPerMeter = 2.0f;
    [SerializeField] private bool capEnds = true;

    [Header("Smoothing")]
    [SerializeField] private bool enableSmoothing = true;
    [SerializeField] private int smoothingIterations = 1;

    private Mesh mesh;
    private MeshFilter meshFilter;

    private Vector3[] rawLocalCenters;
    private Vector3[] localCenters;
    private Vector3[] smoothingBufferA;
    private Vector3[] smoothingBufferB;

    private Vector3[] vertices;
    private Vector3[] normals;
    private Vector2[] uvs;
    private int[] triangles;

    private int cachedPointCount;
    private int cachedRadialSegments;
    private bool cachedCapEnds;
    private int cachedRawPointCount;
    private bool cachedEnableSmoothing;
    private int cachedSmoothingIterations;

    private int RadialSegments => Mathf.Max(3, radialSegments);
    private int SmoothingIterations => enableSmoothing ? Mathf.Max(0, smoothingIterations) : 0;
    private int RingVertexCount => RadialSegments + 1;
    private float WidthRadius => Mathf.Max(widthRadius, 0.0001f);
    private float HeightRadius => Mathf.Max(heightRadius, 0.0001f);
    private float TextureTilingPerMeter => Mathf.Max(textureTilingPerMeter, 0.0f);
    private int SideVertexCount => cachedPointCount * RingVertexCount;
    private int StartCapRimStart => SideVertexCount;
    private int StartCapCenterIndex => StartCapRimStart + RingVertexCount;
    private int EndCapRimStart => StartCapCenterIndex + 1;
    private int EndCapCenterIndex => EndCapRimStart + RingVertexCount;

    private void Awake() { // 컴포넌트 참조와 동적 Mesh 인스턴스를 초기화한다.
        InitializeReferences();
        InitializeMesh();
    }

    private void LateUpdate() { // Verlet 물리 갱신 이후 메쉬 데이터를 갱신한다.
        if (!IsRopeReady())
            return;

        if (NeedsLayoutRebuild())
            RebuildMeshLayout();

        UpdateRawLocalCenters();
        BuildSmoothedCenters();
        UpdateMeshData();
        ApplyMeshData();
    }

    private void InitializeReferences() { // Rope와 MeshFilter 참조를 확보한다.
        if (rope == null)
            rope = GetComponentInParent<VerletRope>();

        meshFilter = GetComponent<MeshFilter>();
    }

    private void InitializeMesh() { // 런타임에서 갱신할 전용 Mesh를 생성하고 MeshFilter에 연결한다.
        mesh = new Mesh { name = "Verlet Rope Tube Mesh" };
        mesh.MarkDynamic();
        meshFilter.sharedMesh = mesh;
    }

    private bool IsRopeReady() { // rope.pos[]를 사용해 튜브를 만들 수 있는 상태인지 확인한다.
        return rope != null && rope.pos != null && rope.pos.Length > 1;
    }

    private bool NeedsLayoutRebuild() { // 포인트 수나 단면 설정이 바뀌어 배열/삼각형 재생성이 필요한지 확인한다.
        return cachedRawPointCount != rope.pos.Length
            || cachedRadialSegments != RadialSegments
            || cachedCapEnds != capEnds
            || cachedEnableSmoothing != enableSmoothing
            || cachedSmoothingIterations != SmoothingIterations;
    }

    private void RebuildMeshLayout() { // 정점, 노멀, UV, 삼각형 배열 크기와 인덱스 구조를 다시 만든다.
        mesh.Clear();

        cachedRawPointCount = rope.pos.Length;
        cachedPointCount = GetSmoothedPointCount(cachedRawPointCount);
        cachedRadialSegments = RadialSegments;
        cachedCapEnds = capEnds;
        cachedEnableSmoothing = enableSmoothing;
        cachedSmoothingIterations = SmoothingIterations;

        int vertexCount = SideVertexCount;
        if (capEnds)
            vertexCount += (RingVertexCount + 1) * 2;

        rawLocalCenters = new Vector3[cachedRawPointCount];
        localCenters = new Vector3[cachedPointCount];
        smoothingBufferA = new Vector3[cachedPointCount];
        smoothingBufferB = new Vector3[cachedPointCount];
        vertices = new Vector3[vertexCount];
        normals = new Vector3[vertexCount];
        uvs = new Vector2[vertexCount];

        int sideTriangleCount = (cachedPointCount - 1) * cachedRadialSegments * 6;
        int capTriangleCount = capEnds ? cachedRadialSegments * 6 : 0;
        triangles = new int[sideTriangleCount + capTriangleCount];

        BuildSideTriangles();
        if (capEnds)
            BuildCapTriangles(sideTriangleCount);
    }

    private void UpdateLocalCenters() { // VerletRope의 월드 좌표 중심선을 이 오브젝트의 로컬 좌표로 변환한다.
        UpdateRawLocalCenters();
        BuildSmoothedCenters();
    }

    private void UpdateRawLocalCenters() { // VerletRope 원본 중심선을 로컬 좌표 배열로 변환한다.
        for (int i = 0; i < cachedRawPointCount; i++)
            rawLocalCenters[i] = transform.InverseTransformPoint(rope.pos[i]);
    }

    private void BuildSmoothedCenters() { // 원본 중심선에 렌더링 전용 smoothing을 적용해 최종 중심선을 만든다.
        if (SmoothingIterations == 0) {
            for (int i = 0; i < cachedRawPointCount; i++)
                localCenters[i] = rawLocalCenters[i];
            return;
        }

        Vector3[] source = rawLocalCenters;
        Vector3[] target = smoothingBufferA;
        int sourceCount = cachedRawPointCount;

        for (int iteration = 0; iteration < SmoothingIterations; iteration++) {
            int targetCount = ApplyChaikin(source, sourceCount, target);
            source = target;
            sourceCount = targetCount;
            target = target == smoothingBufferA ? smoothingBufferB : smoothingBufferA;
        }

        for (int i = 0; i < cachedPointCount; i++)
            localCenters[i] = source[i];
    }

    private int ApplyChaikin(Vector3[] source, int sourceCount, Vector3[] target) { // 한 번의 Chaikin corner cutting으로 꺾인 중심선을 부드럽게 만든다.
        int targetIndex = 0;
        target[targetIndex++] = source[0];

        for (int i = 0; i < sourceCount - 1; i++) {
            Vector3 current = source[i];
            Vector3 next = source[i + 1];
            target[targetIndex++] = Vector3.Lerp(current, next, 0.25f);
            target[targetIndex++] = Vector3.Lerp(current, next, 0.75f);
        }

        target[targetIndex++] = source[sourceCount - 1];
        return targetIndex;
    }

    private int GetSmoothedPointCount(int rawPointCount) { // smoothing 반복 횟수에 따른 최종 렌더링 중심점 개수를 계산한다.
        int count = rawPointCount;
        for (int i = 0; i < SmoothingIterations; i++)
            count = count * 2;

        return count;
    }

    private void UpdateMeshData() { // 현재 중심선 기준으로 정점, 노멀, UV 값을 갱신한다.
        float accumulatedDistance = 0.0f;
        Vector3 previousNormal = Vector3.up;

        for (int pointIndex = 0; pointIndex < cachedPointCount; pointIndex++) {
            if (pointIndex > 0)
                accumulatedDistance += Vector3.Distance(localCenters[pointIndex - 1], localCenters[pointIndex]);

            BuildRingFrame(pointIndex, previousNormal, out Vector3 forward, out Vector3 normal, out Vector3 binormal);
            previousNormal = normal;

            for (int segmentIndex = 0; segmentIndex < RingVertexCount; segmentIndex++) {
                float t = (float)segmentIndex / cachedRadialSegments;
                float angle = t * Mathf.PI * 2.0f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                Vector3 ringOffset = normal * cos * WidthRadius + binormal * sin * HeightRadius;
                Vector3 ringNormal = (normal * cos / WidthRadius + binormal * sin / HeightRadius).normalized;
                int vertexIndex = GetSideVertexIndex(pointIndex, segmentIndex);

                vertices[vertexIndex] = localCenters[pointIndex] + ringOffset;
                normals[vertexIndex] = ringNormal;
                uvs[vertexIndex] = new Vector2(t, accumulatedDistance * TextureTilingPerMeter);
            }

            if (capEnds && pointIndex == 0)
                UpdateCap(0, StartCapRimStart, StartCapCenterIndex, -forward);
            else if (capEnds && pointIndex == cachedPointCount - 1)
                UpdateCap(pointIndex, EndCapRimStart, EndCapCenterIndex, forward);
        }
    }

    private void ApplyMeshData() { // 갱신된 배열 데이터를 Mesh에 적용한다.
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    private void BuildRingFrame(int pointIndex, Vector3 previousNormal, out Vector3 forward, out Vector3 normal, out Vector3 binormal) { // 특정 로프 포인트의 단면 방향 축을 계산한다.
        forward = GetForward(pointIndex);
        normal = pointIndex == 0
            ? GetInitialNormal(forward)
            : Vector3.ProjectOnPlane(previousNormal, forward);

        if (normal.sqrMagnitude < 0.0001f)
            normal = GetInitialNormal(forward);

        normal.Normalize();
        binormal = Vector3.Cross(forward, normal).normalized;
    }

    private Vector3 GetForward(int pointIndex) { // 인접 중심점들을 이용해 해당 포인트의 진행 방향을 계산한다.
        Vector3 forward;
        if (pointIndex == 0)
            forward = localCenters[1] - localCenters[0];
        else if (pointIndex == cachedPointCount - 1)
            forward = localCenters[pointIndex] - localCenters[pointIndex - 1];
        else
            forward = localCenters[pointIndex + 1] - localCenters[pointIndex - 1];

        return forward.sqrMagnitude < 0.0001f ? Vector3.forward : forward.normalized;
    }

    private Vector3 GetInitialNormal(Vector3 forward) { // 첫 링에서 사용할 forward에 수직인 기준 normal을 계산한다.
        Vector3 normal = Vector3.ProjectOnPlane(Vector3.up, forward);
        if (normal.sqrMagnitude < 0.0001f)
            normal = Vector3.ProjectOnPlane(Vector3.right, forward);

        return normal.normalized;
    }

    private void BuildSideTriangles() { // 인접한 단면 링 사이를 연결하는 side 삼각형 인덱스를 만든다.
        int triangleIndex = 0;
        for (int pointIndex = 0; pointIndex < cachedPointCount - 1; pointIndex++) {
            for (int segmentIndex = 0; segmentIndex < cachedRadialSegments; segmentIndex++) {
                int current = GetSideVertexIndex(pointIndex, segmentIndex);
                int currentNext = GetSideVertexIndex(pointIndex, segmentIndex + 1);
                int next = GetSideVertexIndex(pointIndex + 1, segmentIndex);
                int nextNext = GetSideVertexIndex(pointIndex + 1, segmentIndex + 1);

                triangles[triangleIndex++] = current;
                triangles[triangleIndex++] = next;
                triangles[triangleIndex++] = currentNext;

                triangles[triangleIndex++] = currentNext;
                triangles[triangleIndex++] = next;
                triangles[triangleIndex++] = nextNext;
            }
        }
    }

    private void BuildCapTriangles(int triangleStartIndex) { // 튜브 양 끝을 막는 cap 삼각형 인덱스를 만든다.
        int triangleIndex = triangleStartIndex;
        for (int segmentIndex = 0; segmentIndex < cachedRadialSegments; segmentIndex++) {
            triangles[triangleIndex++] = StartCapCenterIndex;
            triangles[triangleIndex++] = StartCapRimStart + segmentIndex + 1;
            triangles[triangleIndex++] = StartCapRimStart + segmentIndex;

            triangles[triangleIndex++] = EndCapCenterIndex;
            triangles[triangleIndex++] = EndCapRimStart + segmentIndex;
            triangles[triangleIndex++] = EndCapRimStart + segmentIndex + 1;
        }
    }

    private int GetSideVertexIndex(int pointIndex, int segmentIndex) { // 포인트/세그먼트 위치에 해당하는 side 정점 배열 인덱스를 반환한다.
        return pointIndex * RingVertexCount + segmentIndex;
    }

    private void UpdateCap(int pointIndex, int rimStartIndex, int centerIndex, Vector3 capNormal) { // cap 전용 정점, 노멀, UV 값을 갱신한다.
        for (int segmentIndex = 0; segmentIndex < RingVertexCount; segmentIndex++) {
            int sideIndex = GetSideVertexIndex(pointIndex, segmentIndex);
            int capIndex = rimStartIndex + segmentIndex;

            vertices[capIndex] = vertices[sideIndex];
            normals[capIndex] = capNormal;

            float t = (float)segmentIndex / cachedRadialSegments;
            float angle = t * Mathf.PI * 2.0f;
            uvs[capIndex] = new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f);
        }

        vertices[centerIndex] = localCenters[pointIndex];
        normals[centerIndex] = capNormal;
        uvs[centerIndex] = new Vector2(0.5f, 0.5f);
    }
}
