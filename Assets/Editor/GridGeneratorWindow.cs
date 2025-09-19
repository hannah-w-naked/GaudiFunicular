using UnityEditor;
using UnityEngine;

public class GridGeneratorWindow : EditorWindow
{
    private GameObject prefab;
    private int gridSizeX = 10;
    private int gridSizeY = 10;

    [MenuItem("NAKED_Funicular/New Grid")]
    public static void ShowWindow()
    {
        GetWindow<GridGeneratorWindow>("Grid Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Grid Settings", EditorStyles.boldLabel);

        prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);
        gridSizeX = EditorGUILayout.IntField("Grid Size X", gridSizeX);
        gridSizeY = EditorGUILayout.IntField("Grid Size Y", gridSizeY);

        if (GUILayout.Button("Generate Grid"))
        {
            GenerateGrid(gridSizeX, gridSizeY);
        }
    }

    private void GenerateGrid(int gridSizeX, int gridSizeY)
    {
        GameObject prefabToUse = prefab;

        if (prefabToUse == null)
        {
            prefabToUse = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GF_GridPoint.prefab");

            if (prefabToUse == null)
            {
                Debug.LogError("No prefab assigned, and default prefab not found at Assets/Prefabs/GF_GridPoint.prefab");
                return;
            }

            Debug.Log("Using default prefab at Assets/Prefabs/GF_GridPoint.prefab");
        }

        GF_Grid.GenerateGrid(Vector3.zero, prefabToUse, gridSizeX, gridSizeY, 0);
    }
}
