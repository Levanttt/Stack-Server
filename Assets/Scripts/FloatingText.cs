using UnityEngine;
using TMPro;
using System.Collections;
using System; 

public class FloatingText : MonoBehaviour
{
    public TextMeshPro textMesh;
    
    [Header("Animation Timings")]
    public float popUpSpeed = 1.5f;
    public float popUpDuration = 0.4f;   
    public float hoverDuration = 0.15f;  
    public float flyDuration = 0.3f;     

    [Header("Text Colors")]
    public Color positiveColor = Color.green;
    public Color negativeColor = Color.red;
    public Color neutralColor = Color.gray;

    private Camera mainCam;
    private Color baseColor;

    private void Awake()
    {
        mainCam = Camera.main;
    }

    private void Update()
    {
        if (mainCam != null)
            transform.rotation = mainCam.transform.rotation;
    }

    public void Setup(int score, RectTransform targetUI = null, Action onArrive = null)
    {
        if (score > 0) 
        {
            textMesh.text = $"+{score}"; 
            if (ScoreManager.Instance != null && score >= ScoreManager.Instance.scoreFullLineBonus)
                baseColor = new Color(1f, 0.84f, 0f, 1f); 
            else
                baseColor = positiveColor;
        }
        else if (score < 0) { textMesh.text = $"{score}"; baseColor = negativeColor; }
        else { textMesh.text = "0"; baseColor = neutralColor; }

        textMesh.color = baseColor;
        transform.localScale = Vector3.one;

        if (targetUI != null)
            StartCoroutine(FloatAndFlyToUI(targetUI, onArrive));
        else
            StartCoroutine(AnimateAndPool());
    }

    public void SetupCustomText(string customText, Color customColor, RectTransform targetUI = null, Action onArrive = null)
    {
        textMesh.text = customText;
        baseColor = customColor;
        textMesh.color = baseColor;
        transform.localScale = Vector3.one;

        if (targetUI != null)
            StartCoroutine(FloatAndFlyToUI(targetUI, onArrive));
        else
            StartCoroutine(AnimateAndPool());
    }

    public void ChangeToJackpot(int additionalScore)
    {
        int currentScore = 0;
        string rawText = textMesh.text.Replace("+", "");
        int.TryParse(rawText, out currentScore);
        
        int totalScore = currentScore + additionalScore;
        textMesh.text = $"+{totalScore}";
        
        baseColor = new Color(1f, 0.84f, 0f, 1f);
        textMesh.color = baseColor;
    }

    private IEnumerator AnimateAndPool()
    {
        float elapsed = 0f;
        float lifetime = popUpDuration + hoverDuration;

        while (elapsed < lifetime)
        {
            transform.position += Vector3.up * popUpSpeed * Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / lifetime);
            textMesh.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        FloatingTextManager.Instance.ReturnToPool(this.gameObject);
    }

    private IEnumerator FloatAndFlyToUI(RectTransform targetUI, Action onArrive)
    {
        float elapsed = 0f;
        
        while (elapsed < popUpDuration)
        {
            transform.position += Vector3.up * popUpSpeed * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(hoverDuration);

        elapsed = 0f;
        Vector3 startPos = transform.position;
        Vector3 initialScale = transform.localScale;

        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flyDuration;
            float easeInT = t * t * t; 

            Vector3 screenPos = targetUI.position;
            screenPos.z = mainCam.nearClipPlane + 2f; 
            Vector3 targetWorldPos = mainCam.ScreenToWorldPoint(screenPos);

            transform.position = Vector3.Lerp(startPos, targetWorldPos, easeInT);
            transform.localScale = Vector3.Lerp(initialScale, initialScale * 0.5f, easeInT);
            
            textMesh.color = baseColor; 
            
            yield return null;
        }

        onArrive?.Invoke();

        transform.localScale = Vector3.one; 
        FloatingTextManager.Instance.ReturnToPool(this.gameObject);
    }
}