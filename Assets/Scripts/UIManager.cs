using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections; 

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD Score UI")]
    public Image radialScoreFill;
    public Image previewScoreFill; // <--- VARIABEL BARU UNTUK GHOST PREVIEW
    public TextMeshProUGUI scoreValueText; 
    public TextMeshProUGUI scoreMaxText;   

    [Header("Dynamic Font Scaling")]
    public float valueBaseSize = 32f;
    public float maxBaseSize = 28f;
    public int safeCharacterLimit = 4;       // Batas aman huruf sebelum mulai dikecilkan
    public float sizeReductionPerChar = 2f;  // Turun 2 poin tiap nambah 1 huruf ekstra
    
    [Header("Animation Settings")]
    public float fillAnimationSpeed = 5f;
    public Color milestoneFlashColor = new Color(0.4f, 0.95f, 1f, 1f); 

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI highScoreText;

    private float targetFillAmount = 0f;
    private int lastMilestoneScore = 0;
    private bool isWrappingAround = false;

    private Vector3 originalTextScale;
    private Vector3 originalMaxTextScale;
    private Color originalFillColor;
    private Coroutine celebrationCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (scoreValueText != null) 
        {
            originalTextScale = scoreValueText.transform.localScale;
            scoreValueText.fontSize = valueBaseSize;
        }
        if (scoreMaxText != null) 
        {
            originalMaxTextScale = scoreMaxText.transform.localScale;
            scoreMaxText.fontSize = maxBaseSize;
        }
        if (radialScoreFill != null) originalFillColor = radialScoreFill.color;
        
        // Pastikan ghost bar disembunyikan saat game baru mulai
        HideScorePreview();
    }

    private void Update()
    {
        if (radialScoreFill != null)
        {
            if (isWrappingAround)
            {
                radialScoreFill.fillAmount = Mathf.Lerp(radialScoreFill.fillAmount, 1.05f, Time.deltaTime * fillAnimationSpeed * 1.5f);
                
                if (radialScoreFill.fillAmount >= 1f)
                {
                    radialScoreFill.fillAmount = 0f; 
                    isWrappingAround = false; 
                }
            }
            else if (radialScoreFill.fillAmount != targetFillAmount)
            {
                radialScoreFill.fillAmount = Mathf.Lerp(radialScoreFill.fillAmount, targetFillAmount, Time.deltaTime * fillAnimationSpeed);
            }
        }
    }

    private string FormatScore(int score)
    {
        if (score >= 1000000)
            return (score / 1000000f).ToString("0.#") + "M"; 
        if (score >= 10000)
            return (score / 1000f).ToString("0.#") + "K";    
            
        return score.ToString(); 
    }

    public void UpdateHUDScore(int currentScore, int targetMilestoneScore)
    {
        string formattedCurrent = FormatScore(currentScore);
        string formattedTarget = FormatScore(targetMilestoneScore);

        if (scoreValueText != null) scoreValueText.text = formattedCurrent;
        if (scoreMaxText != null) scoreMaxText.text = formattedTarget;

        // --- LOGIKA DYNAMIC FONT SCALING ---
        int maxCharLength = Mathf.Max(formattedCurrent.Length, formattedTarget.Length);
        int reductionSteps = Mathf.Max(0, maxCharLength - safeCharacterLimit);

        if (scoreValueText != null) 
            scoreValueText.fontSize = valueBaseSize - (reductionSteps * sizeReductionPerChar);
        if (scoreMaxText != null) 
            scoreMaxText.fontSize = maxBaseSize - (reductionSteps * sizeReductionPerChar);

        // --- LOGIKA RADIAL & ANIMASI ---
        if (radialScoreFill != null && targetMilestoneScore > 0)
        {
            if (lastMilestoneScore != 0 && targetMilestoneScore > lastMilestoneScore)
            {
                isWrappingAround = true;
                
                if (celebrationCoroutine != null) StopCoroutine(celebrationCoroutine);
                celebrationCoroutine = StartCoroutine(CelebrateMilestoneRoutine());
            }
            
            lastMilestoneScore = targetMilestoneScore;
            targetFillAmount = (float)currentScore / targetMilestoneScore;
        }
    }

    private IEnumerator CelebrateMilestoneRoutine()
    {
        float duration = 0.4f; 
        float elapsed = 0f;

        if (radialScoreFill != null) radialScoreFill.color = milestoneFlashColor;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float bounceEffect = Mathf.Sin(t * Mathf.PI);
            float currentScale = 1f + (bounceEffect * 0.5f); 
            
            if (scoreValueText != null) 
            {
                scoreValueText.transform.localScale = originalTextScale * currentScale;
            }
            if (scoreMaxText != null) 
            {
                scoreMaxText.transform.localScale = originalMaxTextScale * currentScale;
            }

            if (radialScoreFill != null)
            {
                radialScoreFill.color = Color.Lerp(milestoneFlashColor, originalFillColor, t);
            }

            yield return null;
        }

        if (scoreValueText != null) scoreValueText.transform.localScale = originalTextScale;
        if (scoreMaxText != null) scoreMaxText.transform.localScale = originalMaxTextScale;
        if (radialScoreFill != null) radialScoreFill.color = originalFillColor;
    }

    public void ShowGameOverPanel(int finalScore)
    {
        if (gameOverPanel == null) return;
        gameOverPanel.SetActive(true);
        if (finalScoreText != null) finalScoreText.text = "Score: " + finalScore.ToString(); 
        int highScore = PlayerPrefs.GetInt("HighScore", 0);
        if (finalScore > highScore)
        {
            PlayerPrefs.SetInt("HighScore", finalScore);
            highScore = finalScore;
        }
        if (highScoreText != null) highScoreText.text = "Best: " + highScore.ToString();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // =========================================================
    // --- FUNGSI BARU: GHOST PREVIEW BAR ---
    // =========================================================
    public void ShowScorePreview(int currentScore, int estimatedBonus, int targetScore)
    {
        if (previewScoreFill == null) return;

        if (estimatedBonus > 0 && targetScore > 0)
        {
            previewScoreFill.gameObject.SetActive(true);
            
            float target = (float)targetScore;
            float predictedScore = (float)(currentScore + estimatedBonus);
            
            // Batasi fill maximum agar bayangan tidak melebihi 100% (nilai 1f)
            previewScoreFill.fillAmount = Mathf.Clamp01(predictedScore / target);
        }
        else
        {
            HideScorePreview();
        }
    }

    public void HideScorePreview()
    {
        if (previewScoreFill != null)
        {
            previewScoreFill.gameObject.SetActive(false);
        }
    }
}