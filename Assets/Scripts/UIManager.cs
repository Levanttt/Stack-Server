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
    public TextMeshProUGUI scoreValueText; 
    public TextMeshProUGUI scoreMaxText;   

    [Header("Score Bar UI")]
    public UnityEngine.UI.Image scoreFill;         
    public UnityEngine.UI.Image scorePreviewFill;

    [Header("Ghost Bar Colors (Atur di Sini!)")]
    public Color positivePreviewColor = new Color(0f, 1f, 0f, 0.6f); 
    public Color negativePreviewColor = new Color(1f, 0f, 0f, 1f);   

    [Header("Dynamic Font Scaling")]
    public float valueBaseSize = 32f;
    public float maxBaseSize = 28f;
    public int safeCharacterLimit = 4;      
    public float sizeReductionPerChar = 2f;  
    
    [Header("Animation Settings")]
    public float fillAnimationSpeed = 5f;
    public float barSmoothTime = 0.15f; 
    public Color milestoneFlashColor = new Color(0.4f, 0.95f, 1f, 1f); 
    public Color negativeScoreColor = Color.red; 

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI highScoreText;

    // --- SISTEM TARGET BAR BARU (Anti-Glitch) ---
    private float actualTargetFill = 0f; // Target asli (Skor murni)
    private float mainTargetFill = 0f;   // Target untuk bar biru (Bisa menciut saat preview minus)
    private float ghostTargetFill = 0f;  // Target untuk bar bayangan

    private int lastMilestoneScore = 0;
    private bool isWrappingAround = false;
    private bool isPreviewing = false; 

    // Variabel pegas untuk pergerakan mulus
    private float radialVelocity = 0f;
    private float linearVelocity = 0f;
    private float ghostVelocity = 0f;

    private int displayedScore = 0;
    private int targetScoreValue = 0;
    private Coroutine scoreLerpCoroutine;

    private Vector3 originalTextScale;
    private Vector3 originalMaxTextScale;
    private Color originalFillColor;
    private Color originalTextColor;
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
            originalTextColor = scoreValueText.color; 
        }
        if (scoreMaxText != null) 
        {
            originalMaxTextScale = scoreMaxText.transform.localScale;
            scoreMaxText.fontSize = maxBaseSize;
        }
        if (radialScoreFill != null) originalFillColor = radialScoreFill.color;
        
        HideScorePreview();
    }

    private void Update()
    {
        if (isWrappingAround)
        {
            float step = Time.deltaTime * fillAnimationSpeed * 2f;
            bool radialDone = false;
            bool linearDone = false;

            if (radialScoreFill != null)
            {
                radialScoreFill.fillAmount = Mathf.MoveTowards(radialScoreFill.fillAmount, 1f, step);
                if (radialScoreFill.fillAmount >= 1f) radialDone = true;
            }
            else radialDone = true;

            if (scoreFill != null)
            {
                scoreFill.fillAmount = Mathf.MoveTowards(scoreFill.fillAmount, 1f, step);
                if (scoreFill.fillAmount >= 1f) linearDone = true;
            }
            else linearDone = true;

            if (radialDone && linearDone) 
            {
                isWrappingAround = false;
                // Reset ke 0 secara instan agar animasi ronde selanjutnya mulai dari bawah
                if (scoreFill != null) scoreFill.fillAmount = 0f;
                if (radialScoreFill != null) radialScoreFill.fillAmount = 0f;
                linearVelocity = 0f;
                radialVelocity = 0f;
            }
        }
        else
        {
            // --- KUNCI ANTI-GLITCH ---
            // Bar utama SELALU bergerak mulus mengejar 'mainTargetFill', tidak pernah dipaksa instan!
            if (radialScoreFill != null)
                radialScoreFill.fillAmount = Mathf.SmoothDamp(radialScoreFill.fillAmount, mainTargetFill, ref radialVelocity, barSmoothTime);
            
            if (scoreFill != null)
                scoreFill.fillAmount = Mathf.SmoothDamp(scoreFill.fillAmount, mainTargetFill, ref linearVelocity, barSmoothTime);

            // Bar bayangan juga bergerak mulus mengejar targetnya sendiri
            if (isPreviewing && scorePreviewFill != null && scorePreviewFill.gameObject.activeSelf)
            {
                scorePreviewFill.fillAmount = Mathf.SmoothDamp(scorePreviewFill.fillAmount, ghostTargetFill, ref ghostVelocity, barSmoothTime * 0.8f);
            }
        }
    }

    private string FormatScore(int score)
    {
        bool isNegative = score < 0;
        int absScore = Mathf.Abs(score);

        string result = "";
        if (absScore >= 1000000)
            result = (absScore / 1000000f).ToString("0.#") + "M"; 
        else if (absScore >= 10000)
            result = (absScore / 1000f).ToString("0.#") + "K";    
        else 
            result = absScore.ToString(); 

        return isNegative ? "-" + result : result;
    }

    public void UpdateHUDScore(int currentScore, int targetMilestoneScore)
    {
        targetScoreValue = currentScore;

        if (scoreLerpCoroutine != null) StopCoroutine(scoreLerpCoroutine);
        scoreLerpCoroutine = StartCoroutine(LerpScoreText(targetScoreValue, targetMilestoneScore));

        string formattedTarget = FormatScore(targetMilestoneScore);
        if (scoreMaxText != null) scoreMaxText.text = formattedTarget;

        if (targetMilestoneScore > 0)
        {
            if (lastMilestoneScore != 0 && targetMilestoneScore > lastMilestoneScore)
            {
                isWrappingAround = true;
                if (celebrationCoroutine != null) StopCoroutine(celebrationCoroutine);
                celebrationCoroutine = StartCoroutine(CelebrateMilestoneRoutine());
            }
            
            lastMilestoneScore = targetMilestoneScore;
            
            // Simpan target posisi bar yang murni dari sistem skor saat ini
            actualTargetFill = Mathf.Clamp01((float)Mathf.Max(0, currentScore) / targetMilestoneScore);
            
            // Jika pemain sedang TIDAK melihat preview, sinkronkan target utamanya
            if (!isPreviewing) 
            {
                mainTargetFill = actualTargetFill;
            }
        }
    }

    private IEnumerator LerpScoreText(int endScore, int targetMilestoneScore)
    {
        float duration = 0.3f;
        float elapsed = 0f;
        int startScore = displayedScore;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            displayedScore = Mathf.RoundToInt(Mathf.Lerp(startScore, endScore, t));
            UpdateTextVisuals(displayedScore, targetMilestoneScore);
            yield return null;
        }

        displayedScore = endScore;
        UpdateTextVisuals(displayedScore, targetMilestoneScore);
    }

    private void UpdateTextVisuals(int currentDisplayScore, int targetMilestoneScore)
    {
        string formattedCurrent = FormatScore(currentDisplayScore);
        string formattedTarget = FormatScore(targetMilestoneScore);

        if (scoreValueText != null) 
        {
            scoreValueText.text = formattedCurrent;
            scoreValueText.color = currentDisplayScore < 0 ? negativeScoreColor : originalTextColor; 
        }

        int maxCharLength = Mathf.Max(formattedCurrent.Length, formattedTarget.Length);
        int reductionSteps = Mathf.Max(0, maxCharLength - safeCharacterLimit);

        if (scoreValueText != null) 
            scoreValueText.fontSize = valueBaseSize - (reductionSteps * sizeReductionPerChar);
        if (scoreMaxText != null) 
            scoreMaxText.fontSize = maxBaseSize - (reductionSteps * sizeReductionPerChar);
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
            
            if (scoreValueText != null) scoreValueText.transform.localScale = originalTextScale * currentScale;
            if (scoreMaxText != null) scoreMaxText.transform.localScale = originalMaxTextScale * currentScale;
            if (radialScoreFill != null) radialScoreFill.color = Color.Lerp(milestoneFlashColor, originalFillColor, t);

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
    // --- GHOST PREVIEW BAR (ANTI-GLITCH VERSION) ---
    // =========================================================
    public void ShowScorePreview(int currentScore, int estimatedScore, int targetScore)
    {
        if (targetScore <= 0) targetScore = 1;

        isPreviewing = true; 

        // Munculkan bar bayangan dan "tancapkan" di posisi bar utama sekarang
        // agar dia memanjang/berubah dengan mulus, tidak tiba-tiba terbang dari angka 0.
        if (scorePreviewFill != null && !scorePreviewFill.gameObject.activeSelf)
        {
            scorePreviewFill.gameObject.SetActive(true);
            scorePreviewFill.fillAmount = scoreFill != null ? scoreFill.fillAmount : 0f;
        }

        int predictedScore = Mathf.Max(0, currentScore + estimatedScore);
        int safeCurrentScore = Mathf.Max(0, currentScore);

        float currentFill = Mathf.Clamp01((float)safeCurrentScore / targetScore);
        float predictedFill = Mathf.Clamp01((float)predictedScore / targetScore);

        if (estimatedScore > 0)
        {
            // PREDIKSI POSITIF (+)
            mainTargetFill = currentFill;       // Bar biru diam di tempat
            ghostTargetFill = predictedFill;    // Bar bayangan memanjang ke depan mengejar bonus

            if (scorePreviewFill != null) scorePreviewFill.color = positivePreviewColor;
        }
        else if (estimatedScore < 0)
        {
            // PREDIKSI NEGATIF (-)
            mainTargetFill = predictedFill;     // Bar biru menciut ke belakang menjauhi bahaya!
            ghostTargetFill = currentFill;      // Bar bayangan (merah) menetap di titik saat ini untuk menyoroti kerugian

            if (scorePreviewFill != null) scorePreviewFill.color = negativePreviewColor; 
        }
        else 
        {
            // PREDIKSI NOL
            mainTargetFill = currentFill;
            ghostTargetFill = currentFill;
            if (scorePreviewFill != null) scorePreviewFill.gameObject.SetActive(false);
        }
    }

    public void HideScorePreview()
    {
        isPreviewing = false; 
        if (scorePreviewFill != null) scorePreviewFill.gameObject.SetActive(false);

        // Kembalikan target bar utama ke skor asli saat mouse dialihkan atau saat blok selesai ditaruh
        mainTargetFill = actualTargetFill;
    }
}