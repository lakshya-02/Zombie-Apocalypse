using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton that manages game state: score, survival time, game over, and restart.
/// Attach to an empty GameObject named "GameManager" in the scene.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public bool isGameActive = false;
    public int score = 0;
    public float survivalTime = 0f;
    public int zombiesKilled = 0;

    [Header("Points")]
    public int pointsPerKill = 10;
    public int pointsPerJumpscareKill = 25;

    [Header("References")]
    public GameObject gameOverCanvas;
    public ZombieSpawner zombieSpawner;
    public HUDManager hudManager;

    // Events for other scripts to listen to
    public System.Action OnGameOver;
    public System.Action OnGameStart;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        StartGame();
    }

    private void Update()
    {
        // Check for restart input when game is over (must be BEFORE the early return)
        if (!isGameActive)
        {
            if (OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger) ||
                OVRInput.GetDown(OVRInput.RawButton.LIndexTrigger))
            {
                RestartGame();
            }
            return;
        }

        survivalTime += Time.deltaTime;
    }

    public void StartGame()
    {
        isGameActive = true;
        score = 0;
        zombiesKilled = 0;
        survivalTime = 0f;
        Time.timeScale = 1f;

        if (gameOverCanvas != null)
            gameOverCanvas.SetActive(false);

        OnGameStart?.Invoke();
        Debug.Log("[GameManager] Game Started!");
    }

    public void AddScore(int points)
    {
        if (!isGameActive) return;
        score += points;
        zombiesKilled++;
        Debug.Log($"[GameManager] Score: {score} | Kills: {zombiesKilled}");
    }

    public void AddKill(bool isJumpscare = false)
    {
        int points = isJumpscare ? pointsPerJumpscareKill : pointsPerKill;
        AddScore(points);
    }

    public void GameOver()
    {
        if (!isGameActive) return;

        isGameActive = false;
        Debug.Log($"[GameManager] GAME OVER! Score: {score} | Survived: {GetFormattedTime()}");

        // Stop spawning
        if (zombieSpawner != null)
            zombieSpawner.StopSpawning();

        // Show game over UI
        if (gameOverCanvas != null)
            gameOverCanvas.SetActive(true);

        // Update HUD with final stats
        if (hudManager != null)
            hudManager.ShowGameOver(score, zombiesKilled, survivalTime);

        // Slow down time for dramatic effect
        Time.timeScale = 0.3f;

        OnGameOver?.Invoke();

        // Play game over sound
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayGameOver();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(survivalTime / 60f);
        int seconds = Mathf.FloorToInt(survivalTime % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
