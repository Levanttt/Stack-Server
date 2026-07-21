using UnityEngine;

public class FloatingIconAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    public float bobSpeed = 5f;      // Kecepatan naik-turun
    public float bobHeight = 0.15f;  // Jarak naik-turun

    private Vector3 startLocalPos;

    private void OnEnable()
    {
        // Simpan posisi awalnya saat ikon ini baru menyala
        startLocalPos = transform.localPosition;
    }

    private void Update()
    {
        // Bikin animasi ngambang pakai matematika Sinus
        float newY = startLocalPos.y + (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        transform.localPosition = new Vector3(startLocalPos.x, newY, startLocalPos.z);
        
        // Buat Ikon selalu menghadap tegak ke arah kamera (Billboard)
        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }
    }
}