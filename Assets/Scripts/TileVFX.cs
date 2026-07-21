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

    public bool isOverheating { get; private set; } = false;

    private Material[] materials;
    private Coroutine overheatCoroutine;

    [Header("Infection Visuals")]
    public bool isInfected = false;
    public GameObject warningIcon;

    private void Awake()
    {
        if (targetRenderer != null) 
        {
            materials = targetRenderer.materials;
            foreach (Material mat in materials) mat.EnableKeyword("_EMISSION");
        }
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
    }

    private IEnumerator BlinkRoutine()
    {
        while (true)
        {
            float lerp = Mathf.PingPong(Time.time * blinkSpeed, 1f);
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

    public void SetInfectedVisual(bool state)
    {
        isInfected = state;
        
        if (warningIcon != null)
        {
            // Nyalakan ikon jika terinfeksi, matikan jika aman
            warningIcon.SetActive(isInfected);
        }
    }
}