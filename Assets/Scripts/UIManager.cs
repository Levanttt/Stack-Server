using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD Score UI")]
    public Image radialScoreFill;
    public TextMeshProUGUI scoreValueText; // Text kiri (e.g., 257)
    public TextMeshProUGUI scoreMaxText;   // Text kanan (e.g., /320)
    
    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI highScoreText;

    private float targetFillAmount = 0f;
    private float fillAnimationSpeed = 5f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        // Animasi halus untuk radial bar
        if (radialScoreFill != null && radialScoreFill.fillAmount != targetFillAmount)
        {
            radialScoreFill.fillAmount = Mathf.Lerp(radialScoreFill.fillAmount, targetFillAmount, Time.deltaTime * fillAnimationSpeed);
        }
    }

    public void UpdateHUDScore(int currentScore, int targetMilestoneScore)
    {
        if (scoreValueText != null) scoreValueText.text = currentScore.ToString();
        if (scoreMaxText != null) scoreMaxText.text = "/" + targetMilestoneScore.ToString();

        if (radialScoreFill != null && targetMilestoneScore > 0)
        {
            targetFillAmount = (float)currentScore / targetMilestoneScore;
        }
    }

    public void ShowGameOverPanel(int finalScore)
    {
        if (gameOverPanel == null)
        {
            Debug.Log("Panel Game Over belum dibuat, skip UI.");
            return;
        }

        gameOverPanel.SetActive(true);
        
        if (finalScoreText != null) 
            finalScoreText.text = "Score: " + finalScore.ToString();

        int highScore = PlayerPrefs.GetInt("HighScore", 0);
        if (finalScore > highScore)
        {
            PlayerPrefs.SetInt("HighScore", finalScore);
            highScore = finalScore;
        }
        
        if (highScoreText != null) 
            highScoreText.text = "Best: " + highScore.ToString();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}