using UnityEngine;
using TMPro;
using System.Collections;

public class ControlsWidget : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform contentPanel; 
    public TextMeshProUGUI buttonText; 
    public RectTransform arrowIcon; 

    [Header("Text Settings")]
    public string textWhenHidden = "Show Controls";
    public string textWhenShown = "Hide Controls";

    [Header("Animation Settings")]
    public float shownY = 0f;       
    public float hiddenY = -200f;   
    public float slideSpeed = 0.3f; 

    private bool isOpen = false;
    private Coroutine slideCoroutine;
    private GameState lastState;

    private void Start()
    {
        contentPanel.anchoredPosition = new Vector2(contentPanel.anchoredPosition.x, hiddenY);
        
        if (buttonText != null) 
            buttonText.text = textWhenHidden;

        if (arrowIcon != null) 
            arrowIcon.localEulerAngles = Vector3.zero; 
            
        if (GameStateManager.Instance != null)
            lastState = GameStateManager.Instance.currentState;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.currentState == GameState.Playing)
            {
                ToggleControls();
            }
        }

        if (GameStateManager.Instance != null && GameStateManager.Instance.currentState != lastState)
        {
            lastState = GameStateManager.Instance.currentState;
            if ((lastState == GameState.Paused || lastState == GameState.GameOver) && isOpen)
            {
                ForceClose();
            }
        }
    }

    public void ToggleControls()
    {
        isOpen = !isOpen;
        
        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(SlidePanel(isOpen ? shownY : hiddenY));

        if (buttonText != null) 
            buttonText.text = isOpen ? textWhenShown : textWhenHidden;

        if (arrowIcon != null)
        {
            arrowIcon.localEulerAngles = isOpen ? new Vector3(0, 0, 180f) : Vector3.zero;
        }
    }
    
    public void ForceClose()
    {
        if (!isOpen) return;
        
        isOpen = false;
        
        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(SlidePanel(hiddenY));

        if (buttonText != null) 
            buttonText.text = textWhenHidden;

        if (arrowIcon != null)
        {
            arrowIcon.localEulerAngles = Vector3.zero;
        }
    }

    private IEnumerator SlidePanel(float targetY)
    {
        float elapsed = 0f;
        Vector2 startPos = contentPanel.anchoredPosition;
        Vector2 endPos = new Vector2(startPos.x, targetY);

        while (elapsed < slideSpeed)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / slideSpeed;
            float smoothT = t * t * (3f - 2f * t); 
            
            contentPanel.anchoredPosition = Vector2.Lerp(startPos, endPos, smoothT);
            yield return null;
        }
        
        contentPanel.anchoredPosition = endPos;
    }
}