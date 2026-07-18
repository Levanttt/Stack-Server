using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Zoom Settings")]
    public float zoomSpeed = 10f;
    public float minZoom = 2f;
    public float maxZoom = 15f;

    [Header("Pan Settings")]
    public float panSpeed = 0.5f;

    private Camera cam;
    private Vector3 lastMousePosition;

    private void Start()
    {
        cam = GetComponent<Camera>();
    }

    private void Update()
    {
        HandleZoom();
        HandlePan();
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0.0f)
        {
            cam.orthographicSize -= scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }

    private void HandlePan()
    {
        if (Input.GetMouseButtonDown(2))
        {
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(2))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            Vector3 move = new Vector3(-delta.x, 0, -delta.y) * panSpeed * Time.deltaTime;
            
            move = Quaternion.Euler(0, transform.eulerAngles.y, 0) * move;

            transform.Translate(move, Space.World);
            
            lastMousePosition = Input.mousePosition;
        }
    }
}