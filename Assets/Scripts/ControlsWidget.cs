using UnityEngine;
using TMPro;
using System.Collections;

public class ControlsWidget : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform contentPanel; 
    public TextMeshProUGUI buttonText; 
    public RectTransform arrowIcon; // Referensi untuk memutar ikon panah

    [Header("Text Settings")]
    public string textWhenHidden = "Show Controls";
    public string textWhenShown = "Hide Controls";

    [Header("Animation Settings")]
    public float shownY = 0f;       
    public float hiddenY = -200f;   
    public float slideSpeed = 0.3f; 

    private bool isOpen = false;
    private Coroutine slideCoroutine;

    private void Start()
    {
        contentPanel.anchoredPosition = new Vector2(contentPanel.anchoredPosition.x, hiddenY);
        
        if (buttonText != null) 
            buttonText.text = textWhenHidden;

        if (arrowIcon != null) 
            arrowIcon.localEulerAngles = Vector3.zero; 
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleControls();
        }
    }

    public void ToggleControls()
    {
        isOpen = !isOpen;
        
        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(SlidePanel(isOpen ? shownY : hiddenY));

        // Ubah teks sesuai status
        if (buttonText != null) 
            buttonText.text = isOpen ? textWhenShown : textWhenHidden;

        // Putar ikon panah (180 derajat di sumbu Z jika terbuka, kembali ke 0 jika tertutup)
        if (arrowIcon != null)
        {
            arrowIcon.localEulerAngles = isOpen ? new Vector3(0, 0, 180f) : Vector3.zero;
        }
    }

    private IEnumerator SlidePanel(float targetY)
    {
        float elapsed = 0f;
        Vector2 startPos = contentPanel.anchoredPosition;
        Vector2 endPos = new Vector2(startPos.x, targetY);

        while (elapsed < slideSpeed)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideSpeed;
            float smoothT = t * t * (3f - 2f * t); 
            
            contentPanel.anchoredPosition = Vector2.Lerp(startPos, endPos, smoothT);
            yield return null;
        }
        
        contentPanel.anchoredPosition = endPos;
    }
}