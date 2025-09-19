using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class GF_Volume : MonoBehaviour
{
    [Header("Base Grid Points (Clockwise from bottom-left)")]
    [SerializeField] private GF_GridPoint base00;
    [SerializeField] private GF_GridPoint base10;
    [SerializeField] private GF_GridPoint base11;
    [SerializeField] private GF_GridPoint base01;

    [Header("Generated Grid Settings")]
    private float spacing = 10f;
    [SerializeField] private int height = -10;
    [SerializeField] private RuntimeGridGenerator gridGenerator;

    [Header("Vertical Lines (Base to Grid)")]
    [SerializeField] private LineRenderer line00;
    [SerializeField] private LineRenderer line10;
    [SerializeField] private LineRenderer line11;
    [SerializeField] private LineRenderer line01;

    [Header("Horizontal Rectangle Lines (Between Grid Points)")]
    [SerializeField] private LineRenderer rectLine0;
    [SerializeField] private LineRenderer rectLine1;
    [SerializeField] private LineRenderer rectLine2;
    [SerializeField] private LineRenderer rectLine3;

    [Header("Generated Grid Corner Points")]
    public GF_GridPoint gen00; // Bottom-left
    public GF_GridPoint gen10; // Bottom-right
    public GF_GridPoint gen11; // Top-right
    public GF_GridPoint gen01; // Top-left

    [Header("Roofs")]
    [SerializeField] private GF_SailVault sailVault;
    [SerializeField] private GF_TentRoof tentRoof;

    [Header("Mesh Settings")]
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private Material meshMaterial;

    private GameObject generatedGridParent;
    private BoxCollider boxCollider;

    private int minX, minY, maxX, maxY;
    private int sizeX, sizeY;

    private List<GameObject> planeObjects = new List<GameObject>();

    void Start()
    {
        if (gridGenerator == null)
        {
            Debug.LogError("GF_Volume: RuntimeGridGenerator reference not assigned.");
            return;
        }

        boxCollider = GetComponent<BoxCollider>();
        SetLineWidths();
        InitializeGrid();
        UpdateLines();
        UpdateColliderBounds();
    }

    void Update()
    {
        if (generatedGridParent != null)
        {
            Vector3 pos = generatedGridParent.transform.position;
            generatedGridParent.transform.position = new Vector3(pos.x, height + 10, pos.z);
            UpdateLines();
            UpdateColliderBounds();
        }
    }

    public void Initialize(GF_GridPoint point00, GF_GridPoint point10, GF_GridPoint point11, GF_GridPoint point01, RuntimeGridGenerator rgg)
    {
        base00 = point00;
        base10 = point10;
        base11 = point11;
        base01 = point01;
        gridGenerator = rgg;
    }

    void SetLineWidths()
    {
        var allLines = new LineRenderer[] { line00, line10, line11, line01, rectLine0, rectLine1, rectLine2, rectLine3 };
        foreach (var line in allLines)
        {
            if (line != null)
            {
                line.startWidth = 0.1f;
                line.endWidth = 0.1f;
            }
        }
    }

    void InitializeGrid()
    {
        Vector2Int[] coords = new Vector2Int[]
        {
            base00.GetGridPosition(),
            base10.GetGridPosition(),
            base11.GetGridPosition(),
            base01.GetGridPosition()
        };

        minX = int.MaxValue;
        minY = int.MaxValue;
        maxX = int.MinValue;
        maxY = int.MinValue;

        foreach (var c in coords)
        {
            minX = Mathf.Min(minX, c.x);
            minY = Mathf.Min(minY, c.y);
            maxX = Mathf.Max(maxX, c.x);
            maxY = Mathf.Max(maxY, c.y);
        }

        sizeX = maxX - minX + 1;
        sizeY = maxY - minY + 1;

        Vector3 origin = new Vector3(minX * spacing, height, minY * spacing);

        generatedGridParent = gridGenerator.GenerateGridAtRuntime(origin, sizeX, sizeY, 0);
        if (generatedGridParent == null)
        {
            Debug.LogError("GF_Volume: Failed to generate grid.");
            return;
        }

        generatedGridParent.transform.SetParent(this.transform);

        List<GF_GridPoint> allPoints = new();
        foreach (Transform child in generatedGridParent.transform)
        {
            var gp = child.GetComponent<GF_GridPoint>();
            if (gp != null)
                allPoints.Add(gp);
        }

        gen00 = FindPointWithLogical(allPoints, true, true);
        gen10 = FindPointWithLogical(allPoints, false, true);
        gen11 = FindPointWithLogical(allPoints, false, false);
        gen01 = FindPointWithLogical(allPoints, true, false);

        InitializeSailVault(sailVault);
        InitializeTentRoof(tentRoof);
    }

    GF_GridPoint FindPointWithLogical(List<GF_GridPoint> points, bool findMinX, bool findMinY)
    {
        GF_GridPoint result = null;
        int bestX = findMinX ? int.MaxValue : int.MinValue;
        int bestY = findMinY ? int.MaxValue : int.MinValue;

        foreach (var gp in points)
        {
            Vector2Int pos = gp.GetGridPosition();
            bool isBetter =
                (findMinX ? pos.x <= bestX : pos.x >= bestX) &&
                (findMinY ? pos.y <= bestY : pos.y >= bestY);

            if (isBetter)
            {
                result = gp;
                bestX = pos.x;
                bestY = pos.y;
            }
        }

        return result;
    }

    void UpdateLines()
    {
        DrawLine(line00, base00, gen00);
        DrawLine(line10, base10, gen10);
        DrawLine(line11, base11, gen11);
        DrawLine(line01, base01, gen01);

        DrawLine(rectLine0, gen00, gen10);
        DrawLine(rectLine1, gen10, gen11);
        DrawLine(rectLine2, gen11, gen01);
        DrawLine(rectLine3, gen01, gen00);
    }

    void DrawLine(LineRenderer lr, GF_GridPoint a, GF_GridPoint b)
    {
        if (lr != null && a != null && b != null)
        {
            lr.positionCount = 2;
            lr.SetPosition(0, a.transform.position);
            lr.SetPosition(1, b.transform.position);
        }
    }

    void UpdateColliderBounds()
    {
        if (boxCollider == null) return;

        Vector3 p00 = base00.transform.position;
        Vector3 p10 = base10.transform.position;
        Vector3 p11 = base11.transform.position;
        Vector3 p01 = base01.transform.position;

        Vector3 baseCenter = (p00 + p10 + p11 + p01) / 4f;
        Vector3 topCenter = baseCenter + new Vector3(0, height, 0);

        float width = Vector3.Distance(p00, p10);
        float depth = Vector3.Distance(p00, p01);
        float heightAbs = Mathf.Abs(height);

        Vector3 worldCenter = (baseCenter + topCenter) * 0.5f;
        boxCollider.center = transform.InverseTransformPoint(worldCenter);
        boxCollider.size = new Vector3(width, heightAbs, depth);
    }

    void InitializeSailVault(GF_SailVault sailVault)
    {
        if (gen00 == null || gen10 == null || gen11 == null || gen01 == null)
        {
            Debug.LogWarning("GF_Volume: Cannot initialize sail vault - generated grid points missing.");
            return;
        }

        if (sailVault.ropeBottom != null)
            sailVault.ropeBottom.SetEndpoints(gen00.gameObject, gen10.gameObject);

        if (sailVault.ropeTop != null)
            sailVault.ropeTop.SetEndpoints(gen01.gameObject, gen11.gameObject);

        if (sailVault.ropeLeft != null)
            sailVault.ropeLeft.SetEndpoints(gen00.gameObject, gen01.gameObject);

        if (sailVault.ropeRight != null)
            sailVault.ropeRight.SetEndpoints(gen10.gameObject, gen11.gameObject);

        float horizontalLength = Vector3.Distance(gen00.transform.position, gen10.transform.position) + 5f;
        float verticalLength = Vector3.Distance(gen00.transform.position, gen01.transform.position) + 5f;

        sailVault.SetHorizontalRopeLength(horizontalLength);
        sailVault.SetVerticalRopeLength(verticalLength);
        sailVault.SetGuideRopeLength(horizontalLength + 5f);
    }

    void InitializeTentRoof(GF_TentRoof tentRoof)
    {
        if (gen00 == null || gen10 == null || gen11 == null || gen01 == null)
        {
            Debug.LogWarning("GF_Volume: Cannot initialize tent roof - generated grid points missing.");
            return;
        }

        if (tentRoof.ropeBottom != null)
            tentRoof.ropeBottom.SetEndpoints(gen00.gameObject, gen10.gameObject);

        if (tentRoof.ropeTop != null)
            tentRoof.ropeTop.SetEndpoints(gen01.gameObject, gen11.gameObject);

        float horizontalLength = Vector3.Distance(gen00.transform.position, gen10.transform.position) + 5f;
        tentRoof.ropeBottom.RopeLength = horizontalLength * 1.5f;
    }

    [ContextMenu("Generate Mesh")]
    public void GenerateMesh()
    {
        ClearOldPlanes();

        Transform meshParent = GameObject.FindGameObjectWithTag("Mesh Parent")?.transform;
        if (meshParent == null)
        {
            Debug.LogError("GenerateMesh: No GameObject found with tag 'Mesh Parent'.");
            return;
        }

        CreatePlane("Bottom", meshParent, base00.transform.position, base10.transform.position, base11.transform.position, base01.transform.position, "XZ");
        CreatePlane("Top", meshParent, gen01.transform.position, gen11.transform.position, gen10.transform.position, gen00.transform.position, "XZ");
        CreatePlane("Left", meshParent, base00.transform.position, base01.transform.position, gen01.transform.position, gen00.transform.position, "ZY");
        CreatePlane("Front", meshParent, base10.transform.position, base00.transform.position, gen00.transform.position, gen10.transform.position, "XY");
        CreatePlane("Right", meshParent, base11.transform.position, base10.transform.position, gen10.transform.position, gen11.transform.position, "ZY");
        CreatePlane("Back", meshParent, base01.transform.position, base11.transform.position, gen11.transform.position, gen01.transform.position, "XY");

        if(IsSailRoofActive){
            sailVault.GenerateMesh(meshParent);
        }
        if(IsTentRoofActive){
            tentRoof.GenerateMesh(meshParent);
        }
    }

    void ClearOldPlanes()
    {
        foreach (var plane in planeObjects)
        {
            if (plane != null)
                DestroyImmediate(plane);
        }
        planeObjects.Clear();
    }

    void CreatePlane(string name, Transform parent, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, string uvProjection)
    {
        GameObject plane = new GameObject(name);
        plane.transform.SetParent(parent, false);

        Vector3 faceCenter = (v0 + v1 + v2 + v3) / 4f;
        plane.transform.position = faceCenter;
        planeObjects.Add(plane);

        MeshFilter mf = plane.AddComponent<MeshFilter>();
        MeshRenderer mr = plane.AddComponent<MeshRenderer>();
        mr.material = meshMaterial;

        // Vertices relative to face center
        Vector3 rv0 = v0 - faceCenter;
        Vector3 rv1 = v1 - faceCenter;
        Vector3 rv2 = v2 - faceCenter;
        Vector3 rv3 = v3 - faceCenter;

        mf.mesh = CreateQuad(rv0, rv1, rv2, rv3, uvProjection, tileSize);
    }


    Mesh CreateQuad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, string projection, float tileScale)
    {
        Mesh mesh = new Mesh();

        List<Vector3> verts = new List<Vector3>() { v0, v1, v2, v3 };
        List<Vector2> uvs = new List<Vector2>()
        {
            GetUV(v0, projection, tileScale),
            GetUV(v1, projection, tileScale),
            GetUV(v2, projection, tileScale),
            GetUV(v3, projection, tileScale)
        };

        List<int> tris = new List<int>()
        {
            0, 2, 1,
            0, 3, 2
        };

        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    Vector2 GetUV(Vector3 v, string projection, float scale)
    {
        switch (projection)
        {
            case "XZ":
                return new Vector2(v.x, v.z) * (1f / scale);
            case "XY":
                return new Vector2(v.x, v.y) * (1f / scale);
            case "ZY":
                return new Vector2(v.z, v.y) * (1f / scale);
            default:
                return Vector2.zero;
        }
    }

    private void OnMouseDown()
    {
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
            uiManager.ShowVolumeSettings(this);
    }

    public int VolumeHeight
    {
        get => height;
        set => height = value;
    }

    public bool IsSailRoofActive => sailVault != null && sailVault.gameObject.activeSelf;
    public bool IsTentRoofActive => tentRoof != null && tentRoof.gameObject.activeSelf;

    public void EnableSailRoof()
    {
        if (generatedGridParent != null)
            generatedGridParent.SetActive(false);
        if (tentRoof != null)
            tentRoof.gameObject.SetActive(false);

        if (sailVault != null)
            sailVault.gameObject.SetActive(true);
    }

    public void DisableSailRoof()
    {
        if (generatedGridParent != null)
            generatedGridParent.SetActive(true);

        if (sailVault != null)
            sailVault.gameObject.SetActive(false);
        if (tentRoof != null)
            tentRoof.gameObject.SetActive(false);
    }

    public void EnableTentRoof()
    {
        if (generatedGridParent != null)
            generatedGridParent.SetActive(false);
        if (sailVault != null)
            sailVault.gameObject.SetActive(false);

        if (tentRoof != null)
            tentRoof.gameObject.SetActive(true);
    }

    public void DisableTentRoof()
    {
        if (generatedGridParent != null)
            generatedGridParent.SetActive(true);

        if (sailVault != null)
            sailVault.gameObject.SetActive(false);
        if (tentRoof != null)
            tentRoof.gameObject.SetActive(false);
    }
}
