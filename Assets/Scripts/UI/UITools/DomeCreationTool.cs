using System.Collections.Generic;
using UnityEngine;

public class DomeCreationTool : UITool
{
    [Header("Prefab to instantiate")]
    [SerializeField] private GameObject ropePrefab;

    public override bool ValidatePoints(List<GF_GridPoint> gridPoints)
    {
        foreach (var p in gridPoints)
            Debug.Log(p.name);
            
        if (gridPoints.Count != 2)
            return false;

        Vector3 a = gridPoints[0].transform.position;
        Vector3 b = gridPoints[1].transform.position;

        bool sameRow = Mathf.Approximately(a.z, b.z);
        bool sameCol = Mathf.Approximately(a.x, b.x);

        return sameRow || sameCol;
    }

    public override void OnToolActivated(List<GF_GridPoint> gridPoints)
    {
        Debug.Log("DomeCreationTool activated with points:");
        foreach (var p in gridPoints)
            Debug.Log(p.name);

        if (gridPoints.Count != 2 || ropePrefab == null)
        {
            Debug.LogError("DomeCreationTool: Invalid state. Missing points or prefab.");
            return;
        }

        Vector3 posA = gridPoints[0].transform.position;
        Vector3 posB = gridPoints[1].transform.position;
        Vector3 midpoint = (posA + posB) * 0.5f;

        GameObject ropeInstance = Instantiate(ropePrefab, midpoint, Quaternion.identity);
        ropeInstance.SetActive(false);

        GF_Rope2 ropeScript = ropeInstance.GetComponent<GF_Rope2>();

        if (ropeScript != null)
        {
            ropeScript.SetEndpoints(gridPoints[0].gameObject, gridPoints[1].gameObject);
        }
        else
        {
            Debug.LogError("DomeCreationTool: GF_Rope2 script not found on prefab.");
            return;
        }

        ropeInstance.SetActive(true);
        ropeScript.InitializeRope(posA, posB);

        foreach (var p in gridPoints)
        {
            p.Deselect();
        }
    }
}
