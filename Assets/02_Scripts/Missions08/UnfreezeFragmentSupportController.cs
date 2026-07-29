using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualRescue.Missions08
{
    [DisallowMultipleComponent]
    public sealed class UnfreezeFragmentSupportController : MonoBehaviour
    {
        [Header("Graph")]
        [SerializeField, Min(0.0001f)] private float _vertexTolerance = 0.002f;
        [SerializeField, Min(2)] private int _minimumSharedVertices = 3;
        [SerializeField, Min(0f)] private float _anchorMargin = 0.08f;

        [Header("Frame Anchors")]
        [SerializeField] private bool _anchorLeft = true;
        [SerializeField] private bool _anchorRight = true;
        [SerializeField] private bool _anchorTop = true;
        [SerializeField] private bool _anchorBottom = true;

        [Header("Unsupported Collapse")]
        [SerializeField, Min(0f)] private float _collapseDelay = 0.15f;
        [SerializeField, Min(0f)] private float _randomDelay = 0.1f;
        [SerializeField, Min(0f)] private float _downwardImpulse = 0.25f;

        private readonly List<FragmentData> _fragments = new List<FragmentData>();
        private readonly HashSet<FragmentData> _supportedFragments =
            new HashSet<FragmentData>();
        private readonly HashSet<FragmentData> _pendingFragments =
            new HashSet<FragmentData>();
        private readonly Queue<FragmentData> _supportQueue =
            new Queue<FragmentData>();

        private void Awake()
        {
            BuildSupportGraph();
        }

        private void Update()
        {
            bool releasedStateChanged = false;
            foreach (FragmentData fragment in _fragments)
            {
                bool isReleased = fragment.IsReleased;
                if (isReleased == fragment.WasReleased)
                {
                    continue;
                }

                fragment.WasReleased = isReleased;
                releasedStateChanged = true;
            }

            if (releasedStateChanged)
            {
                EvaluateSupport();
            }
        }

        private void OnValidate()
        {
            _vertexTolerance = Mathf.Max(0.0001f, _vertexTolerance);
            _minimumSharedVertices = Mathf.Max(2, _minimumSharedVertices);
            _anchorMargin = Mathf.Max(0f, _anchorMargin);
            _collapseDelay = Mathf.Max(0f, _collapseDelay);
            _randomDelay = Mathf.Max(0f, _randomDelay);
            _downwardImpulse = Mathf.Max(0f, _downwardImpulse);
        }

        private void BuildSupportGraph()
        {
            _fragments.Clear();
            UnfreezeFragment[] fragments =
                GetComponentsInChildren<UnfreezeFragment>(true);

            foreach (UnfreezeFragment fragment in fragments)
            {
                Rigidbody fragmentRigidbody = fragment.GetComponent<Rigidbody>();
                MeshFilter meshFilter = fragment.GetComponent<MeshFilter>();
                if (fragmentRigidbody == null ||
                    meshFilter == null ||
                    meshFilter.sharedMesh == null)
                {
                    continue;
                }

                Vector3[] vertices = meshFilter.sharedMesh.vertices;
                if (vertices.Length == 0)
                {
                    continue;
                }

                var vertexKeys = new HashSet<VertexKey>();
                Vector3 firstVertex = transform.InverseTransformPoint(
                    meshFilter.transform.TransformPoint(vertices[0]));
                Bounds localBounds = new Bounds(firstVertex, Vector3.zero);

                foreach (Vector3 vertex in vertices)
                {
                    Vector3 localVertex = transform.InverseTransformPoint(
                        meshFilter.transform.TransformPoint(vertex));
                    localBounds.Encapsulate(localVertex);
                    vertexKeys.Add(new VertexKey(localVertex, _vertexTolerance));
                }

                _fragments.Add(
                    new FragmentData(fragmentRigidbody, vertexKeys, localBounds));
            }

            if (_fragments.Count == 0)
            {
                Debug.LogWarning(
                    "공중 파편 지지 검사를 위한 UnfreezeFragment를 찾을 수 없습니다.",
                    this);
                return;
            }

            Bounds wallBounds = _fragments[0].Bounds;
            for (int i = 1; i < _fragments.Count; i++)
            {
                wallBounds.Encapsulate(_fragments[i].Bounds.min);
                wallBounds.Encapsulate(_fragments[i].Bounds.max);
            }

            GetWallPlaneAxes(
                wallBounds.size,
                out int horizontalAxis,
                out int verticalAxis);
            foreach (FragmentData fragment in _fragments)
            {
                fragment.IsAnchor = IsFrameAnchor(
                    fragment.Bounds,
                    wallBounds,
                    horizontalAxis,
                    verticalAxis);
                fragment.WasReleased = fragment.IsReleased;
            }

            for (int i = 0; i < _fragments.Count - 1; i++)
            {
                for (int j = i + 1; j < _fragments.Count; j++)
                {
                    if (CountSharedVertices(
                        _fragments[i].Vertices,
                        _fragments[j].Vertices) < _minimumSharedVertices)
                    {
                        continue;
                    }

                    _fragments[i].Neighbors.Add(_fragments[j]);
                    _fragments[j].Neighbors.Add(_fragments[i]);
                }
            }
        }

        private void EvaluateSupport()
        {
            _supportedFragments.Clear();
            _supportQueue.Clear();

            foreach (FragmentData fragment in _fragments)
            {
                if (!fragment.IsAnchor ||
                    fragment.IsReleased ||
                    !_supportedFragments.Add(fragment))
                {
                    continue;
                }

                _supportQueue.Enqueue(fragment);
            }

            while (_supportQueue.Count > 0)
            {
                FragmentData currentFragment = _supportQueue.Dequeue();
                foreach (FragmentData neighbor in currentFragment.Neighbors)
                {
                    if (neighbor.IsReleased ||
                        !_supportedFragments.Add(neighbor))
                    {
                        continue;
                    }

                    _supportQueue.Enqueue(neighbor);
                }
            }

            foreach (FragmentData fragment in _fragments)
            {
                if (fragment.IsReleased ||
                    _supportedFragments.Contains(fragment) ||
                    !_pendingFragments.Add(fragment))
                {
                    continue;
                }

                StartCoroutine(CollapseAfterDelay(fragment));
            }
        }

        private IEnumerator CollapseAfterDelay(FragmentData fragment)
        {
            float delay = _collapseDelay + UnityEngine.Random.Range(0f, _randomDelay);
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            _pendingFragments.Remove(fragment);
            if (fragment.IsReleased)
            {
                yield break;
            }

            fragment.Rigidbody.constraints = RigidbodyConstraints.None;
            fragment.Rigidbody.AddForce(
                Vector3.down * _downwardImpulse,
                ForceMode.Impulse);
        }

        private bool IsFrameAnchor(
            Bounds pieceBounds,
            Bounds wallBounds,
            int horizontalAxis,
            int verticalAxis)
        {
            bool touchesLeft =
                _anchorLeft &&
                GetAxis(pieceBounds.min, horizontalAxis) <=
                GetAxis(wallBounds.min, horizontalAxis) + _anchorMargin;
            bool touchesRight =
                _anchorRight &&
                GetAxis(pieceBounds.max, horizontalAxis) >=
                GetAxis(wallBounds.max, horizontalAxis) - _anchorMargin;
            bool touchesBottom =
                _anchorBottom &&
                GetAxis(pieceBounds.min, verticalAxis) <=
                GetAxis(wallBounds.min, verticalAxis) + _anchorMargin;
            bool touchesTop =
                _anchorTop &&
                GetAxis(pieceBounds.max, verticalAxis) >=
                GetAxis(wallBounds.max, verticalAxis) - _anchorMargin;

            return touchesLeft || touchesRight || touchesBottom || touchesTop;
        }

        private static void GetWallPlaneAxes(
            Vector3 size,
            out int horizontalAxis,
            out int verticalAxis)
        {
            int smallestAxis = 0;
            if (size.y < GetAxis(size, smallestAxis))
            {
                smallestAxis = 1;
            }

            if (size.z < GetAxis(size, smallestAxis))
            {
                smallestAxis = 2;
            }

            int firstPlaneAxis = smallestAxis == 0 ? 1 : 0;
            int secondPlaneAxis = smallestAxis == 2 ? 1 : 2;
            if (GetAxis(size, firstPlaneAxis) >= GetAxis(size, secondPlaneAxis))
            {
                horizontalAxis = firstPlaneAxis;
                verticalAxis = secondPlaneAxis;
            }
            else
            {
                horizontalAxis = secondPlaneAxis;
                verticalAxis = firstPlaneAxis;
            }
        }

        private static float GetAxis(Vector3 value, int axis)
        {
            switch (axis)
            {
                case 0:
                    return value.x;
                case 1:
                    return value.y;
                default:
                    return value.z;
            }
        }

        private static int CountSharedVertices(
            HashSet<VertexKey> first,
            HashSet<VertexKey> second)
        {
            HashSet<VertexKey> smaller =
                first.Count <= second.Count ? first : second;
            HashSet<VertexKey> larger =
                first.Count <= second.Count ? second : first;

            int sharedCount = 0;
            foreach (VertexKey vertex in smaller)
            {
                if (larger.Contains(vertex))
                {
                    sharedCount++;
                }
            }

            return sharedCount;
        }

        private sealed class FragmentData
        {
            public FragmentData(
                Rigidbody rigidbody,
                HashSet<VertexKey> vertices,
                Bounds bounds)
            {
                Rigidbody = rigidbody;
                Vertices = vertices;
                Bounds = bounds;
            }

            public Rigidbody Rigidbody { get; }
            public HashSet<VertexKey> Vertices { get; }
            public Bounds Bounds { get; }
            public List<FragmentData> Neighbors { get; } =
                new List<FragmentData>();
            public bool IsAnchor { get; set; }
            public bool WasReleased { get; set; }
            public bool IsReleased =>
                Rigidbody == null ||
                Rigidbody.constraints == RigidbodyConstraints.None;
        }

        private readonly struct VertexKey : IEquatable<VertexKey>
        {
            private readonly int _x;
            private readonly int _y;
            private readonly int _z;

            public VertexKey(Vector3 vertex, float tolerance)
            {
                _x = Mathf.RoundToInt(vertex.x / tolerance);
                _y = Mathf.RoundToInt(vertex.y / tolerance);
                _z = Mathf.RoundToInt(vertex.z / tolerance);
            }

            public bool Equals(VertexKey other)
            {
                return _x == other._x &&
                    _y == other._y &&
                    _z == other._z;
            }

            public override bool Equals(object obj)
            {
                return obj is VertexKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = _x;
                    hashCode = (hashCode * 397) ^ _y;
                    hashCode = (hashCode * 397) ^ _z;
                    return hashCode;
                }
            }
        }
    }
}
