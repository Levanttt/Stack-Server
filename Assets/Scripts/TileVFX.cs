using UnityEngine;
using System.Collections;

public class TileVFX : MonoBehaviour
{
    [Header("Visual References")]
    public Renderer targetRenderer; 
    
    [Header("Overheat Settings")]
    [ColorUsage(true, true)]
    public Color overheatGlowColor = new Color(2f, 0f, 0f, 1f);
    public float blinkSpeed = 5f;
    
    [Tooltip("Ikon sprite yang muncul saat block ini overheat (kepanasan) saja")]
    public GameObject overheatIcon; 

    public bool isOverheating { get; private set; } = false;

    private Material[] materials;
    private Coroutine overheatCoroutine;

    [Header("Infection Visuals")]
    public bool isInfected = false;
    
    [Tooltip("Ikon sprite yang muncul saat block terinfeksi malware saja")]
    public GameObject warningIcon;

    [Header("Combined Danger Visuals")]
    [Tooltip("Ikon sprite gabungan saat block terinfeksi SEKALIGUS overheat")]
    public GameObject combinedDangerIcon; 

    private void Awake()
    {
        if (targetRenderer != null) 
        {
            materials = targetRenderer.materials;
            foreach (Material mat in materials) mat.EnableKeyword("_EMISSION");
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
            Color currentGlow = Color.Lerp(Color.black, overheatGlowColor, lerp);
            SetEmissionColor(currentGlow);
            yield return null;
        }
    }

    private void SetEmissionColor(Color color)
    {
        if (materials != null)
        {
            foreach (Material mat in materials) mat.SetColor("_EmissionColor", color);
        }
    }

    private void ResetGlow()
    {
        SetEmissionColor(Color.black);
    }
}