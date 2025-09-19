using UnityEngine;
using System.IO;

public class CameraSnapshot : MonoBehaviour
{
    [Header("Path Settings")]
    public string saveDirectory = "C:/MyScreenshots";
    public string filePrefix = "snapshot_";

    [Header("Camera")]
    public Camera targetCamera;  // <— assign this in the Inspector

    void Start()
    {
        if (!Directory.Exists(saveDirectory))
            Directory.CreateDirectory(saveDirectory);

        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
            TakeSnapshot();
    }

    void TakeSnapshot()
    {
        if (targetCamera == null)
        {
            Debug.LogError("❌ No camera assigned to CameraSnapshot!");
            return;
        }

        int width = Screen.width;
        int height = Screen.height;

        RenderTexture rt = new RenderTexture(width, height, 24);
        targetCamera.targetTexture = rt;
        Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);

        targetCamera.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenShot.Apply();

        targetCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        byte[] bytes = screenShot.EncodeToPNG();
        string filename = Path.Combine(saveDirectory, $"{filePrefix}{System.DateTime.Now:yyyyMMdd_HHmmss}.png");
        File.WriteAllBytes(filename, bytes);

        Debug.Log($"📸 Snapshot saved to: {filename}");
    }
}
