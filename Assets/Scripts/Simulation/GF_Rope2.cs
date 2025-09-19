using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class GF_Rope2 : MonoBehaviour
{
    [Header("Endpoints")]
    [SerializeField] private GameObject pointA;
    [SerializeField] private GameObject pointB;

    [Header("Rope Settings")]
    [SerializeField] private float ropeLength = 80f;
    [SerializeField] private int totalNodes = 51;
    [SerializeField] private float ropeWidth = 0.1f;

    [Header("Physics Settings")]
    [SerializeField] private float gravityStrength = 2f;
    [SerializeField, Range(0, 1)] private float velocityDampen = 0.9f;
    [SerializeField] private float simulationSpeed = 50f;
    [SerializeField] private int iterations = 50;

    [Header("Weights")]
    [SerializeField] private bool tipWeight = false;
    [SerializeField] private bool balancedWeights = false;
    [SerializeField, Range(0f, 1f)] private float balancedWeightT = 0.7f;
    [SerializeField] private float weightStrength = 30f;

    [Header("Debug")]
    public bool drawPerpendicularLine = true;
    [SerializeField] private LineRenderer perpendicularLineRenderer;

    [Header("Linked Ropes")]
    [SerializeField] private List<GF_Rope2> linkedRopes = new();

    // Internal state
    private float nodeDistance;
    private Vector3 gravity;
    private Vector3[] previousNodePositions;
    private Vector3[] currentNodePositions;
    private Vector3[] linePositions;
    private Vector3[] perpendicularLinePositions;

    private LineRenderer lineRenderer;
    private BoxCollider ropeCollider;

    // --- Properties ---
    public bool TipWeight
    {
        get => tipWeight;
        set => SetAndSync(ref tipWeight, value, nameof(TipWeight));
    }

    public bool BalancedWeights
    {
        get => balancedWeights;
        set => SetAndSync(ref balancedWeights, value, nameof(BalancedWeights));
    }

    public float BalancedWeightT
    {
        get => balancedWeightT;
        set => SetAndSync(ref balancedWeightT, value, nameof(BalancedWeightT));
    }

    public float RopeLength
    {
        get => ropeLength;
        set => SetAndSync(ref ropeLength, value, nameof(RopeLength));
    }

    public GameObject PointA => pointA;
    public GameObject PointB => pointB;

    // --- Sequence ---
    private void Awake()
    {
        gravity = new Vector3(0, -gravityStrength, 0);
        lineRenderer = GetComponent<LineRenderer>();

        AllocateBuffers();
        nodeDistance = ropeLength / (totalNodes - 1);

        if (HasValidEndpoints())
            InitializeRope(pointA.transform.position, pointB.transform.position);
    }

    private void Update()
    {
        if (HasValidEndpoints())
            DrawRope();
    }

    private void FixedUpdate()
    {
        if (!HasValidEndpoints()) return;

        Simulate();
        for (int i = 0; i < iterations; i++)
            ApplyConstraints();
    }

    // --- Initialization ---
    private void AllocateBuffers()
    {
        currentNodePositions = new Vector3[totalNodes];
        previousNodePositions = new Vector3[totalNodes];
        linePositions = new Vector3[totalNodes];
        perpendicularLinePositions = new Vector3[totalNodes];
    }

    public void InitializeRope(Vector3 start, Vector3 end)
    {
        nodeDistance = ropeLength / (totalNodes - 1);
        for (int i = 0; i < totalNodes; i++)
        {
            Vector3 pos = Vector3.Lerp(start, end, i / (float)(totalNodes - 1));
            currentNodePositions[i] = previousNodePositions[i] = pos;
        }
    }

    // --- Simulation ---
    //Infer velocity, apply gravity, update positions
    private void Simulate()
    {
        float dt = Time.fixedDeltaTime;
        int tipIndex = totalNodes / 2;
        int balancedA = Mathf.RoundToInt(balancedWeightT * tipIndex);
        int balancedB = totalNodes - 1 - balancedA;

        for (int i = 0; i < totalNodes; i++)
        {
            Vector3 velocity = (currentNodePositions[i] - previousNodePositions[i]) * velocityDampen;
            previousNodePositions[i] = currentNodePositions[i];

            Vector3 pos = currentNodePositions[i] + velocity + gravity * dt * dt * simulationSpeed;

            if ((TipWeight && i == tipIndex) ||
                (BalancedWeights && (i == balancedA || i == balancedB)))
            {
                pos += gravity * dt * dt * simulationSpeed * weightStrength;
            }

            currentNodePositions[i] = pos;
        }
    }

    // Apply distance constraints
    private void ApplyConstraints()
    {
        nodeDistance = ropeLength / (totalNodes - 1);

        currentNodePositions[0] = pointA.transform.position;
        currentNodePositions[^1] = pointB.transform.position;

        int tipIndex = totalNodes / 2;

        // Spread constraints outward from center
        for (int offset = 1; offset < totalNodes; offset++)
        {
            if (tipIndex - offset >= 0)
                ApplyConstraintBetweenNodes(tipIndex - offset, tipIndex - offset + 1);

            if (tipIndex + offset < totalNodes)
                ApplyConstraintBetweenNodes(tipIndex + offset - 1, tipIndex + offset);
        }

        if (TipWeight)
            BalanceTip(tipIndex);
    }

    private void ApplyConstraintBetweenNodes(int i, int j)
    {
        Vector3 delta = currentNodePositions[j] - currentNodePositions[i];
        float dist = delta.magnitude;
        if (dist == 0) return;

        float diff = (dist - nodeDistance) / dist;
        Vector3 correction = delta * 0.5f * diff;

        if (i != 0)
            currentNodePositions[i] += correction;
        if (j != totalNodes - 1)
            currentNodePositions[j] -= correction;
    }

    private void BalanceTip(int tipIndex)
    {
        Vector3 tip = currentNodePositions[tipIndex];
        Vector3 leftEnd = currentNodePositions[0];
        Vector3 rightEnd = currentNodePositions[^1];

        float imbalance = (Vector3.Distance(tip, rightEnd) - Vector3.Distance(tip, leftEnd)) * 0.5f;

        Vector3 correction =
            (rightEnd - tip).normalized * imbalance -
            (leftEnd - tip).normalized * imbalance;

        correction = Vector3.ClampMagnitude(correction, nodeDistance * 0.5f);
        currentNodePositions[tipIndex] += correction;
    }

    // --- Rendering ---
    private void DrawRope()
    {
        lineRenderer.startWidth = lineRenderer.endWidth = ropeWidth;
        lineRenderer.positionCount = totalNodes;

        int tipIndex = totalNodes / 2;
        Vector3 tip = currentNodePositions[tipIndex];

        for (int i = 0; i < totalNodes; i++)
        {
            linePositions[i] = currentNodePositions[i];

            if (drawPerpendicularLine)
            {
                Vector3 local = currentNodePositions[i] - tip;
                Vector3 rotated = new(local.z, local.y, -local.x);
                perpendicularLinePositions[i] = rotated + tip;
            }
        }

        lineRenderer.SetPositions(linePositions);

        if (drawPerpendicularLine && perpendicularLineRenderer != null)
        {
            perpendicularLineRenderer.startWidth = perpendicularLineRenderer.endWidth = ropeWidth;
            perpendicularLineRenderer.positionCount = totalNodes;
            perpendicularLineRenderer.SetPositions(perpendicularLinePositions);
        }

        UpdateBoxCollider();
    }

    private void UpdateBoxCollider()
    {
        ropeCollider ??= gameObject.AddComponent<BoxCollider>();

        Vector3 start = currentNodePositions[0];
        Vector3 end = currentNodePositions[^1];
        Vector3 dir = end - start;

        Vector3 center = (start + end) * 0.5f;
        center.y -= ropeLength * 0.25f;

        ropeCollider.size = new Vector3(ropeWidth, ropeLength * 0.5f, dir.magnitude);
        ropeCollider.center = transform.InverseTransformPoint(center);
        ropeCollider.transform.rotation = Quaternion.LookRotation(dir);
    }

    // --- Utilities ---
    public List<Vector3> GetArcPoints() => new(currentNodePositions);

    public void SetEndpoints(GameObject a, GameObject b)
    {
        pointA = a;
        pointB = b;
    }

    private bool HasValidEndpoints() => pointA && pointB;

    private void SetAndSync<T>(ref T field, T value, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        SyncPropertyToLinkedRopes(propertyName, value);
    }

    private void SyncPropertyToLinkedRopes(string propertyName, object value)
    {
        foreach (var rope in linkedRopes)
        {
            if (rope == null) continue;

            switch (propertyName)
            {
                case nameof(TipWeight): rope.TipWeight = (bool)value; break;
                case nameof(BalancedWeights): rope.BalancedWeights = (bool)value; break;
                case nameof(BalancedWeightT): rope.BalancedWeightT = (float)value; break;
                case nameof(RopeLength): rope.RopeLength = (float)value; break;
            }
        }
    }

    // --- UI Interaction ---
    private void OnMouseDown()
    {
        FindObjectOfType<UIManager>()?.ShowRopeSettings(this);
    }
}
