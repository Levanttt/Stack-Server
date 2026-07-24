using UnityEngine;
using System.Collections;

public class TileVFX : MonoBehaviour
{
    [Header("Visual References")]
    public Renderer targetRenderer; 
    
    [Tooltip("Indeks material yang akan berkedip (0 = Material 3, 1 = Glass_Unity 1)")]
    public int targetMaterialIndex = 0; 
    
    [Header("Overheat Settings")]
    [ColorUsage(true, true)]
    public Color overheatGlowColor = new Color(2f, 0f, 0f, 1f);
    public float blinkSpeed = 5f;
    
    public GameObject overheatIcon; 

    public bool isOverheating { get; private set; } = false;

    private Material blinkMaterial;
    private Color originalColor;
    private Coroutine overheatCoroutine;

    [Header("Infection Visuals")]
    public bool isInfected = false;
    
    public GameObject warningIcon;

    [Header("Combined Danger Visuals")]
    public GameObject combinedDangerIcon; 

    private void Awake()
    {
        if (targetRenderer != null && targetRenderer.materials.Length > targetMaterialIndex) 
        {
            // Hanya ambil dan modifikasi material sesuai indeks (Element 0)
            blinkMaterial = targetRenderer.materials[targetMaterialIndex];
            
            blinkMaterial.EnableKeyword("_EMISSION");
            
            if (blinkMaterial.HasProperty("_BaseColor"))
                originalColor = blinkMaterial.GetColor("_BaseColor");
            else if (blinkMaterial.HasProperty("_Color"))
                originalColor = blinkMaterial.GetColor("_Color");
            else
                originalColor = Color.white;
        }

        UpdateIconDisplay();
    }

    public void SetOverheatStatus(bool state)
    {
        if (isOverheating == state) return; 
        
        isOverheating = state;

        if (isOverheating)
        {
            if (overheatCoroutine == null) overheatCoroutine = StartCoroutine(BlinkRoutine());
        }
        else
        {
            if (overheatCoroutine != null)
            {
                StopCoroutine(overheatCoroutine);
                overheatCoroutine = null;
            }
            ResetGlow();
        }

        UpdateIconDisplay();
    }

    public void SetInfectedVisual(bool state)
    {
        if (isInfected == state) return;
        
        isInfected = state;
        UpdateIconDisplay();
    }

    private void UpdateIconDisplay()
    {
        if (warningIcon != null) warningIcon.SetActive(false);
        if (overheatIcon != null) overheatIcon.SetActive(false);
        if (combinedDangerIcon != null) combinedDangerIcon.SetActive(false);

        if (isOverheating && isInfected)
        {
            if (combinedDangerIcon != null) combinedDangerIcon.SetActive(true);
        }
        else if (isOverheating)
        {
            if (overheatIcon != null) overheatIcon.SetActive(true);
        }
        else if (isInfected)
        {
            if (warningIcon != null) warningIcon.SetActive(true);
        }
    }

    private IEnumerator BlinkRoutine()
    {
        while (true)
        {
            float lerp = Mathf.PingPong(Time.unscaledTime * blinkSpeed, 1f);
            Color currentEmission = Color.Lerp(Color.black, overheatGlowColor, lerp);
            
            if (blinkMaterial != null)
            {
                blinkMaterial.SetColor("_EmissionColor", currentEmission);
                
                Color targetBaseColor = Color.Lerp(originalColor, overheatGlowColor, lerp);
                
                if (blinkMaterial.HasProperty("_BaseColor"))
                    blinkMaterial.SetColor("_BaseColor", targetBaseColor);
                else if (blinkMaterial.HasProperty("_Color"))
                    blinkMaterial.SetColor("_Color", targetBaseColor);
            }
            yield return null;
        }
    }

    private void ResetGlow()
    {
        if (blinkMaterial != null)
        {
            blinkMaterial.SetColor("_EmissionColor", Color.black);
            
            if (blinkMaterial.HasProperty("_BaseColor"))
                blinkMaterial.SetColor("_BaseColor", originalColor);
            else if (blinkMaterial.HasProperty("_Color"))
                blinkMaterial.SetColor("_Color", originalColor);
        }
    }
}