using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
public class GF_DomeMesh : MonoBehaviour
{
    [Range(3, 64)] public int radialSegments = 16;
    [Range(2, 32)] public int heightSegments = 8;

    private MeshFilter meshFilter;
    private GF_Rope2 rope;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        rope = GetComponent<GF_Rope2>();

        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();
    }

    public void GenerateDomeMesh()
    {
        if (rope == null || rope.GetArcPoints() == null)
        {
            Debug.LogWarning("GF_DomeMesh: Missing rope or arc points.");
            return;
        }

        Mesh domeMesh = GenerateMeshFromArc(rope.GetArcPoints(), radialSegments, heightSegments, transform);
        meshFilter.sharedMesh = domeMesh;
    }

    public static Mesh GenerateMeshFromArc(List<Vector3> arcPoints, int radialSegments, int heightSegments, Transform meshTransform)
    {
        if (arcPoints == null || arcPoints.Count < 2)
            return null;

        List<Vector3> sampledArc = ResampleArc(arcPoints, heightSegments);

        Vector3 pointA = arcPoints[0];
        Vector3 pointB = arcPoints[arcPoints.Count - 1];
        Vector3 center = (pointA + pointB) * 0.5f;

        Vector3 forward = (pointB - pointA).normalized;
        Vector3 up = Vector3.up;
        Vector3 right = Vector3.Cross(up, forward).normalized;
        up = Vector3.Cross(forward, right).normalized;

        Matrix4x4 domeTransform = Matrix4x4.TRS(center, Quaternion.LookRotation(forward, up), Vector3.one);
        Matrix4x4 inverseDome = domeTransform.inverse;

        List<Vector3> vertices = new();
        List<int> triangles = new();
        List<Vector3> normals = new();
        List<Vector2> uvs = new();

        for (int y = 0; y < heightSegments - 1; y++)
        {
            Vector3 local0 = inverseDome.MultiplyPoint3x4(sampledArc[y]);
            Vector3 local1 = inverseDome.MultiplyPoint3x4(sampledArc[y + 1]);

            float height0 = local0.y;
            float height1 = local1.y;

            float radius0 = new Vector2(local0.x, local0.z).magnitude;
            float radius1 = new Vector2(local1.x, local1.z).magnitude;

            float v0 = y / (float)(heightSegments - 1);
            float v1 = (y + 1) / (float)(heightSegments - 1);

            for (int x = 0; x < radialSegments; x++)
            {
                float t0 = x / (float)radialSegments;
                float t1 = (x + 1) / (float)radialSegments;

                float angle0 = t0 * Mathf.PI * 2f;
                float angle1 = t1 * Mathf.PI * 2f;

                Vector3 v00 = domeTransform.MultiplyPoint3x4(new Vector3(Mathf.Cos(angle0) * radius0, height0, Mathf.Sin(angle0) * radius0));
                Vector3 v01 = domeTransform.MultiplyPoint3x4(new Vector3(Mathf.Cos(angle1) * radius0, height0, Mathf.Sin(angle1) * radius0));
                Vector3 v10 = domeTransform.MultiplyPoint3x4(new Vector3(Mathf.Cos(angle0) * radius1, height1, Mathf.Sin(angle0) * radius1));
                Vector3 v11 = domeTransform.MultiplyPoint3x4(new Vector3(Mathf.Cos(angle1) * radius1, height1, Mathf.Sin(angle1) * radius1));

                if (meshTransform != null)
                {
                    v00 = meshTransform.InverseTransformPoint(v00);
                    v01 = meshTransform.InverseTransformPoint(v01);
                    v10 = meshTransform.InverseTransformPoint(v10);
                    v11 = meshTransform.InverseTransformPoint(v11);
                }

                int i = vertices.Count;
                vertices.Add(v00); vertices.Add(v10); vertices.Add(v01);
                Vector3 n1 = Vector3.Cross(v10 - v00, v01 - v00).normalized;
                normals.Add(n1); normals.Add(n1); normals.Add(n1);
                uvs.Add(new Vector2(t0, v0));
                uvs.Add(new Vector2(t0, v1));
                uvs.Add(new Vector2(t1, v0));
                triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + 2);

                i = vertices.Count;
                vertices.Add(v01); vertices.Add(v10); vertices.Add(v11);
                Vector3 n2 = Vector3.Cross(v10 - v01, v11 - v01).normalized;
                normals.Add(n2); normals.Add(n2); normals.Add(n2);
                uvs.Add(new Vector2(t1, v0));
                uvs.Add(new Vector2(t0, v1));
                uvs.Add(new Vector2(t1, v1));
                triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + 2);
            }
        }

        Mesh mesh = new Mesh { name = "GeneratedDomeMesh" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateBounds();

        return mesh;
    }

    private static List<Vector3> ResampleArc(List<Vector3> arc, int segments)
    {
        List<Vector3> sampled = new();
        if (arc.Count < 2) return sampled;

        float minH = float.MaxValue;
        float maxH = float.MinValue;
        foreach (var p in arc)
        {
            minH = Mathf.Min(minH, p.y);
            maxH = Mathf.Max(maxH, p.y);
        }

        for (int i = 0; i < segments; i++)
        {
            float targetHeight = Mathf.Lerp(minH, maxH, i / (float)(segments - 1));

            for (int j = 0; j < arc.Count - 1; j++)
            {
                if (targetHeight >= arc[j].y && targetHeight <= arc[j + 1].y)
                {
                    float t = Mathf.InverseLerp(arc[j].y, arc[j + 1].y, targetHeight);
                    sampled.Add(Vector3.Lerp(arc[j], arc[j + 1], t));
                    break;
                }
            }
        }

        return sampled;
    }
}
