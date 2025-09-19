using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GF_Grid
{
    public static GameObject GenerateGrid(Vector3 origin, GameObject prefab, int sizeX, int sizeY, int height)
    {
        if (prefab == null)
        {
            Debug.LogError("GridGenerator: prefab is null!");
            return null;
        }

        GameObject parent = new GameObject("Funicular Simulation Grid");

        for (int y = 0; y < sizeY; y++)
        {
            for (int x = 0; x < sizeX; x++)
            {
                Vector3 position = origin + new Vector3(x * 10, height, y * 10);
                GameObject obj = Object.Instantiate(prefab, position, Quaternion.identity);
                obj.transform.SetParent(parent.transform);

                GF_GridPoint gridPoint = obj.GetComponent<GF_GridPoint>();
                if (gridPoint != null)
                {
                    gridPoint.Initialize(x, y);
                }
            }
        }

        Debug.Log($"Grid generated at {origin}: {sizeX} x {sizeY}");
        return parent;
    }
}

