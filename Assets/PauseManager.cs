using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject pauseCanvas;
    public CanvasGroup bgDimmer;
    public RectTransform sidePanel;
    public TextMeshProUGUI highscoreValueText;

    [Header("Animation Settings")]
    public float slideDuration = 0.35f;
    public float panelOffScreenX = -400f; 
    public float dimmerMaxAlpha = 0.8f;

    private bool isPaused = false;
    private Coroutine animationCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (pauseCanvas != null) pauseCanvas.SetActive(false);
    }

    private void Update()
    {
        // UPDATE: Diganti menjadi tombol P atau Esc
        if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.currentState == GameState.GameOver) 
                return;

            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    // FUNGSI INI BISA DIPANGGIL OLEH TOMBOL UI DI POJOK LAYAR
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; 
        
        if (pauseCanvas != null) pauseCanvas.SetActive(true);
        
        if (highscoreValueText != null && ScoreManager.Instance != null)
        {
            highscoreValueText.text = ScoreManager.Instance.GetHighScore().ToString("N0");
        }

        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(AnimatePanel(true));
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; 
        
        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(AnimatePanel(false));
    }

    private IEnumerator AnimatePanel(bool isShowing)
    {
        float elapsed = 0f;
        
        float startAlpha = bgDimmer != null ? bgDimmer.alpha : 0f;
        float targetAlpha = isShowing ? dimmerMaxAlpha : 0f;

        Vector2 startPos = sidePanel.anchoredPosition;
        Vector2 targetPos = new Vector2(isShowing ? 0f : panelOffScreenX, sidePanel.anchoredPosition.y);

        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime; 
            float t = elapsed / slideDuration;
            float smoothT = 1f - Mathf.Pow(1f - t, 3f); 

            if (bgDimmer != null) bgDimmer.alpha = Mathf.Lerp(startAlpha, targetAlpha, smoothT);
            if (sidePanel != null) sidePanel.anchoredPosition = Vector2.Lerp(startPos, targetPos, smoothT);

            yield return null;
        }

        if (bgDimmer != null) bgDimmer.alpha = targetAlpha;
        if (sidePanel != null) sidePanel.anchoredPosition = targetPos;

        if (!isShowing && pauseCanvas != null)
        {
            pauseCanvas.SetActive(false);
        }
    }
    
    public void Btn_Resume()
    {
        ResumeGame();
    }

    public void Btn_Restart()
    {
        Time.timeScale = 1f;
        
        if (UIManager.Instance != null) UIManager.Instance.RestartGame();
        else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Btn_MainMenu()
    {
        Time.timeScale = 1f;
        
        if (UIManager.Instance != null) UIManager.Instance.ReturnToMainMenu();
        else SceneManager.LoadScene("MainMenu");
    }
}