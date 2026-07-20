using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Debug")]
    [Tooltip("Centang ini saat Play Mode untuk mencari posisi kamera yang pas. Jika dicentang, script tidak akan memaksa posisi kamera.")]
    public bool debugMode = false;

    [Header("References")]
    public GridManager gridManager; 
    public Transform cameraTarget; 

    [Header("Zoom Settings")]
    public float zoomSpeed = 10f;
    public float minZoom = 2f;
    public float maxZoom = 15f;

    [Header("Pan Settings")]
    public float panSpeed = 0.5f;

    [Header("Smoothness Settings")]
    public float smoothSpeed = 5f;
    public float baseZoom = 7f; 
    public float zoomMultiplier = 0.6f;
    
    private Camera cam;
    private Vector3 initialOffset; 
    private Vector3 lastMousePosition;
    private float targetZoom;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        cam = GetComponent<Camera>();
        targetZoom = cam.orthographicSize;
    }

    // Dipanggil oleh GridManager SETELAH target berada di tengah
    public void SetInitialOffset()
    {
        if (cameraTarget != null)
        {
            initialOffset = transform.position - cameraTarget.position;
        }
    }

    private void Update()
    {
        HandleZoom();
        HandlePan();
    }

    private void LateUpdate()
    {
        if (cameraTarget == null) return;

        // JIKA DEBUG MODE NYALA, HENTIKAN KODE DI BAWAH INI
        if (debugMode) return; 

        Vector3 desiredPos = cameraTarget.position + initialOffset;
        desiredPos.y = transform.position.y; 

        transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * smoothSpeed);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * smoothSpeed);
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0.0f)
        {
            targetZoom -= scroll * zoomSpeed;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
            
            if (debugMode && cam.orthographicSize != targetZoom)
            {
                cam.orthographicSize = targetZoom; // Zoom instan saat mode debug
            }
        }
    }

    private void HandlePan()
    {
        if (cameraTarget == null) return;

        if (Input.GetMouseButtonDown(2))
        {
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(2))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            Vector3 move = new Vector3(-delta.x, 0, -delta.y) * panSpeed * Time.deltaTime;
            move = Quaternion.Euler(0, transform.eulerAngles.y, 0) * move;
            
            cameraTarget.position += move; 
            lastMousePosition = Input.mousePosition;
        }
    }

    public void CalculateAutoZoom()
    {
        if (gridManager == null) return;

        float gridWidth = (gridManager.maxX - gridManager.minX);
        float gridHeight = (gridManager.maxY - gridManager.minY);
        float maxDimension = Mathf.Max(gridWidth, gridHeight);

        float calculatedZoom = baseZoom + (maxDimension * zoomMultiplier);
        targetZoom = calculatedZoom;

        if (calculatedZoom > maxZoom) 
        {
            maxZoom = calculatedZoom + 2f; 
        }
    }
}