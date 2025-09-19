using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class GF_TentRoof : MonoBehaviour
{
    [Header("Reference Ropes")]
    public GF_Rope2 ropeBottom;
    public GF_Rope2 ropeTop;

    [Header("Mesh settings")]
    public Material meshMaterial;
    public Material meshMaterial2;

    private LineRenderer lineRenderer;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        // Setup basic LineRenderer parameters
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.widthMultiplier = 0.02f;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.numCapVertices = 2;
        lineRenderer.numCornerVertices = 2;
    }

    void Update()
    {
        if (ropeBottom == null || ropeTop == null)
        {
            Debug.LogWarning("Assign ropeBottom and ropeTop.");
            return;
        }

        List<Vector3> arcB = ropeBottom.GetArcPoints();
        List<Vector3> arcT = ropeTop.GetArcPoints();

        if (arcB == null || arcT == null || arcB.Count == 0 || arcT.Count == 0)
        {
            Debug.LogWarning("Missing arc data from ropes.");
            return;
        }

        Vector3 midB = SampleArcAt(arcB, 0.5f);
        Vector3 midT = SampleArcAt(arcT, 0.5f);

        lineRenderer.SetPosition(0, midB);
        lineRenderer.SetPosition(1, midT);
    }

    private Vector3 SampleArcAt(List<Vector3> arc, float t)
    {
        if (arc == null || arc.Count == 0) return Vector3.zero;

        float scaledIndex = t * (arc.Count - 1);
        int idx = Mathf.FloorToInt(scaledIndex);
        int nextIdx = Mathf.Min(idx + 1, arc.Count - 1);
        float lerpT = scaledIndex - idx;

        return Vector3.Lerp(arc[idx], arc[nextIdx], lerpT);
    }

    public void GenerateMesh(Transform meshParent, Material material = null)
    {
        if (ropeBottom == null || ropeTop == null)
        {
            Debug.LogWarning("Assign ropeBottom and ropeTop.");
            return;
        }

        List<Vector3> arcB = ropeBottom.GetArcPoints();
        List<Vector3> arcT = ropeTop.GetArcPoints();

        if (arcB == null || arcT == null || arcB.Count < 2 || arcT.Count < 2 || arcB.Count != arcT.Count)
        {
            Debug.LogWarning("Invalid arc data. Ensure ropes have same number of arc points.");
            return;
        }

        // Convert to local space
        for (int i = 0; i < arcB.Count; i++)
        {
            arcB[i] = transform.InverseTransformPoint(arcB[i]);
            arcT[i] = transform.InverseTransformPoint(arcT[i]);
        }

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector2> uv2s = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();

        int segmentCount = arcB.Count;

        for (int i = 0; i < segmentCount; i++)
        {
            Vector3 vBottom = arcB[i];
            Vector3 vTop = arcT[i];

            vertices.Add(vBottom);
            vertices.Add(vTop);

            float u = i / (segmentCount - 1f);
            uvs.Add(new Vector2(u, 0));
            uvs.Add(new Vector2(u, 1));
            uv2s.Add(new Vector2(u, 0));
            uv2s.Add(new Vector2(u, 1));

            normals.Add(Vector3.zero); // placeholder
            normals.Add(Vector3.zero); // placeholder
        }

        // Build quad strip with reversed winding for flipped normals
        for (int i = 0; i < segmentCount - 1; i++)
        {
            int i0 = i * 2;
            int i1 = i0 + 1;
            int i2 = i0 + 2;
            int i3 = i0 + 3;

            triangles.Add(i0); // bottom left
            triangles.Add(i2); // bottom right
            triangles.Add(i1); // top left

            triangles.Add(i2); // bottom right
            triangles.Add(i3); // top right
            triangles.Add(i1); // top left

            // Calculate flipped face normal
            Vector3 p0 = vertices[i0];
            Vector3 p1 = vertices[i1];
            Vector3 p2 = vertices[i2];
            Vector3 faceNormal = -Vector3.Cross(p1 - p0, p2 - p0).normalized;

            normals[i0] += faceNormal;
            normals[i1] += faceNormal;
            normals[i2] += faceNormal;
            normals[i3] += faceNormal;
        }

        for (int i = 0; i < normals.Count; i++)
            normals[i] = normals[i].normalized;

        Mesh mesh = new Mesh();
        mesh.name = "TentRoof";
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.SetUVs(1, uv2s);
        mesh.SetNormals(normals);
        mesh.RecalculateTangents();

        GameObject meshObj = new GameObject("TentRoofMesh", typeof(MeshFilter), typeof(MeshRenderer));
        meshObj.transform.SetParent(transform, false);
        meshObj.transform.SetParent(meshParent, true);
        meshObj.GetComponent<MeshFilter>().mesh = mesh;

        var renderer = meshObj.GetComponent<MeshRenderer>();
        renderer.material = material != null ? material : (meshMaterial != null ? meshMaterial : new Material(Shader.Find("Standard")));

        // Rope bases
        GenerateRopeBaseMesh(ropeBottom, "front rope", meshMaterial2, true, meshParent);
        GenerateRopeBaseMesh(ropeTop, "back rope", meshMaterial2, false, meshParent);
    }

    public void GenerateRopeBaseMesh(GF_Rope2 rope, string meshName, Material material, bool flipNormals, Transform meshParent)
    {
        if (rope == null) return;

        List<Vector3> arc = rope.GetArcPoints();
        if (arc == null || arc.Count < 2) return;

        for (int i = 0; i < arc.Count; i++)
            arc[i] = transform.InverseTransformPoint(arc[i]);

        Vector3 pointA = arc[0];
        Vector3 pointB = arc[arc.Count - 1];
        Vector3 center = (pointA + pointB) * 0.5f;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector2> uv2s = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();

        float radius = (pointA - center).magnitude;

        vertices.Add(center);
        uvs.Add(new Vector2(0.5f, 0f));
        uv2s.Add(new Vector2(0.5f, 0f));
        normals.Add(Vector3.zero);

        int axisU = 0;
        int axisV = 1;

        for (int i = 0; i < arc.Count; i++)
        {
            Vector3 local = arc[i] - center;
            vertices.Add(arc[i]);

            float u = local[axisU];
            float v = local[axisV];
            float uvU = (u / (2f * radius)) + 0.5f;
            float uvV = v / radius;

            uvs.Add(new Vector2(uvU, uvV));
            uv2s.Add(new Vector2(uvU, uvV));
            normals.Add(Vector3.zero); // Temp
        }

        // Build triangle fan and accumulate normals
        for (int i = 1; i < arc.Count; i++)
        {
            int i0 = 0;
            int i1 = i;
            int i2 = i + 1;

            if (flipNormals)
            {
                (i1, i2) = (i2, i1);
            }

            triangles.Add(i0);
            triangles.Add(i1);
            triangles.Add(i2);

            // Compute triangle normal
            Vector3 p0 = vertices[i0];
            Vector3 p1 = vertices[i1];
            Vector3 p2 = vertices[i2];
            Vector3 faceNormal = Vector3.Cross(p1 - p0, p2 - p0).normalized;

            normals[i0] += faceNormal;
            normals[i1] += faceNormal;
            normals[i2] += faceNormal;
        }

        // Normalize normals
        for (int i = 0; i < normals.Count; i++)
        {
            normals[i] = normals[i].normalized;
        }

        Mesh mesh = new Mesh();
        mesh.name = meshName;
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.SetUVs(1, uv2s);
        mesh.SetNormals(normals);

        mesh.RecalculateTangents();

        GameObject meshObj = new GameObject(meshName, typeof(MeshFilter), typeof(MeshRenderer));
        meshObj.transform.SetParent(transform, false);
        meshObj.transform.SetParent(meshParent, true);
        meshObj.GetComponent<MeshFilter>().mesh = mesh;

        var renderer = meshObj.GetComponent<MeshRenderer>();
        renderer.material = material != null ? material : new Material(Shader.Find("Standard"));
    }
}
