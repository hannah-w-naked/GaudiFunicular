using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_DR_Web : MonoBehaviour
{
    public Button pathButton; // Assign in inspector
    public DynamicRelaxationWeb dynamicRelaxation; // Assign in inspector
    public Transform gridParent; // Parent of all GF_GridPoint objects
    public GameObject pointPrefab; // Prefab for intermediary points

    void Start()
    {
        if (pathButton != null)
            pathButton.onClick.AddListener(OnPathButtonPressed);
    }

    void OnPathButtonPressed()
    {
        // Reset the simulation data
        dynamicRelaxation.nodes.Clear();
        dynamicRelaxation.elements.Clear();
        dynamicRelaxation.fixedNodes.Clear();

        // Get all boundary points
        GF_GridPoint[] allGridPoints = gridParent.GetComponentsInChildren<GF_GridPoint>();
        List<GF_GridPoint> boundaryPoints = new List<GF_GridPoint>();

        foreach (var gp in allGridPoints)
        {
            if (gp.toggled)
                boundaryPoints.Add(gp);
        }

        // Need at least 3 boundary points
        if (boundaryPoints.Count < 3) return;

        // Order them around the perimeter
        List<GF_GridPoint> orderedBoundary = OrderBoundaryPoints(boundaryPoints);

        // Add boundary nodes
        foreach (var gp in orderedBoundary)
            dynamicRelaxation.nodes.Add(gp.transform);

        // Step 1: create first layer of intermediary nodes
        List<Transform> firstLayer = CreateIntermediaryLayer(orderedBoundary);

        // Step 2: create second layer of intermediary nodes
        List<Transform> secondLayer = CreateIntermediaryLayer(firstLayer);

        // Step 3: third layer → connect secondLayer nodes randomly
        ConnectLayerRandomly(secondLayer);

        // Step 4: mark fixed/free
        foreach (var node in dynamicRelaxation.nodes)
        {
            bool isFixed = orderedBoundary.Exists(b => b.transform == node);
            dynamicRelaxation.fixedNodes.Add(isFixed);
        }

        // Initialize the simulation
        dynamicRelaxation.SetupSimulation();
    }

    List<Transform> CreateIntermediaryLayer(IList inputNodes)
    {
        List<Transform> newLayer = new List<Transform>();

        for (int i = 0; i < inputNodes.Count; i++)
        {
            Transform a = (inputNodes[i] is GF_GridPoint gpA) ? gpA.transform : (Transform)inputNodes[i];
            Transform b = (inputNodes[(i + 1) % inputNodes.Count] is GF_GridPoint gpB) ? gpB.transform : (Transform)inputNodes[(i + 1) % inputNodes.Count];

            Vector3 mid = (a.position + b.position) / 2f;
            GameObject midGO = Instantiate(pointPrefab, mid, Quaternion.identity);
            dynamicRelaxation.nodes.Add(midGO.transform);
            newLayer.Add(midGO.transform);

            // Connect parent nodes to intermediary
            AddElement(a, midGO.transform);
            AddElement(b, midGO.transform);
        }

        return newLayer;
    }

    void ConnectLayerRandomly(List<Transform> layer)
    {
        if (layer.Count < 2) return;

        // Shuffle the list
        List<Transform> shuffled = new List<Transform>(layer);
        for (int i = 0; i < shuffled.Count; i++)
        {
            Transform temp = shuffled[i];
            int randIndex = Random.Range(i, shuffled.Count);
            shuffled[i] = shuffled[randIndex];
            shuffled[randIndex] = temp;
        }

        // Pair them up
        for (int i = 0; i < shuffled.Count - 1; i += 2)
        {
            AddElement(shuffled[i], shuffled[i + 1], true); // random length
        }

        // If odd number, connect last one to a random earlier node
        if (shuffled.Count % 2 == 1)
        {
            AddElement(shuffled[shuffled.Count - 1], shuffled[Random.Range(0, shuffled.Count - 1)], true); // random length
        }
    }

    void AddElement(Transform a, Transform b, bool randomLength = false)
    {
        int idxA = dynamicRelaxation.nodes.IndexOf(a);
        int idxB = dynamicRelaxation.nodes.IndexOf(b);
        if (idxA != -1 && idxB != -1)
        {
            var element = new DynamicRelaxationWeb.Element
            {
                nodeA = idxA,
                nodeB = idxB,
                ropeLength = randomLength ? Random.Range(50f, 100f) 
                                        : Vector3.Distance(a.position, b.position)
            };
            dynamicRelaxation.elements.Add(element);
        }
    }


    // Orders boundary points along perimeter using nearest neighbor walk
    List<GF_GridPoint> OrderBoundaryPoints(List<GF_GridPoint> points)
    {
        List<GF_GridPoint> ordered = new List<GF_GridPoint>();
        HashSet<GF_GridPoint> remaining = new HashSet<GF_GridPoint>(points);

        GF_GridPoint current = points[0];
        ordered.Add(current);
        remaining.Remove(current);

        while (remaining.Count > 0)
        {
            GF_GridPoint next = null;
            float minDist = float.MaxValue;
            foreach (var candidate in remaining)
            {
                float dist = Vector3.Distance(current.transform.position, candidate.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    next = candidate;
                }
            }
            if (next != null)
            {
                ordered.Add(next);
                remaining.Remove(next);
                current = next;
            }
            else
            {
                break;
            }
        }
        return ordered;
    }
}
