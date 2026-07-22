using UnityEngine;

public class HighScoreManager : MonoBehaviour
{
    public static HighScoreManager Instance { get; private set; }
    private const string HIGHSCORE_KEY = "HighScore";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetHighScore();
        }
    }

    public int GetHighScore()
    {
        return PlayerPrefs.GetInt(HIGHSCORE_KEY, 0);
    }

    public bool CheckAndSaveNewRecord(int finalScore)
    {
        int currentHigh = GetHighScore();
        
        if (finalScore > currentHigh && finalScore > 0)
        {
            PlayerPrefs.SetInt(HIGHSCORE_KEY, finalScore);
            PlayerPrefs.Save(); 
            
            Debug.Log($"<color=yellow>[HIGHSCORE] NEW RECORD SAVED: {finalScore}</color>");
            return true;
        }
        return false;
    }

    public void ResetHighScore()
    {
        PlayerPrefs.DeleteKey(HIGHSCORE_KEY);
        PlayerPrefs.Save();
        Debug.Log("<color=red>[HIGHSCORE] DATA RESET TO 0!</color>");
    }
}