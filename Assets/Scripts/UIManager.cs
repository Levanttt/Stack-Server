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
    public Image scoreFill;
    public Image scorePreviewFill;

    [Header("Ghost Bar Colors")]
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

    [Header("Game Over - Base Panels")]
    public GameObject gameOverPanel;
    public CanvasGroup bgDimmer;
    public CanvasGroup stripeBanner;
    public RectTransform mainBoard;

    [Header("Game Over - Sliding Rows")]
    public RectTransform textReasonRect;
    public RectTransform finalScoreRowRect;
    public RectTransform highScoreRowRect;
    
    [Header("Game Over - Texts & Stamp")]
    public TextMeshProUGUI reasonText;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI highScoreText;
    public GameObject newRecordStamp;
    public RectTransform newRecordTransform;

    [Header("HUD Audio Settings")]
    public SoundFX hudRollTickSFX;
    public float hudStartPitch = 1.5f;
    public float hudEndPitch = 0.5f;
    public float hudTickInterval = 0.04f; 

    [Header("Game Over Audio Settings")]
    public SoundFX gameOverRollTickSFX;
    public float gameOverStartPitch = 0.8f;
    public float gameOverEndPitch = 1f;
    public float gameOverTickInterval = 0.08f; 
    public SoundFX gameOverShowSFX; 
    public float gameOverShowStartPitch = 1.2f; 
    public float gameOverShowEndPitch = 0.4f;   
    public float gameOverShowPitchDuration = 1f;

    [Header("Game Over - Buttons")]
    public CanvasGroup buttonsGroupCanvas;

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
    private Coroutine wrapCoroutine;

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
        if (isWrappingAround) return; 

        if (radialScoreFill != null)
            radialScoreFill.fillAmount = Mathf.SmoothDamp(radialScoreFill.fillAmount, mainTargetFill, ref radialVelocity, barSmoothTime);

        if (scoreFill != null)
            scoreFill.fillAmount = Mathf.SmoothDamp(scoreFill.fillAmount, mainTargetFill, ref linearVelocity, barSmoothTime);

        if (scorePreviewFill != null && scorePreviewFill.gameObject.activeSelf)
        {
            if (isPreviewing)
            {
                scorePreviewFill.fillAmount = Mathf.SmoothDamp(scorePreviewFill.fillAmount, ghostTargetFill, ref ghostVelocity, barSmoothTime * 0.8f);
            }
            else
            {
                scorePreviewFill.fillAmount = Mathf.SmoothDamp(scorePreviewFill.fillAmount, actualTargetFill, ref ghostVelocity, barSmoothTime);

                bool isGhostDone = Mathf.Abs(scorePreviewFill.fillAmount - actualTargetFill) < 0.005f;
                bool isMainDone = scoreFill == null || Mathf.Abs(scoreFill.fillAmount - actualTargetFill) < 0.005f;
                
                if (isGhostDone && isMainDone)
                {
                    scorePreviewFill.gameObject.SetActive(false);
                }
            }
        }
    }

    private string FormatScore(int score)
    {
        bool isNegative = score < 0;
        int absScore = Mathf.Abs(score);
        string result = "";
        
        if (absScore >= 1000000) result = (absScore / 1000000f).ToString("0.#") + "M";
        else if (absScore >= 10000) result = (absScore / 1000f).ToString("0.#") + "K";
        else result = absScore.ToString();

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
            actualTargetFill = Mathf.Clamp01((float)Mathf.Max(0, currentScore) / targetMilestoneScore);

            if (!isPreviewing)
            {
                mainTargetFill = actualTargetFill;
            }

            if (lastMilestoneScore != 0 && targetMilestoneScore > lastMilestoneScore)
            {
                if (wrapCoroutine != null) StopCoroutine(wrapCoroutine);
                wrapCoroutine = StartCoroutine(WrapBarRoutine());

                if (celebrationCoroutine != null) StopCoroutine(celebrationCoroutine);
                celebrationCoroutine = StartCoroutine(CelebrateMilestoneRoutine());
            }

            lastMilestoneScore = targetMilestoneScore;
        }
    }

    private IEnumerator WrapBarRoutine()
    {
        isWrappingAround = true;
    
        float speed = 2.5f; 
        float currentFill = radialScoreFill != null ? radialScoreFill.fillAmount : 0f;
        
        while (currentFill < 1f)
        {
            currentFill = Mathf.MoveTowards(currentFill, 1f, Time.deltaTime * speed);
            
            if (radialScoreFill != null) radialScoreFill.fillAmount = currentFill;
            if (scoreFill != null) scoreFill.fillAmount = currentFill;
            if (scorePreviewFill != null && scorePreviewFill.gameObject.activeSelf) scorePreviewFill.fillAmount = currentFill;
            
            yield return null;
        }
        
        if (radialScoreFill != null) radialScoreFill.fillAmount = 0f;
        if (scoreFill != null) scoreFill.fillAmount = 0f;
        
        if (scorePreviewFill != null && scorePreviewFill.gameObject.activeSelf) scorePreviewFill.fillAmount = 0f;
        
        radialVelocity = 0f;
        linearVelocity = 0f;
        ghostVelocity = 0f;
        
        isWrappingAround = false;
    }

    private IEnumerator LerpScoreText(int endScore, int targetMilestoneScore)
    {
        float duration = 0.3f;
        float elapsed = 0f;
        int startScore = displayedScore;
        float nextTickTime = 0f; 
        
        bool isMinus = endScore < startScore;
        bool hasScoreChanged = endScore != startScore;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            displayedScore = Mathf.RoundToInt(Mathf.Lerp(startScore, endScore, t));
            UpdateTextVisuals(displayedScore, targetMilestoneScore);

            if (hasScoreChanged && elapsed >= nextTickTime)
            {
                if (AudioManager.Instance != null && hudRollTickSFX != null && hudRollTickSFX.clip != null)
                {
                    float currentPitch;
                    
                    if (isMinus)
                    {
                        currentPitch = Mathf.Lerp(hudEndPitch, hudStartPitch, t);
                    }
                    else
                    {
                        currentPitch = Mathf.Lerp(hudStartPitch, hudEndPitch, t);
                    }
                    
                    float originalPitch = hudRollTickSFX.pitch;
                    hudRollTickSFX.pitch = currentPitch;
                    
                    AudioManager.Instance.PlaySFX(hudRollTickSFX);
                    
                    hudRollTickSFX.pitch = originalPitch; 
                }
                nextTickTime = elapsed + hudTickInterval; 
            }

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
        mainTargetFill = actualTargetFill;
    }

    public void ShowGameOverPanel(int finalScore, string reasonMsg)
    {
        StartCoroutine(GameOverSequence(finalScore, reasonMsg));
    }

    private IEnumerator GameOverSequence(int finalScore, string reasonMsg)
    {
        bool isNewRecord = false;
        int currentHighScore = 0;

        if (HighScoreManager.Instance != null)
        {
            isNewRecord = HighScoreManager.Instance.CheckAndSaveNewRecord(finalScore);
            currentHighScore = HighScoreManager.Instance.GetHighScore();
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (bgDimmer != null) bgDimmer.alpha = 0f;
        
        float offScreenX = -2500f;
        
        if (mainBoard != null) mainBoard.anchoredPosition = new Vector2(offScreenX, mainBoard.anchoredPosition.y); 

        if (textReasonRect != null) textReasonRect.anchoredPosition = new Vector2(offScreenX, textReasonRect.anchoredPosition.y);
        if (finalScoreRowRect != null) finalScoreRowRect.anchoredPosition = new Vector2(offScreenX, finalScoreRowRect.anchoredPosition.y);
        if (highScoreRowRect != null) highScoreRowRect.anchoredPosition = new Vector2(offScreenX, highScoreRowRect.anchoredPosition.y);

        if (buttonsGroupCanvas != null)
        {
            buttonsGroupCanvas.alpha = 0f;
            buttonsGroupCanvas.gameObject.SetActive(true);
        }

        if (newRecordStamp != null) newRecordStamp.SetActive(false);
        if (reasonText != null) reasonText.text = reasonMsg;
        if (finalScoreText != null) finalScoreText.text = "0";
        if (highScoreText != null) highScoreText.text = "0";

        if (stripeBanner != null)
        {
            stripeBanner.alpha = 0f;
            stripeBanner.gameObject.SetActive(true);
        }

        if (AudioManager.Instance != null && gameOverShowSFX != null)
        {
            AudioSource bannerAudio = AudioManager.Instance.PlaySFX(gameOverShowSFX);
            if (bannerAudio != null)
            {
                StartCoroutine(SlideAudioPitch(bannerAudio, gameOverShowStartPitch, gameOverShowEndPitch, gameOverShowPitchDuration));
            }
        }

        float elapsed = 0f;
        float duration = 0.25f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (stripeBanner != null) stripeBanner.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }

        yield return new WaitForSeconds(0.8f); 

        elapsed = 0f;
        duration = 0.35f;
        Vector2 boardStartPos = new Vector2(offScreenX, mainBoard != null ? mainBoard.anchoredPosition.y : 0f);
        Vector2 boardEndPos = new Vector2(0f, mainBoard != null ? mainBoard.anchoredPosition.y : 0f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smoothT = 1f - Mathf.Pow(1f - t, 3f); 
            
            if (bgDimmer != null) bgDimmer.alpha = Mathf.Lerp(0f, 0.8f, smoothT);
            if (mainBoard != null) mainBoard.anchoredPosition = Vector2.Lerp(boardStartPos, boardEndPos, smoothT);
            
            yield return null;
        }
        
        if (mainBoard != null) mainBoard.anchoredPosition = boardEndPos;

        if (stripeBanner != null) stripeBanner.gameObject.SetActive(false);

        yield return new WaitForSeconds(0.1f);

        yield return StartCoroutine(SlideUI(textReasonRect, offScreenX, 0f, 0.4f));
        yield return new WaitForSeconds(0.1f);

        yield return StartCoroutine(SlideUI(finalScoreRowRect, offScreenX, 0f, 0.4f));
        yield return StartCoroutine(RollTextNumber(finalScoreText, finalScore, 0.6f));
        yield return new WaitForSeconds(0.15f);

        yield return StartCoroutine(SlideUI(highScoreRowRect, offScreenX, 0f, 0.4f));
        if (highScoreText != null) highScoreText.color = isNewRecord ? Color.yellow : Color.white;
        yield return StartCoroutine(RollTextNumber(highScoreText, currentHighScore, 0.6f));
        yield return new WaitForSeconds(0.2f);

        if (isNewRecord && newRecordStamp != null && newRecordTransform != null)
        {
            newRecordStamp.SetActive(true);
            newRecordTransform.localScale = Vector3.zero;

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

        if (buttonsGroupCanvas != null)
        {
            elapsed = 0f;
            duration = 0.4f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                buttonsGroupCanvas.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }
        }
    }

    private IEnumerator SlideUI(RectTransform target, float startX, float endX, float duration)
    {
        if (target == null) yield break;

        float elapsed = 0f;
        Vector2 startPos = new Vector2(startX, target.anchoredPosition.y);
        Vector2 endPos = new Vector2(endX, target.anchoredPosition.y);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smoothT = 1f - Mathf.Pow(1f - t, 3f); 
            target.anchoredPosition = Vector2.Lerp(startPos, endPos, smoothT);
            yield return null;
        }
        target.anchoredPosition = endPos;
    }

    private IEnumerator RollTextNumber(TextMeshProUGUI textElement, int targetNumber, float duration)
    {
        if (textElement == null) yield break;

        float elapsed = 0f;
        float nextTickTime = 0f; 

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            int currentVal = Mathf.RoundToInt(Mathf.Lerp(0, targetNumber, t));
            
            textElement.text = currentVal.ToString("N0"); 

            if (elapsed >= nextTickTime)
            {
                if (AudioManager.Instance != null && gameOverRollTickSFX != null && gameOverRollTickSFX.clip != null)
                {
                    float currentPitch = Mathf.Lerp(gameOverStartPitch, gameOverEndPitch, t);
                    
                    float originalPitch = gameOverRollTickSFX.pitch;
                    gameOverRollTickSFX.pitch = currentPitch;
                    
                    AudioManager.Instance.PlaySFX(gameOverRollTickSFX);
                    
                    gameOverRollTickSFX.pitch = originalPitch; 
                }
                nextTickTime = elapsed + gameOverTickInterval; 
            }

            yield return null;
        }
        textElement.text = targetNumber.ToString("N0");
    }

    private IEnumerator PulseNewRecord()
    {
        while (true)
        {
            float elapsed = 0f;
            float duration = 0.8f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                float scale = 1f + 0.15f * Mathf.Sin(t * Mathf.PI);
                float maxTiltAngle = 10f;
                float zRotation = Mathf.Sin(t * Mathf.PI * 2f) * maxTiltAngle;

                if (newRecordTransform != null)
                {
                    newRecordTransform.localScale = Vector3.one * scale;
                    newRecordTransform.localEulerAngles = new Vector3(0f, 0f, zRotation);
                }

                yield return null;
            }
        }
    }

    private IEnumerator SlideAudioPitch(AudioSource source, float startPitch, float endPitch, float slideDuration)
    {
        float timeElapsed = 0f;
        
        while (timeElapsed < slideDuration && source != null)
        {
            timeElapsed += Time.deltaTime;
            source.pitch = Mathf.Lerp(startPitch, endPitch, timeElapsed / slideDuration);
            yield return null;
        }
        
        if (source != null) source.pitch = endPitch;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}