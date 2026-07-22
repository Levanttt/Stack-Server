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

    [Header("Game Over - Parent & Dimmer")]
    public GameObject gameOverPanel;
    public CanvasGroup bgDimmer; 

    [Header("Game Over - Phase 1 (Stripe Banner)")]
    public CanvasGroup stripeBanner; 
    
    [Header("Game Over - Phase 2 (Main Board)")]
    public RectTransform mainBoard; 
    public TextMeshProUGUI reasonText; 
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI highScoreText;
    
    [Header("Game Over - Phase 3 (New Record Stamp)")]
    public GameObject newRecordStamp;
    public RectTransform newRecordTransform; 
    
    [Header("Game Over - Buttons")]
    public GameObject buttonsGroup; 

    private float actualTargetFill = 0f; 
    private float mainTargetFill = 0f;   
    private float ghostTargetFill = 0f;  

    private int lastMilestoneScore = 0;
    private bool isWrappingAround = false;
    private bool isPreviewing = false; 

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

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
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
                if (scoreFill != null) scoreFill.fillAmount = 0f;
                if (radialScoreFill != null) radialScoreFill.fillAmount = 0f;
                linearVelocity = 0f;
                radialVelocity = 0f;
            }
        }
        else
        {
            if (radialScoreFill != null)
                radialScoreFill.fillAmount = Mathf.SmoothDamp(radialScoreFill.fillAmount, mainTargetFill, ref radialVelocity, barSmoothTime);
            
            if (scoreFill != null)
                scoreFill.fillAmount = Mathf.SmoothDamp(scoreFill.fillAmount, mainTargetFill, ref linearVelocity, barSmoothTime);

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
            
            actualTargetFill = Mathf.Clamp01((float)Mathf.Max(0, currentScore) / targetMilestoneScore);
            
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

    public void ShowScorePreview(int currentScore, int estimatedScore, int targetScore)
    {
        if (targetScore <= 0) targetScore = 1;

        isPreviewing = true; 
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
            mainTargetFill = currentFill;      
            ghostTargetFill = predictedFill;    

            if (scorePreviewFill != null) scorePreviewFill.color = positivePreviewColor;
        }
        else if (estimatedScore < 0)
        {
            mainTargetFill = predictedFill;     
            ghostTargetFill = currentFill;     

            if (scorePreviewFill != null) scorePreviewFill.color = negativePreviewColor; 
        }
        else 
        {
            mainTargetFill = currentFill;
            ghostTargetFill = currentFill;
            if (scorePreviewFill != null) scorePreviewFill.gameObject.SetActive(false);
        }
    }

    public void HideScorePreview()
    {
        isPreviewing = false; 
        if (scorePreviewFill != null) scorePreviewFill.gameObject.SetActive(false);
        mainTargetFill = actualTargetFill;
    }

    // ==========================================
    // LOGIKA GAME OVER CINEMATIC
    // ==========================================
    public void ShowGameOverPanel(int finalScore)
    {
        StartCoroutine(GameOverSequence(finalScore, "SYSTEM OVERLOADED"));
    }

    private IEnumerator GameOverSequence(int finalScore, string reasonMsg)
    {
        // 1. Cek Data Highscore Melalui HighScoreManager
        // Sistem otomatis mengevaluasi dan menyimpan rekor, lalu mengembalikan status True/False
        bool isNewRecord = false;
        int currentHighScore = 0;

        // Memastikan HighScoreManager tidak Null agar aman dari error
        if (HighScoreManager.Instance != null)
        {
            isNewRecord = HighScoreManager.Instance.CheckAndSaveNewRecord(finalScore);
            currentHighScore = HighScoreManager.Instance.GetHighScore();
        }

        // 2. SET UP KONDISI AWAL
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (bgDimmer != null) bgDimmer.alpha = 0f;
        
        if (stripeBanner != null)
        {
            stripeBanner.alpha = 0f;
            stripeBanner.gameObject.SetActive(true);
        }
        
        if (mainBoard != null)
        {
            // LEMPAR JAUH KE -2500 DAN MATIKAN DULU!
            mainBoard.anchoredPosition = new Vector2(-2500f, mainBoard.anchoredPosition.y);
            mainBoard.gameObject.SetActive(false); 
        }

        if (newRecordStamp != null) newRecordStamp.SetActive(false);
        if (buttonsGroup != null) buttonsGroup.SetActive(false);

        // Isi Teks
        if (reasonText != null) reasonText.text = reasonMsg;
        if (finalScoreText != null) finalScoreText.text = finalScore.ToString();
        if (highScoreText != null) 
        {
            highScoreText.text = currentHighScore.ToString();
            highScoreText.color = isNewRecord ? Color.yellow : Color.white;
        }

        // 3. PHASE 1: FADE IN STRIPE BANNER
        float elapsed = 0f;
        float duration = 0.5f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (stripeBanner != null) stripeBanner.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }

        // Jeda dramatis (Stripe Banner tampil sendiri)
        yield return new WaitForSeconds(1.5f);

        // 4. PHASE 2: SLIDE IN MAIN BOARD & FADE BACKGROUND
        // BARU NYALAKAN MAIN BOARD DI SINI SEBELUM MELUNCUR
        if (mainBoard != null) mainBoard.gameObject.SetActive(true);
        
        Vector2 boardStartPos = new Vector2(-2500f, mainBoard != null ? mainBoard.anchoredPosition.y : 0f);
        Vector2 boardCenterPos = new Vector2(0f, mainBoard != null ? mainBoard.anchoredPosition.y : 0f);
        
        elapsed = 0f;
        duration = 0.6f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Rumus smooth step agar luncurannya mulus
            float smoothT = 1f - Mathf.Pow(1f - t, 3f); 

            if (bgDimmer != null) bgDimmer.alpha = Mathf.Lerp(0f, 0.6f, smoothT); 
            if (stripeBanner != null) stripeBanner.alpha = Mathf.Lerp(1f, 0f, smoothT); 
            if (mainBoard != null) mainBoard.anchoredPosition = Vector2.Lerp(boardStartPos, boardCenterPos, smoothT); 

            yield return null;
        }
        
        if (stripeBanner != null) stripeBanner.gameObject.SetActive(false);

        // Jeda sebentar sebelum tombol muncul
        yield return new WaitForSeconds(0.3f);
        if (buttonsGroup != null) buttonsGroup.SetActive(true);

        // 5. PHASE 3: ANIMASI "NEW RECORD!"
        if (isNewRecord && newRecordStamp != null && newRecordTransform != null)
        {
            newRecordStamp.SetActive(true);
            newRecordTransform.localScale = Vector3.zero;

            // Animasi Pop Up
            elapsed = 0f;
            duration = 0.4f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float bounceT = 1f + 0.5f * Mathf.Sin(t * Mathf.PI) * (1f - t); 
                newRecordTransform.localScale = Vector3.one * bounceT;
                yield return null;
            }
            newRecordTransform.localScale = Vector3.one;

            StartCoroutine(PulseNewRecord());
        }
    }

    private IEnumerator PulseNewRecord()
    {
        while (true)
        {
            float elapsed = 0f;
            float duration = 0.8f; // Kecepatan satu siklus detak dan goyangan
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // 1. Animasi Skala (Kedat-kedut membesar)
                // Menggunakan setengah gelombang Sin (0 -> 1 -> 0)
                float scale = 1f + 0.15f * Mathf.Sin(t * Mathf.PI); 
                
                // 2. Animasi Rotasi Z (Miring kanan-kiri)
                // Menggunakan gelombang penuh Sin (0 -> 1 -> 0 -> -1 -> 0) dikali kemiringan maksimal
                float maxTiltAngle = 10f; // Ubah angka ini kalau mau miringnya lebih ekstrem (misal 15f atau 20f)
                float zRotation = Mathf.Sin(t * Mathf.PI * 2f) * maxTiltAngle; 
                
                if (newRecordTransform != null) 
                {
                    // Terapkan skala
                    newRecordTransform.localScale = Vector3.one * scale;
                    // Terapkan rotasi pada sumbu Z
                    newRecordTransform.localEulerAngles = new Vector3(0f, 0f, zRotation); 
                }
                
                yield return null;
            }
        }
    }

    // ==========================================
    // FUNGSI TOMBOL NAVIGASI
    // ==========================================
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu"); // Sesuaikan nama scene menu utamamu
    }
}