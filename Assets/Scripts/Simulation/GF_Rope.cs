using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class GF_Rope : MonoBehaviour
{
    public GameObject pointA;
    public GameObject pointB;

    public float ropeLength = 5f;
    public float segmentSpacing = 0.2f;
    public int simulationSteps = 500;
    public int constraintIterations = 5;
    public float gravity = -9.81f;

    [Header("Weights")]
    public bool tipWeight = false;
    public bool balancedWeights = false;
    [Range(0f, 1f)] public float balancedWeightT = 0.7f;
    public float weightStrength = 1f;

    [Header("Debug")]
    public bool showDebug = true;

    private List<Vector3> ropePoints = new();

    public UnityEvent OnRopeUpdated = new();

    void Update(){
        SimulateRope();
    }

    public void SimulateRope()
    {
        ropePoints.Clear();

        if (!pointA || !pointB) return;

        Vector3 start = pointA.transform.position;
        Vector3 end = pointB.transform.position;

        int segmentCount = Mathf.Max(3, Mathf.CeilToInt(ropeLength / segmentSpacing) + 1);
        if (segmentCount % 2 == 0) segmentCount++; // odd for center tip

        float segmentLength = ropeLength / (segmentCount - 1);

        List<Vector3> current = new(segmentCount);
        List<Vector3> previous = new(segmentCount);

        for (int i = 0; i < segmentCount; i++)
        {
            Vector3 p = Vector3.Lerp(start, end, i / (float)(segmentCount - 1));
            current.Add(p);
            previous.Add(p);
        }

        int tipIndex = segmentCount / 2;
        int balA = Mathf.RoundToInt(balancedWeightT * tipIndex);
        int balB = segmentCount - 1 - balA;

        for (int step = 0; step < simulationSteps; step++)
        {
            // Verlet integration
            for (int i = 1; i < segmentCount - 1; i++)
            {
                Vector3 vel = current[i] - previous[i];
                previous[i] = current[i];

                Vector3 force = Vector3.up * gravity * Time.fixedDeltaTime * Time.fixedDeltaTime;

                // Weight influence
                if ((tipWeight && i == tipIndex) || 
                    (balancedWeights && (i == balA || i == balB)))
                    force *= (1f + weightStrength);

                current[i] += vel + force;
            }

            // Constraint pass
            for (int c = 0; c < constraintIterations; c++)
            {
                current[0] = start;
                current[^1] = end;

                for (int i = 0; i < segmentCount - 1; i++)
                {
                    Vector3 delta = current[i + 1] - current[i];
                    float dist = delta.magnitude;
                    if (dist == 0f) continue;

                    Vector3 correction = delta * (1 - segmentLength / dist) * 0.5f;
                    if (i != 0) current[i] += correction;
                    if (i + 1 != segmentCount - 1) current[i + 1] -= correction;
                }
            }
        }

        ropePoints.AddRange(current);
        OnRopeUpdated.Invoke();
    }

    public List<Vector3> GetArcPoints()
    {
        if (ropePoints.Count == 0) SimulateRope();
        return ropePoints;
    }

    private void OnDrawGizmos()
    {
        if (!showDebug || ropePoints.Count < 2) return;

        Gizmos.color = Color.yellow;
        foreach (var p in ropePoints)
            Gizmos.DrawSphere(p, 0.01f);

        for (int i = 0; i < ropePoints.Count - 1; i++)
            Gizmos.DrawLine(ropePoints[i], ropePoints[i + 1]);
    }
}
