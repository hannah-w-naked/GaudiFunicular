using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_DR2 : MonoBehaviour
{
    public Button pathButton; // Assign in inspector
    public DynamicRelaxationWeb dynamicRelaxation; // Assign in inspector
    public Transform gridParent; // Parent of all GF_GridPoint objects

    void Start()
    {
        if (pathButton != null)
            pathButton.onClick.AddListener(OnPathButtonPressed);
    }

    void OnPathButtonPressed()
{
    // Reset simulation
    dynamicRelaxation.nodes.Clear();
    dynamicRelaxation.elements.Clear();
    dynamicRelaxation.fixedNodes.Clear();

    // Collect toggled nodes
    GF_GridPoint[] allGridPoints = gridParent.GetComponentsInChildren<GF_GridPoint>();
    HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
    Dictionary<Vector2Int, Transform> posToTransform = new Dictionary<Vector2Int, Transform>();

    foreach (var gp in allGridPoints)
    {
        if (gp.toggled)
        {
            Vector2Int gridPos = new Vector2Int(Mathf.RoundToInt(gp.transform.position.x),
                                               Mathf.RoundToInt(gp.transform.position.y));
            occupied.Add(gridPos);
            posToTransform[gridPos] = gp.transform;
        }
        gp.Deselect(); // Reset toggled state
    }

    if (occupied.Count == 0) return;

    // Decompose occupied area into rectangles
    List<RectInt> rectangles = DecomposeIntoRectangles(occupied);

    // 🔹 DEBUG: log all points in each rectangle
    for (int i = 0; i < rectangles.Count; i++)
    {
        RectInt rect = rectangles[i];
        List<Vector2Int> rectPoints = new List<Vector2Int>();

        for (int y = rect.yMin; y < rect.yMax; y++)
        {
            for (int x = rect.xMin; x < rect.xMax; x++)
            {
                Vector2Int p = new Vector2Int(x, y);
                if (occupied.Contains(p))
                    rectPoints.Add(p);
            }
        }

        string pointList = string.Join(", ", rectPoints);
        Debug.Log($"Rectangle {i}: Origin=({rect.xMin},{rect.yMin}) Size=({rect.width}x{rect.height}) " +
                  $"Contains Points: {pointList}");
    }

    HashSet<Transform> perimeterNodes = new HashSet<Transform>();

    // Build elements around each rectangle perimeter
    foreach (RectInt rect in rectangles)
    {
        // Walk around rectangle perimeter
        List<Vector2Int> perimeter = new List<Vector2Int>();

        for (int x = rect.xMin; x < rect.xMax; x++) perimeter.Add(new Vector2Int(x, rect.yMin));
        for (int y = rect.yMin; y < rect.yMax; y++) perimeter.Add(new Vector2Int(rect.xMax, y));
        for (int x = rect.xMax; x > rect.xMin; x--) perimeter.Add(new Vector2Int(x, rect.yMax));
        for (int y = rect.yMax; y > rect.yMin; y--) perimeter.Add(new Vector2Int(rect.xMin, y));

        Transform prev = null;
        foreach (var cell in perimeter)
        {
            if (!posToTransform.ContainsKey(cell)) continue; // skip missing nodes
            Transform node = posToTransform[cell];

            // Ensure node in sim
            if (!dynamicRelaxation.nodes.Contains(node))
            {
                dynamicRelaxation.nodes.Add(node);
            }

            perimeterNodes.Add(node);

            if (prev != null) AddElement(prev, node);
            prev = node;
        }

        // Close the loop
        if (perimeter.Count > 1)
        {
            Transform first = posToTransform[perimeter[0]];
            if (first != null && prev != null && first != prev)
                AddElement(prev, first);
        }
    }

    // Mark fixed nodes (all perimeter nodes fixed, others free)
    foreach (var node in dynamicRelaxation.nodes)
    {
        bool isFixed = perimeterNodes.Contains(node);
        dynamicRelaxation.fixedNodes.Add(isFixed);
    }

    // Initialize simulation
    dynamicRelaxation.SetupSimulation();
}


    void AddElement(Transform a, Transform b)
    {
        int idxA = dynamicRelaxation.nodes.IndexOf(a);
        int idxB = dynamicRelaxation.nodes.IndexOf(b);
        if (idxA != -1 && idxB != -1)
        {
            var element = new DynamicRelaxationWeb.Element
            {
                nodeA = idxA,
                nodeB = idxB,
                ropeLength = Vector3.Distance(a.position, b.position)
            };
            dynamicRelaxation.elements.Add(element);
        }
    }

    // Greedy decomposition of occupied cells into axis-aligned rectangles
    List<RectInt> DecomposeIntoRectangles(HashSet<Vector2Int> occupied)
    {
        List<RectInt> rects = new List<RectInt>();
        HashSet<Vector2Int> used = new HashSet<Vector2Int>();

        foreach (var start in occupied)
        {
            if (used.Contains(start)) continue;

            int width = 1;
            int height = 1;

            // expand width
            while (occupied.Contains(new Vector2Int(start.x + width, start.y)))
                width++;

            // expand height while full rows exist
            bool canExpand = true;
            while (canExpand)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!occupied.Contains(new Vector2Int(start.x + x, start.y + height)))
                    {
                        canExpand = false;
                        break;
                    }
                }
                if (canExpand) height++;
            }

            // mark as used
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    used.Add(new Vector2Int(start.x + x, start.y + y));

            rects.Add(new RectInt(start.x, start.y, width, height));
        }
        return rects;
    }
}
