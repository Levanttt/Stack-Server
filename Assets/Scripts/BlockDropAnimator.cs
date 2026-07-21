using UnityEngine;
using System.Collections;

public class BlockDropAnimator : MonoBehaviour
{
    private float animDuration = 0.2f;   // Disamakan dengan durasi tile lantai
    private float startOffsetY = 0.5f;  // Mulai dari bawah (menyesuaikan gaya GridManager)

    private void Start()
    {
        StartCoroutine(AnimateDrop());
    }

    private IEnumerator AnimateDrop()
    {
        Vector3 finalPos = transform.position; // Posisi akhir di lantai (y = 0.05f)
        Vector3 startPos = new Vector3(finalPos.x, finalPos.y + startOffsetY, finalPos.z);
        
        transform.position = startPos;

        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            if (this == null || transform == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / animDuration;
            
            // Rumus Smoothstep persis seperti SpawnTilesAnim di GridManager
            float easeT = t * t * (3f - 2f * t); 
            
            transform.position = Vector3.Lerp(startPos, finalPos, easeT);
            yield return null;
        }

        if (transform != null)
        {
            transform.position = finalPos;
        }

        // Hapus script ini dari objek setelah animasi selesai
        Destroy(this); 
    }
}