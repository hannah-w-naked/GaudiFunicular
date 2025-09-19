using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SimulationControl : MonoBehaviour
{
    public DynamicRelaxationWeb dynamicRelaxation;
    public Button addElementButton;
    public Transform gridPointsParent; // Parent holding all GF_GridPoint children
    public int intersections = 1; // Number of intermediary nodes
    public GameObject nodePrefab; // Assign your node prefab in the inspector

    private void Awake()
    {
        if (addElementButton != null)
            addElementButton.onClick.AddListener(OnAddElementButtonPressed);
    }

    private int AddNodePrefab(Vector3 position, Transform parent = null)
    {
        GameObject nodeObj = Instantiate(nodePrefab, position, Quaternion.identity, parent);
        dynamicRelaxation.nodes.Add(nodeObj.transform);

        GF_GridPoint gp = nodeObj.GetComponent<GF_GridPoint>();
        bool isFixed = gp != null && gp.isFixed;
        dynamicRelaxation.fixedNodes.Add(isFixed);

        int nodeID = dynamicRelaxation.nodes.Count - 1;
        Debug.Log($"Node prefab added to DynamicRelaxation: {nodeObj.name} (ID: {nodeID}, Fixed: {isFixed})");
        return nodeID;
    }

    private void OnAddElementButtonPressed()
    {
        var selectedPoints = GetSelectedGridPoints();
        if (selectedPoints.Count != 2)
        {
            Debug.LogWarning("Exactly two grid points must be selected.");
            return;
        }

        GF_GridPoint pointA = selectedPoints[0];
        GF_GridPoint pointB = selectedPoints[1];

        // Endpoints: always use their existing transform, check if already added
        int indexA = FindNodeByPosition(pointA.transform.position);
        if (indexA == -1)
        {
            dynamicRelaxation.nodes.Add(pointA.transform);
            dynamicRelaxation.fixedNodes.Add(pointA.isFixed);
            indexA = dynamicRelaxation.nodes.Count - 1;
            Debug.Log($"Endpoint node added: {pointA.name} (ID: {indexA}, Fixed: {pointA.isFixed})");
        }

        int indexB = FindNodeByPosition(pointB.transform.position);
        if (indexB == -1)
        {
            dynamicRelaxation.nodes.Add(pointB.transform);
            dynamicRelaxation.fixedNodes.Add(pointB.isFixed);
            indexB = dynamicRelaxation.nodes.Count - 1;
            Debug.Log($"Endpoint node added: {pointB.name} (ID: {indexB}, Fixed: {pointB.isFixed})");
        }

        List<int> nodeIndices = new List<int> { indexA };
        for (int i = 1; i <= intersections; i++)
        {
            float t = i / (float)(intersections + 1);
            Vector3 pos = Vector3.Lerp(pointA.transform.position, pointB.transform.position, t);

            int midIdx = FindNodeByPosition(pos);
            if (midIdx == -1)
            {
                GameObject nodeObj = Instantiate(nodePrefab, pos, Quaternion.identity, dynamicRelaxation.transform);
                dynamicRelaxation.nodes.Add(nodeObj.transform);
                dynamicRelaxation.fixedNodes.Add(false);
                midIdx = dynamicRelaxation.nodes.Count - 1;
                Debug.Log($"Intermediary node prefab added: {nodeObj.name} (ID: {midIdx}, Fixed: false)");
            }
            nodeIndices.Add(midIdx);
        }
        nodeIndices.Add(indexB);

        // Create elements sequentially to form a chain
        List<DynamicRelaxationWeb.Element> chainElements = new List<DynamicRelaxationWeb.Element>();
        for (int i = 0; i < nodeIndices.Count - 1; i++)
        {
            var element = new DynamicRelaxationWeb.Element
            {
                nodeA = nodeIndices[i],
                nodeB = nodeIndices[i + 1]
            };
            dynamicRelaxation.AddElement(element);
            chainElements.Add(element);
        }

        // Add the chain as a new group
        var group = new DynamicRelaxationWeb.ElementGroup
        {
            elements = chainElements,
            groupLength = chainElements.Count > 0 ? chainElements[0].ropeLength : 1f
        };
        dynamicRelaxation.groups.Add(group);

        dynamicRelaxation.SetupSimulation();
    }

    private List<GF_GridPoint> GetSelectedGridPoints()
    {
        List<GF_GridPoint> selected = new();

        GameObject[] pointObjects = GameObject.FindGameObjectsWithTag("Point");
        foreach (GameObject obj in pointObjects)
        {
            GF_GridPoint gp = obj.GetComponent<GF_GridPoint>();
            if (gp != null && gp.toggled)
                selected.Add(gp);
        }
        return selected;
    }

    private int AddNodeAlways(Transform nodeTransform)
    {
        dynamicRelaxation.nodes.Add(nodeTransform);

        GF_GridPoint gp = nodeTransform.GetComponent<GF_GridPoint>();
        bool isFixed = gp != null && gp.isFixed;
        dynamicRelaxation.fixedNodes.Add(isFixed);

        int nodeID = dynamicRelaxation.nodes.Count - 1;
        Debug.Log($"Node added to DynamicRelaxation: {nodeTransform.name} (ID: {nodeID}, Fixed: {isFixed})");

        return nodeID;
    }

    private int AddNodeFixed(Transform nodeTransform)
    {
        dynamicRelaxation.nodes.Add(nodeTransform);
        dynamicRelaxation.fixedNodes.Add(true);

        int nodeID = dynamicRelaxation.nodes.Count - 1;
        Debug.Log($"Fixed node added to DynamicRelaxation: {nodeTransform.name} (ID: {nodeID})");
        return nodeID;
    }

    private int AddNodeWithFixedStatus(Transform nodeTransform, bool isFixed)
    {
        dynamicRelaxation.nodes.Add(nodeTransform);
        dynamicRelaxation.fixedNodes.Add(isFixed);

        int nodeID = dynamicRelaxation.nodes.Count - 1;
        Debug.Log($"Node added to DynamicRelaxation: {nodeTransform.name} (ID: {nodeID}, Fixed: {isFixed})");
        return nodeID;
    }

    private int FindNodeByPosition(Vector3 position, float tolerance = 0.001f)
    {
        for (int i = 0; i < dynamicRelaxation.nodes.Count; i++)
        {
            if (Vector3.Distance(dynamicRelaxation.nodes[i].position, position) < tolerance)
                return i;
        }
        return -1;
    }
}