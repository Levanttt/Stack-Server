using UnityEngine;

public class FloatingIconAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    public float bobSpeed = 5f;      
    public float bobHeight = 0.15f;  

    private Vector3 startLocalPos;
    private Camera mainCam;        

    private void Awake()
    {
        mainCam = Camera.main;
    }

    private void OnEnable()
    {
        startLocalPos = transform.localPosition;
    }

    private void Update()
    {
        float newY = startLocalPos.y + (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        transform.localPosition = new Vector3(startLocalPos.x, newY, startLocalPos.z);
        
        if (mainCam != null)
        {
            transform.rotation = mainCam.transform.rotation;
        }
    }
}