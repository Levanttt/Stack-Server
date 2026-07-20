using UnityEngine;

public enum GameState 
{ 
    MainMenu, 
    Playing, 
    Paused, 
    GameOver 
}

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Header("Current State")]
    public GameState currentState;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Default saat scene dimulai
        ChangeState(GameState.Playing);
    }

    public void ChangeState(GameState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            case GameState.Playing:
                Time.timeScale = 1f; // Pastikan waktu berjalan normal
                break;
            case GameState.Paused:
                Time.timeScale = 0f; // Hentikan pergerakan physics/update (jika ada)
                break;
            case GameState.GameOver:
                HandleGameOver();
                break;
        }
    }

    private void HandleGameOver()
    {
        Debug.LogWarning("==== GAME OVER TRIGGERED DARI STATE MANAGER ====");
        
        // Di sini nanti kamu bisa panggil UI Manager untuk memunculkan panel Game Over
        // UIManager.Instance.ShowGameOverPanel();
        
        // Stop waktu jika diperlukan, atau biarkan agar efek juice tetap jalan
        // Time.timeScale = 0f; 
    }
}