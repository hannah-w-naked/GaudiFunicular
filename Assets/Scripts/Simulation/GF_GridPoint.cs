using UnityEngine;

public class GF_GridPoint : MonoBehaviour
{
    [SerializeField] private Material highlightMaterial;

    private Renderer rend;
    private Material originalMaterial;
    public bool toggled = false;
    public bool canToggle = true; // Allow toggling by default
    public bool isFixed = true;

    public Vector2Int gridPosition;

    public void Initialize(int x, int y)
    {
        gridPosition = new Vector2Int(x, y);
    }

    public void DisablePoint()
    {
        rend.material = highlightMaterial;
        canToggle = false; // Disable toggling
    }

    public void EnablePoint()
    {
        rend.material = originalMaterial;
        canToggle = true; // Disable toggling
    }

    public Vector2Int GetGridPosition()
    {
        return gridPosition;
    }

    private void Start()
    {
        rend = GetComponent<Renderer>();
        originalMaterial = rend.sharedMaterial;
    }

    private void OnMouseDown()
    {
        if (!canToggle) return;
        toggled = !toggled;

        if (toggled && highlightMaterial != null)
        {
            rend.material = highlightMaterial;
        }
        else
        {
            rend.material = originalMaterial;
        }
    }

    public void Deselect(){
        if (!canToggle) return;
        toggled = false;
        rend.material = originalMaterial;
    }
}
