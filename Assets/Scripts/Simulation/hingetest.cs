using UnityEngine;

public class RopeGenerator : MonoBehaviour
{
    [Header("Endpoints")]
    public Transform startPoint;
    public Transform endPoint;

    [Header("Rope Settings")]
    public float ropeLength = 5f;
    public int segments = 10;
    public float segmentMass = 0.1f;
    public float segmentRadius = 0.05f;

    [Header("Debug")]
    public bool generateOnStart = true;

    private void Start()
    {
        if (generateOnStart)
        {
            GenerateRope();
        }
    }

    public void GenerateRope()
    {
        if (startPoint == null || endPoint == null)
        {
            Debug.LogError("❌ RopeGenerator: Please assign Start and End points.");
            return;
        }

        float segmentLength = ropeLength / segments;
        Vector3 direction = (endPoint.position - startPoint.position).normalized;

        Rigidbody previousRb = null;

        for (int i = 0; i <= segments; i++)
        {
            // Interpolate position from start to end
            Vector3 pos = Vector3.Lerp(startPoint.position, endPoint.position, (float)i / segments);

            // Create a small capsule to act as rope segment
            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            segment.name = $"RopeSegment_{i}";
            segment.transform.position = pos;
            segment.transform.localScale = new Vector3(segmentRadius, segmentLength * 0.5f, segmentRadius);
            segment.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);

            Rigidbody rb = segment.AddComponent<Rigidbody>();
            rb.mass = segmentMass;

            // Ignore collision between this and previous segment
            if (previousRb != null)
            {
                Physics.IgnoreCollision(segment.GetComponent<Collider>(), previousRb.GetComponent<Collider>());
            }

            // Add hinge joint to connect to previous
            if (previousRb != null)
            {
                HingeJoint joint = segment.AddComponent<HingeJoint>();
                joint.connectedBody = previousRb;
                joint.axis = Vector3.forward; // Rotate around Z by default
                joint.anchor = new Vector3(0, -0.5f, 0);
                joint.connectedAnchor = new Vector3(0, 0.5f, 0);
            }

            // Fix first and last segments to endpoints
            if (i == 0)
            {
                FixedJoint fj = segment.AddComponent<FixedJoint>();
                fj.connectedBody = startPoint.GetComponent<Rigidbody>();
            }
            else if (i == segments)
            {
                FixedJoint fj = segment.AddComponent<FixedJoint>();
                fj.connectedBody = endPoint.GetComponent<Rigidbody>();
            }

            previousRb = rb;
        }
    }
}
