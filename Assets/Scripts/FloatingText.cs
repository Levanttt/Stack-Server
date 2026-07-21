using UnityEngine;
using TMPro;
using System.Collections;

public class FloatingText : MonoBehaviour
{
    public TextMeshPro textMesh;
    public float floatSpeed = 1.5f;
    public float lifetime = 1f;

    private Camera mainCam;

    private void Awake()
    {
        mainCam = Camera.main;
    }

    private void Update()
    {
        // Billboard: Teks selalu menghadap ke arah kamera agar terbaca
        if (mainCam != null)
        {
            transform.rotation = mainCam.transform.rotation;
        }
    }

    public void Setup(int score)
    {
        if (score > 0)
        {
            textMesh.text = $"+{score}";
            textMesh.color = Color.green;
        }
        else if (score < 0)
        {
            textMesh.text = $"{score}";
            textMesh.color = Color.red;
        }
        else
        {
            textMesh.text = "0";
            textMesh.color = Color.gray;
        }

        StartCoroutine(AnimateAndPool());
    }

    private IEnumerator AnimateAndPool()
    {
        float elapsed = 0f;
        Color startColor = textMesh.color;

        while (elapsed < lifetime)
        {
            transform.position += Vector3.up * floatSpeed * Time.deltaTime;
            
            // Fade out alpha (transparansi)
            float alpha = Mathf.Lerp(1f, 0f, elapsed / lifetime);
            textMesh.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Kembalikan ke Manager (Object Pool)
        FloatingTextManager.Instance.ReturnToPool(this.gameObject);
    }
}