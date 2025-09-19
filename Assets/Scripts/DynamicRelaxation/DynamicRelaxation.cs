using UnityEngine;
using UnityEngine.Jobs;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using System.Collections.Generic;

public class DynamicRelaxationWeb : MonoBehaviour
{
    [System.Serializable]
    public class Element
    {
        public int id;
        public int nodeA;
        public int nodeB;
        [HideInInspector] public float restLength;
        [Tooltip("If > 0, overrides restLength and acts as rope length constraint.")]
        public float ropeLength = 1;
    }

    [System.Serializable]
    public class ElementGroup
    {
        [Tooltip("All elements in this group will have their ropeLength set to groupLength.")]
        public List<Element> elements = new();
        public float groupLength = 1;
    }

    [Header("Setup")]
    public List<Transform> nodes = new();
    public List<Element> elements = new();
    public List<ElementGroup> groups = new(); // New groups list
    public List<bool> fixedNodes = new();

    [Header("Prefabs")]
    public GameObject lineRendererPrefab;

    [Header("Simulation")]
    [Range(0f, 1f)] public float damping = 0.98f;
    public float timeStep = 0.02f;
    public float ropeOffset = 10f;
    public Vector3 gravity = new(0, -9.81f, 0);
    [Min(1)] public int constraintIterations = 1;

    // Native simulation state
    private NativeArray<float3> _positions;
    private NativeArray<float3> _previous;
    private NativeArray<float3> _forces;
    private NativeArray<byte>   _fixedMask;
    private NativeArray<Edge>   _edges;
    private TransformAccessArray _taa;
    private readonly List<LineRenderer> _lineRenderers = new();
    private bool _allocated;

    private struct Edge
    {
        public int A;
        public int B;
        public float TargetLen;
    }

    #region Jobs

    [BurstCompile]
    private struct ZeroForcesJob : IJobParallelFor
    {
        public NativeArray<float3> Forces;
        public void Execute(int i) => Forces[i] = 0f;
    }

    [BurstCompile]
    private struct ApplyExternalForcesJob : IJobParallelFor
    {
        public NativeArray<float3> Forces;
        [ReadOnly] public NativeArray<byte> FixedMask;
        public float3 Gravity;

        public void Execute(int i)
        {
            if (FixedMask[i] == 1) return;
            Forces[i] += Gravity;
        }
    }

    [BurstCompile]
    private struct IntegrateVerletJob : IJobParallelFor
    {
        public NativeArray<float3> Positions;
        public NativeArray<float3> Previous;
        [ReadOnly] public NativeArray<float3> Forces;
        [ReadOnly] public NativeArray<byte> FixedMask;
        public float Damping;
        public float Dt2;

        public void Execute(int i)
        {
            if (FixedMask[i] == 1) return;

            float3 current = Positions[i];
            float3 prev    = Previous[i];
            float3 vel     = (current - prev) * Damping;
            float3 next    = current + vel + Forces[i] * Dt2;

            Previous[i]  = current;
            Positions[i] = next;
        }
    }

    /// Single-pass rope-length projection across all edges (Burst). Repeat this job N times for stiffer results.
    [BurstCompile]
    private struct RopeConstraintPassJob : IJob
    {
        public NativeArray<float3> Positions;
        [ReadOnly] public NativeArray<byte> FixedMask;
        [ReadOnly] public NativeArray<Edge> Edges;

        public void Execute()
        {
            int m = Edges.Length;
            for (int i = 0; i < m; i++)
            {
                Edge e = Edges[i];
                int ia = e.A, ib = e.B;
                float3 a = Positions[ia];
                float3 b = Positions[ib];
                float3 d = b - a;
                float dist = math.length(d);
                if (dist <= 1e-7f) continue;

                float target = math.max(0f, e.TargetLen);
                if (dist <= target) continue; // rope only: no stretch if shorter than target

                float3 n = d / dist;
                float3 corr = (dist - target) * n;

                bool aFixed = FixedMask[ia] == 1;
                bool bFixed = FixedMask[ib] == 1;

                if (!aFixed && !bFixed)
                {
                    a += 0.5f * corr;
                    b -= 0.5f * corr;
                }
                else if (!aFixed)
                {
                    a += corr;
                }
                else if (!bFixed)
                {
                    b -= corr;
                }

                Positions[ia] = a;
                Positions[ib] = b;
            }
        }
    }

