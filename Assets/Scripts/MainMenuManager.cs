using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using TMPro; 

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Settings")]
    public string gameplaySceneName = "GameScene"; 

    [Header("Highscore Settings")]
    [Tooltip("Masukkan objek Text_HS_Value dari Hierarchy ke sini")]
    public TextMeshProUGUI highscoreValueText; 
    [Tooltip("Lama waktu (dalam detik) angka rolling sampai selesai")]
    public float scoreRollDuration = 1.5f;

    [Header("Credits UI References")]
    public GameObject creditsCanvas; 
    public CanvasGroup bgDimmer;     
    public RectTransform sidePanel;  

    [Header("Animation Settings")]
    public float slideDuration = 0.35f;
    public float panelOffScreenX = 400f; 
    public float panelOnScreenX = 0f;    
    public float dimmerMaxAlpha = 0.8f;

    private Coroutine animationCoroutine;

    private void Start()
    {
        UpdateHighscoreDisplay();

        if (sidePanel != null) 
        {
            sidePanel.anchoredPosition = new Vector2(panelOffScreenX, sidePanel.anchoredPosition.y);
        }
        
        if (bgDimmer != null) bgDimmer.alpha = 0f;
        
        if (creditsCanvas != null) creditsCanvas.SetActive(false);
    }

    private void UpdateHighscoreDisplay()
    {
        if (highscoreValueText != null)
        {
            int savedHighscore = PlayerPrefs.GetInt("HighScore", 0);
            StartCoroutine(RollHighscoreText(savedHighscore));
        }
    }

    private IEnumerator RollHighscoreText(int targetScore)
    {
        float elapsed = 0f;
        int startScore = 0; 
        
        highscoreValueText.text = startScore.ToString();

        while (elapsed < scoreRollDuration)
        {
            elapsed += Time.deltaTime; 
            float t = elapsed / scoreRollDuration;
            
            float smoothT = 1f - Mathf.Pow(1f - t, 3f); 

            int currentScore = Mathf.RoundToInt(Mathf.Lerp(startScore, targetScore, smoothT));
            highscoreValueText.text = currentScore.ToString();

            yield return null;
        }

        highscoreValueText.text = targetScore.ToString();
    }

    public void StartGame()
    {
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Game Keluar!");
        Application.Quit();
    }

    public void OpenCredits()
    {
        if (creditsCanvas != null) creditsCanvas.SetActive(true);

        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(AnimatePanel(true));
    }

    public void CloseCredits()
    {
        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(AnimatePanel(false));
    }

    private IEnumerator AnimatePanel(bool isShowing)
    {
        float elapsed = 0f;
        
        float startAlpha = bgDimmer != null ? bgDimmer.alpha : 0f;
        float targetAlpha = isShowing ? dimmerMaxAlpha : 0f;

        Vector2 startPos = sidePanel.anchoredPosition;
        Vector2 targetPos = new Vector2(isShowing ? panelOnScreenX : panelOffScreenX, sidePanel.anchoredPosition.y);

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime; 
            float t = elapsed / slideDuration;
            
            float smoothT = 1f - Mathf.Pow(1f - t, 3f); 

            if (bgDimmer != null) bgDimmer.alpha = Mathf.Lerp(startAlpha, targetAlpha, smoothT);
            if (sidePanel != null) sidePanel.anchoredPosition = Vector2.Lerp(startPos, targetPos, smoothT);

            yield return null;
        }

        if (bgDimmer != null) bgDimmer.alpha = targetAlpha;
        if (sidePanel != null) sidePanel.anchoredPosition = targetPos;

        if (!isShowing && creditsCanvas != null)
        {
            creditsCanvas.SetActive(false);
        }
    }
}