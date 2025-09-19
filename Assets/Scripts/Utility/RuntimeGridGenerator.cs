using UnityEngine;

public class RuntimeGridGenerator : MonoBehaviour
{
    [SerializeField] private GameObject gridPointPrefab;

    public GameObject GenerateGridAtRuntime(Vector3 zero, int gridSizeX, int gridSizeY, int height)
    {
        if (gridPointPrefab == null)
        {
            Debug.LogError("Grid Point Prefab is not assigned.");
            return null;
        }

        return GF_Grid.GenerateGrid(zero, gridPointPrefab, gridSizeX, gridSizeY, height);
    }

    public GameObject GetGridPointPrefab() => gridPointPrefab;
}
