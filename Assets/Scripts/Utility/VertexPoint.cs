using UnityEngine;

[ExecuteAlways]
public class VertexPoint : MonoBehaviour
{
    public GF_Rope2 targetRope;

    void Update()
    {
        if (targetRope == null) return;

        var arc = targetRope.GetArcPoints();
        if (arc == null || arc.Count == 0) return;

        // Get midpoint (t = 0.5)
        float midT = 0.5f;
        float scaledIndex = midT * (arc.Count - 1);
        int idx = Mathf.FloorToInt(scaledIndex);
        int nextIdx = Mathf.Min(idx + 1, arc.Count - 1);
        float lerpT = scaledIndex - idx;

        Vector3 midpoint = Vector3.Lerp(arc[idx], arc[nextIdx], lerpT);
        transform.position = midpoint;
    }
}
