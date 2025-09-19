using UnityEngine;
using System.Collections.Generic;

public class GF_SailVault : MonoBehaviour
{
    public GF_Rope2 ropeLeft;
    public GF_Rope2 ropeRight;
    public GF_Rope2 ropeBottom;
    public GF_Rope2 ropeTop;
    public GF_Rope2 ropeHorizontalCore;

    public bool apexAtHighestPoint = true;
    [Range(2, 100)] public int guideResolution = 20;

    public Material lineMaterial;
    public Material meshMaterial;
    public Material meshMaterial2;
    public float lineWidth = 0.02f;

    private List<LineRenderer> activeLines = new List<LineRenderer>();

    void Update()
    {
        GenerateGuideLines();
    }

    public void GenerateGuideLines()
    {
        ClearPreviousLines();
        if (!ropeLeft || !ropeRight || !ropeBottom || !ropeTop || !ropeHorizontalCore) return;

        List<Vector3> arcL = ropeLeft.GetArcPoints();
        List<Vector3> arcR = ropeRight.GetArcPoints();
        List<Vector3> arcB = ropeBottom.GetArcPoints();
        List<Vector3> arcT = ropeTop.GetArcPoints();
        List<Vector3> arcH = ropeHorizontalCore.GetArcPoints();
        if (arcL == null || arcR == null || arcB == null || arcT == null || arcH == null) return;

        Vector3 bottomMid = SampleArcAt(arcB, 0.5f);
        Vector3 topMid = SampleArcAt(arcT, 0.5f);
        Vector3 apex = FindExtremumPoint(arcH, apexAtHighestPoint);

        Vector3[] verticalCorePoints = new Vector3[guideResolution];
        float vertexT = 0.5f;
        for (int i = 0; i < guideResolution; i++)
        {
            float t = i / (float)(guideResolution - 1);
            verticalCorePoints[i] = ParabolicPoint(bottomMid, apex, topMid, t, vertexT);
        }
        CreateLineRenderer(verticalCorePoints, "VerticalCoreGuide");

        for (int v = 0; v < guideResolution; v++)
        {
            float t = v / (float)(guideResolution - 1);
            Vector3 leftPoint = SampleArcAt(arcL, t);
            Vector3 rightPoint = SampleArcAt(arcR, t);
            Vector3 vertexPoint = verticalCorePoints[v];

            Vector3[] horizontalGuidePoints = new Vector3[guideResolution];
            for (int h = 0; h < guideResolution; h++)
            {
                float ht = h / (float)(guideResolution - 1);
                horizontalGuidePoints[h] = ParabolicPoint(leftPoint, vertexPoint, rightPoint, ht, vertexT);
            }

            CreateLineRenderer(horizontalGuidePoints, $"HorizontalGuide_{v}");
        }
    }

    private Vector3 FindExtremumPoint(List<Vector3> arc, bool findMax)
    {
        Vector3 extremum = arc[0];
        for (int i = 1; i < arc.Count; i++)
        {
            if ((findMax && arc[i].y > extremum.y) || (!findMax && arc[i].y < extremum.y))
                extremum = arc[i];
        }
        return extremum;
    }

    private Vector3 ParabolicPoint(Vector3 start, Vector3 vertex, Vector3 end, float t, float vertexT)
    {
        if (Mathf.Approximately(t, vertexT)) return vertex;
        Vector3 p = Vector3.zero;
        p.x = QuadraticInterpolate(start.x, vertex.x, end.x, t, vertexT);
        p.y = QuadraticInterpolate(start.y, vertex.y, end.y, t, vertexT);
        p.z = QuadraticInterpolate(start.z, vertex.z, end.z, t, vertexT);
        return p;
    }

    private float QuadraticInterpolate(float start, float vertex, float end, float t, float vertexT)
    {
        float c = start;
        float denom = vertexT * vertexT - vertexT;
        if (Mathf.Approximately(denom, 0f))
            return Mathf.Lerp(start, end, t);
        float a = (vertex - c - (vertexT * (end - c))) / denom;
        float b = end - a - c;
        return a * t * t + b * t + c;
    }

    private Vector3 SampleArcAt(List<Vector3> arc, float t)
    {
        float scaledIndex = t * (arc.Count - 1);
        int idx = Mathf.FloorToInt(scaledIndex);
        int nextIdx = Mathf.Min(idx + 1, arc.Count - 1);
        float lerpT = scaledIndex - idx;
        return Vector3.Lerp(arc[idx], arc[nextIdx], lerpT);
    }

    private void CreateLineRenderer(Vector3[] points, string name)
    {
        GameObject lineObj = new GameObject(name);
        lineObj.transform.parent = transform;
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.positionCount = points.Length;
        lr.SetPositions(points);
        lr.material = lineMaterial;
        lr.startWidth = lr.endWidth = lineWidth;
        lr.useWorldSpace = true;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.widthCurve = AnimationCurve.Constant(0, 1, lineWidth);
        lr.numCapVertices = 2;
        lr.numCornerVertices = 2;
        activeLines.Add(lr);
    }

