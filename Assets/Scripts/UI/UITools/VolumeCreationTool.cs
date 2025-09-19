using System.Collections.Generic;
using UnityEngine;

public class VolumeCreationTool : UITool
{
    [SerializeField] private GameObject gfVolumePrefab;
    [SerializeField] private RuntimeGridGenerator gridGenerator;

    public override bool ValidatePoints(List<GF_GridPoint> gridPoints)
    {
        if (gridPoints.Count != 4)
            return false;

        List<Vector2Int> coords = new List<Vector2Int>();
        foreach (var p in gridPoints)
            coords.Add(p.GetGridPosition());

        // Log all logical grid positions
        Debug.Log("Selected Grid Points:");
        for (int i = 0; i < coords.Count; i++)
        {
            Debug.Log($"Point {i}: {coords[i]}");
        }

        coords.Sort((a, b) =>
        {
            Vector2 center = Vector2.zero;
            foreach (var c in coords)
                center += c;
            center /= coords.Count;

            float angleA = Mathf.Atan2(a.y - center.y, a.x - center.x);
            float angleB = Mathf.Atan2(b.y - center.y, b.x - center.x);
            return angleA.CompareTo(angleB);
        });

        Vector2Int a = coords[0];
        Vector2Int b = coords[1];
        Vector2Int c = coords[2];
        Vector2Int d = coords[3];

        // Log sorted coordinates
        Debug.Log($"Sorted Points: A={a}, B={b}, C={c}, D={d}");

        bool isRect = (a.x == d.x && b.x == c.x && a.y == b.y && d.y == c.y) ||
                    (a.y == d.y && b.y == c.y && a.x == b.x && d.x == c.x);

        Debug.Log($"Is rectangle: {isRect}");

        return isRect;
    }


    public override void OnToolActivated(List<GF_GridPoint> gridPoints)
    {
        GF_GridPoint base00 = null, base10 = null, base11 = null, base01 = null;

        foreach (var p in gridPoints)
        {
            var pos = p.GetGridPosition();
            if (base00 == null || (pos.x <= base00.GetGridPosition().x && pos.y <= base00.GetGridPosition().y))
                base00 = p;
            if (base10 == null || (pos.x >= base10.GetGridPosition().x && pos.y <= base10.GetGridPosition().y))
                base10 = p;
            if (base11 == null || (pos.x >= base11.GetGridPosition().x && pos.y >= base11.GetGridPosition().y))
                base11 = p;
            if (base01 == null || (pos.x <= base01.GetGridPosition().x && pos.y >= base01.GetGridPosition().y))
                base01 = p;
        }

        if (gfVolumePrefab == null)
        {
            Debug.LogError("GF_Volume prefab not assigned.");
            return;
        }

        // Compute center position of the 4 selected points
        Vector3 center = Vector3.zero;
        foreach (var p in gridPoints)
        {
            center += p.transform.position;
        }
        center /= gridPoints.Count;

        // Instantiate volume at center
        GameObject instance = Instantiate(gfVolumePrefab, center, Quaternion.identity);

        GF_Volume volume = instance.GetComponent<GF_Volume>();

        if (volume == null)
        {
            Debug.LogError("GF_Volume component missing on prefab.");
            return;
        }

        volume.Initialize(base00, base10, base11, base01, gridGenerator);
        Debug.Log("GF_Volume instantiated and initialized.");

        foreach (var p in gridPoints)
        {
            p.Deselect();
        }
    }
}
