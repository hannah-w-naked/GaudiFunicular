using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    public float rotationSpeed = 3f;
    public float panSpeed = 0.5f;
    public float zoomSpeed = 10f;

    private Vector3 target;
    private float distance = 10f;
    private Vector2 rotation = new Vector2(30f, 45f);

    private void Start()
    {
        target = transform.position + transform.forward * distance;
        distance = Vector3.Distance(transform.position, target);
    }

    private void LateUpdate()
    {
        if (Input.GetMouseButton(1))
        {
            rotation.x += Input.GetAxis("Mouse X") * rotationSpeed;
            rotation.y -= Input.GetAxis("Mouse Y") * rotationSpeed;
            rotation.y = Mathf.Clamp(rotation.y, -89f, 89f); 
        }

        if (Input.GetMouseButton(2))
        {
            Vector3 right = transform.right;
            Vector3 up = transform.up;

            Vector3 pan = -right * Input.GetAxis("Mouse X") * panSpeed
                        - up * Input.GetAxis("Mouse Y") * panSpeed;
            target += pan;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance -= scroll * zoomSpeed;
        distance = Mathf.Clamp(distance, 1f, 100f);

        Quaternion rot = Quaternion.Euler(rotation.y, rotation.x, 0);
        Vector3 dir = rot * Vector3.forward;
        transform.position = target - dir * distance;
        transform.rotation = rot;
    }
}