    [BurstCompile]
    private struct WriteBackJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<float3> Positions;
        public void Execute(int index, TransformAccess transform)
        {
            transform.position = Positions[index];
        }
    }

    #endregion

    #region Public API

    private int _nextElementID = 0;

    // Add an element and return its ID
    public int AddElement(Element element)
    {
        element.id = _nextElementID++;
        elements.Add(element);
        return element.id;
    }

    // Set rope length and sync by ID
    public void SetGroupLength(ElementGroup group, float newLength)
    {
        group.groupLength = newLength;
        foreach (var e in group.elements)
        {
            e.ropeLength = newLength;
        }
        // Do NOT call SetupSimulation() here!
    }

    #endregion

    #region Lifecycle

    private void OnDisable() => DisposeAll();
    private void OnDestroy() => DisposeAll();

    private void AllocateIfNeeded()
    {
        DisposeAll();

        int n = nodes.Count;
        if (n == 0) return;

        _positions = new NativeArray<float3>(n, Allocator.Persistent);
        _previous  = new NativeArray<float3>(n, Allocator.Persistent);
        _forces    = new NativeArray<float3>(n, Allocator.Persistent);
        _fixedMask = new NativeArray<byte>(n, Allocator.Persistent);
        _allocated = true;
    }

    private void DisposeAll()
    {
        if (_positions.IsCreated) _positions.Dispose();
        if (_previous.IsCreated)  _previous.Dispose();
        if (_forces.IsCreated)    _forces.Dispose();
        if (_fixedMask.IsCreated) _fixedMask.Dispose();
        if (_edges.IsCreated)     _edges.Dispose();
        if (_taa.isCreated)       _taa.Dispose();

        _allocated = false;
    }

    private void EnsureListSizes()
    {
        while (fixedNodes.Count < nodes.Count) fixedNodes.Add(false);
    }

    private bool IsValidNodeIndex(int index) => index >= 0 && index < nodes.Count;

    #endregion

    public void SetupSimulation()
    {
        EnsureListSizes();

        // Destroy old LRs
        for (int i = 0; i < _lineRenderers.Count; i++)
        {
            if (_lineRenderers[i] != null)
                Destroy(_lineRenderers[i].gameObject);
        }
        _lineRenderers.Clear();

        AllocateIfNeeded();

        // Initialize positions from current transforms
        for (int i = 0; i < nodes.Count; i++)
        {
            var t = nodes[i];
            float3 p = t != null ? (float3)t.position : (float3)transform.position;
            _positions[i] = p;
            _previous[i]  = p;
            _fixedMask[i] = (byte)(fixedNodes[i] ? 1 : 0);
        }

        // Build edges (elements) + create LRs
        var edges = new List<Edge>(elements.Count);
        for (int i = 0; i < elements.Count; i++)
        {
            var e = elements[i];
            if (!IsValidNodeIndex(e.nodeA) || !IsValidNodeIndex(e.nodeB)) continue;

            var a = nodes[e.nodeA];
            var b = nodes[e.nodeB];
            if (a == null || b == null) continue;

            e.restLength = Vector3.Distance(a.position, b.position);
            float target = (e.ropeLength > 0f ? e.ropeLength : e.restLength) - ropeOffset;
            if (target < 0f) target = 0f;

            edges.Add(new Edge { A = e.nodeA, B = e.nodeB, TargetLen = target });

            if (lineRendererPrefab != null)
            {
                var lrObj = Instantiate(lineRendererPrefab, Vector3.zero, Quaternion.identity, transform);
                var lr = lrObj.GetComponent<LineRenderer>();
                if (lr != null)
                {
                    lr.positionCount = 2;
                    lr.SetPosition(0, a.position);
                    lr.SetPosition(1, b.position);
                    _lineRenderers.Add(lr);
                }
            }
        }

        // Copy edges native
        if (_edges.IsCreated) _edges.Dispose();
        _edges = new NativeArray<Edge>(edges.Count, Allocator.Persistent);
        for (int i = 0; i < edges.Count; i++) _edges[i] = edges[i];

        // Build TransformAccessArray
        if (_taa.isCreated) _taa.Dispose();
        _taa = new TransformAccessArray(nodes.Count);
        for (int i = 0; i < nodes.Count; i++)
            _taa.Add(nodes[i] != null ? nodes[i] : transform);
    }

    private void FixedUpdate()
    {
        if (!_allocated || !_taa.isCreated || nodes.Count == 0 || elements.Count == 0) return;

        // Keep fixed mask current (if toggled at runtime)
        for (int i = 0; i < nodes.Count; i++)
            _fixedMask[i] = (byte)(fixedNodes[i] ? 1 : 0);

        float dt2 = timeStep * timeStep;

        // 1) zero forces
        var j0 = new ZeroForcesJob { Forces = _forces }.Schedule(_forces.Length, 64);

        // 2) gravity
        var j1 = new ApplyExternalForcesJob
        {
            Forces = _forces,
            FixedMask = _fixedMask,
            Gravity = gravity
        }.Schedule(_forces.Length, 64, j0);

        // 3) integrate
        var j2 = new IntegrateVerletJob
        {
            Positions = _positions,
            Previous  = _previous,
            Forces    = _forces,
            FixedMask = _fixedMask,
            Damping   = damping,
            Dt2       = dt2
        }.Schedule(_positions.Length, 64, j1);

        // 4) constraints (repeat for stiffness)
        JobHandle last = j2;
        int iters = math.max(1, constraintIterations);
        for (int k = 0; k < iters; k++)
        {
            last = new RopeConstraintPassJob
            {
                Positions = _positions,
                FixedMask = _fixedMask,
                Edges     = _edges
            }.Schedule(last);
        }

        // 5) write back to transforms
        var jWrite = new WriteBackJob { Positions = _positions }.Schedule(_taa, last);
        jWrite.Complete();

        // 6) update LineRenderers on main thread (cheap)
        for (int i = 0; i < _lineRenderers.Count && i < elements.Count; i++)
        {
            var e = elements[i];
            if (!IsValidNodeIndex(e.nodeA) || !IsValidNodeIndex(e.nodeB)) continue;
            var lr = _lineRenderers[i];
            if (lr == null) continue;

            var a = nodes[e.nodeA];
            var b = nodes[e.nodeB];
            if (a == null || b == null) continue;

            lr.SetPosition(0, a.position);
            lr.SetPosition(1, b.position);
        }
    }

#if UNITY_EDITOR
    private Dictionary<ElementGroup, float> _lastGroupLengths = new();
    private bool _needsSimulationUpdate = false;

    private void OnValidate()
    {
        foreach (var group in groups)
        {
            if (group == null) continue;

            if (!_lastGroupLengths.ContainsKey(group))
                _lastGroupLengths[group] = group.groupLength;

            if (!Mathf.Approximately(_lastGroupLengths[group], group.groupLength))
            {
                SetGroupLength(group, group.groupLength);
                _lastGroupLengths[group] = group.groupLength;
                _needsSimulationUpdate = true; // Set flag instead of calling SetupSimulation
            }
        }
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (_needsSimulationUpdate)
        {
            SetupSimulation();
            _needsSimulationUpdate = false;
        }
#endif
    }
#endif
}
