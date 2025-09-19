using UnityEngine;

public class InterpolatedPoint : MonoBehaviour
{
    public GF_Rope2 rope1;
    public GF_Rope2 rope2;

    public bool usePointA = true;  // If true, interpolate between PointA positions, else PointB
    [Range(0f, 1f)]
    public float t = 0f;           // Interpolation value: 0 = rope1 point, 1 = rope2 point

    void Update()
    {
        if (rope1 == null || rope2 == null)
        {
            Debug.LogWarning("Assign both rope1 and rope2.");
            return;
        }

        Transform p1 = usePointA ? rope1.PointA.transform : rope1.PointB.transform;
        Transform p2 = usePointA ? rope2.PointA.transform : rope2.PointB.transform;

        if (p1 == null || p2 == null)
        {
            Debug.LogWarning("PointA or PointB references are missing in one of the ropes.");
            return;
        }

        transform.position = Vector3.Lerp(p1.position, p2.position, t);
    }
}
