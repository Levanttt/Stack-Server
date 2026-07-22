using UnityEngine;

public class FixedRotation : MonoBehaviour
{
    private Quaternion originalLocalRotation;

    private void Awake()
    {
        // Catat rotasi LOKAL aslinya (posisi murni dari dalam Prefab, bebas dari putaran Induk)
        originalLocalRotation = transform.localRotation;
    }

    private void LateUpdate()
    {
        // Paksa arah hadap GLOBAL (dunia) menjadi sama persis dengan arah LOKAL aslinya
        transform.rotation = originalLocalRotation;
    }
}