using System.Collections.Generic;
using UnityEngine;

public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager Instance { get; private set; }

    [Header("Settings")]
    public GameObject floatingTextPrefab; // Masukkan Prefab Teks 3D-mu ke sini
    public int poolSize = 10;

    private Queue<GameObject> textPool = new Queue<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Pre-warm: Buat stok teks di awal game
        for (int i = 0; i < poolSize; i++)
        {
            GameObject textObj = Instantiate(floatingTextPrefab, transform);
            textObj.SetActive(false);
            textPool.Enqueue(textObj);
        }
    }

    public void SpawnPreviewScore(Vector3 position, int estimatedScore)
    {
        GameObject textObj = null;

        if (textPool.Count > 0)
        {
            textObj = textPool.Dequeue();
        }
        else
        {
            // Kalau stok habis karena pemain geser mouse terlalu cepat, buat baru
            textObj = Instantiate(floatingTextPrefab, transform);
        }

        textObj.transform.position = position;
        textObj.SetActive(true);

        FloatingText ft = textObj.GetComponent<FloatingText>();
        if (ft != null)
        {
            ft.Setup(estimatedScore);
        }
    }

    public void ReturnToPool(GameObject textObj)
    {
        textObj.SetActive(false);
        textPool.Enqueue(textObj);
    }
}