    private void ClearPreviousLines()
    {
        foreach (var line in activeLines)
        {
            if (line) DestroyImmediate(line.gameObject);
        }
        activeLines.Clear();
    }

    public void SetVerticalRopeLength(float length)
    {
        if (ropeLeft != null) ropeLeft.RopeLength = length;
        if (ropeRight != null) ropeRight.RopeLength = length;
    }

    public void SetHorizontalRopeLength(float length)
    {
        if (ropeTop != null) ropeTop.RopeLength = length;
        if (ropeBottom != null) ropeBottom.RopeLength = length;
    }

    public void SetRopeLengths(float verticalLength, float horizontalLength)
    {
        SetVerticalRopeLength(verticalLength);
        SetHorizontalRopeLength(horizontalLength);
    }

    public void SetGuideRopeLength(float length)
    {
        if (ropeHorizontalCore != null) ropeHorizontalCore.RopeLength = length;
    }

    public void GenerateMesh(Transform meshParent)
    {
        List<Vector3[]> gridLines = new List<Vector3[]>();
        float vertexT = 0.5f;

        List<Vector3> arcL = ropeLeft.GetArcPoints();
        List<Vector3> arcR = ropeRight.GetArcPoints();
        List<Vector3> arcB = ropeBottom.GetArcPoints();
        List<Vector3> arcT = ropeTop.GetArcPoints();
        List<Vector3> arcH = ropeHorizontalCore.GetArcPoints();
        if (arcL == null || arcR == null || arcB == null || arcT == null || arcH == null) return;

        Vector3 bottomMid = SampleArcAt(arcB, 0.5f);
        Vector3 topMid = SampleArcAt(arcT, 0.5f);
        Vector3 apex = FindExtremumPoint(arcH, apexAtHighestPoint);

        for (int v = 0; v < guideResolution; v++)
        {
            float t = v / (float)(guideResolution - 1);
            Vector3 leftPoint = SampleArcAt(arcL, t);
            Vector3 rightPoint = SampleArcAt(arcR, t);
            Vector3 vertexPoint = ParabolicPoint(bottomMid, apex, topMid, t, vertexT);

            Vector3[] horizontal = new Vector3[guideResolution];
            for (int h = 0; h < guideResolution; h++)
            {
                float ht = h / (float)(guideResolution - 1);
                Vector3 worldPoint = ParabolicPoint(leftPoint, vertexPoint, rightPoint, ht, vertexT);
                horizontal[h] = transform.InverseTransformPoint(worldPoint);
            }
            gridLines.Add(horizontal);
        }

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        for (int v = 0; v < guideResolution - 1; v++)
        {
            for (int h = 0; h < guideResolution - 1; h++)
            {
                Vector3 bl = gridLines[v][h];
                Vector3 br = gridLines[v][h + 1];
                Vector3 tl = gridLines[v + 1][h];
                Vector3 tr = gridLines[v + 1][h + 1];

                int idx = vertices.Count;
                vertices.Add(bl);
                vertices.Add(br);
                vertices.Add(tl);
                vertices.Add(tr);

                uvs.Add(new Vector2(h / (float)(guideResolution - 1), v / (float)(guideResolution - 1)));
                uvs.Add(new Vector2((h + 1) / (float)(guideResolution - 1), v / (float)(guideResolution - 1)));
                uvs.Add(new Vector2(h / (float)(guideResolution - 1), (v + 1) / (float)(guideResolution - 1)));
                uvs.Add(new Vector2((h + 1) / (float)(guideResolution - 1), (v + 1) / (float)(guideResolution - 1)));

                triangles.Add(idx + 0);
                triangles.Add(idx + 1);
                triangles.Add(idx + 2);

                triangles.Add(idx + 2);
                triangles.Add(idx + 1);
                triangles.Add(idx + 3);
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "SailVaultMesh";
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();

        GameObject meshObj = new GameObject("SailVaultMesh", typeof(MeshFilter), typeof(MeshRenderer));
        meshObj.transform.SetParent(transform, false);
        meshObj.transform.SetParent(meshParent, true);

        meshObj.GetComponent<MeshFilter>().mesh = mesh;

        var renderer = meshObj.GetComponent<MeshRenderer>();
        renderer.material = meshMaterial != null ? meshMaterial : new Material(Shader.Find("Standard"));

        GenerateRopeBaseMesh(ropeLeft, "left rope", meshMaterial2, false, meshParent);
        GenerateRopeBaseMesh(ropeRight, "right rope", meshMaterial2, true, meshParent);
        GenerateRopeBaseMesh(ropeTop, "top rope", meshMaterial2, false, meshParent);
        GenerateRopeBaseMesh(ropeBottom, "bottom rope", meshMaterial2, true, meshParent);
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

        int axisU = (rope == ropeLeft || rope == ropeRight) ? 2 : 0; // Z or X
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
