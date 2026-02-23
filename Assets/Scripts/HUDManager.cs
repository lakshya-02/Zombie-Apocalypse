using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Updates the in-game HUD (score, timer, health bar) and game over screen.
/// Attach to the HUD World Space Canvas, or to any object (will find references).
/// </summary>
public class HUDManager : MonoBehaviour
{
    [Header("HUD Elements")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public Image healthBarFill;

    [Header("Game Over Elements")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverTitleText;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI finalKillsText;
    public TextMeshProUGUI survivalTimeText;
    public TextMeshProUGUI restartHintText;

    [Header("Health Bar Colors")]
    public Color healthFullColor = new Color(0.2f, 0.8f, 0.2f, 1f);   // Green
    public Color healthMidColor = new Color(0.9f, 0.9f, 0.1f, 1f);    // Yellow
    public Color healthLowColor = new Color(0.9f, 0.1f, 0.1f, 1f);    // Red

    [Header("HUD Canvas Settings")]
    public bool followPlayer = true;
    public Vector3 hudOffset = new Vector3(0f, 2.0f, 3f);  // Position relative to player
    public float hudScale = 0.002f;

    private Transform playerCamera;
    private Canvas hudCanvas;

    private void Start()
    {
        playerCamera = Camera.main != null ? Camera.main.transform : null;
        hudCanvas = GetComponent<Canvas>();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // Auto-create HUD if elements are not assigned
        if (scoreText == null || timerText == null || healthBarFill == null)
        {
            CreateHUDElements();
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        // Update score
        if (scoreText != null)
            scoreText.text = $"SCORE: {GameManager.Instance.score}";

        // Update timer
        if (timerText != null)
            timerText.text = GameManager.Instance.GetFormattedTime();

        // Update health bar
        if (healthBarFill != null)
        {
            PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
            if (ph != null)
            {
                float percent = ph.HealthPercent;
                healthBarFill.fillAmount = percent;

                // Color gradient: green → yellow → red
                if (percent > 0.5f)
                    healthBarFill.color = Color.Lerp(healthMidColor, healthFullColor, (percent - 0.5f) * 2f);
                else
                    healthBarFill.color = Color.Lerp(healthLowColor, healthMidColor, percent * 2f);
            }
        }

        // Make HUD face the player
        if (followPlayer && playerCamera != null)
        {
            // Position HUD in front of player
            Vector3 targetPos = playerCamera.position + playerCamera.forward * hudOffset.z + Vector3.up * hudOffset.y;
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 3f);

            // Face player
            transform.rotation = Quaternion.LookRotation(transform.position - playerCamera.position);
        }
    }

    /// <summary>
    /// Called by GameManager when game ends. Updates the game over UI.
    /// </summary>
    public void ShowGameOver(int finalScore, int kills, float time)
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);

        if (finalScoreText != null)
            finalScoreText.text = $"SCORE: {finalScore}";

        if (finalKillsText != null)
            finalKillsText.text = $"ZOMBIES KILLED: {kills}";

        if (survivalTimeText != null)
            survivalTimeText.text = $"SURVIVED: {minutes:00}:{seconds:00}";

        if (restartHintText != null)
            restartHintText.text = "PULL TRIGGER TO RESTART";

        // Hide the normal HUD
        if (scoreText != null) scoreText.gameObject.SetActive(false);
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (healthBarFill != null) healthBarFill.transform.parent.gameObject.SetActive(false);
    }

    /// <summary>
    /// Auto-creates HUD elements if none are assigned.
    /// This creates a world-space canvas with score, timer, and health bar.
    /// </summary>
    private void CreateHUDElements()
    {
        // Ensure this object has a Canvas component
        hudCanvas = GetComponent<Canvas>();
        if (hudCanvas == null)
        {
            hudCanvas = gameObject.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.WorldSpace;
        }

        // Set canvas size
        RectTransform canvasRect = GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(800, 200);
        transform.localScale = Vector3.one * hudScale;
        transform.position = hudOffset;

        // Add CanvasScaler
        if (GetComponent<CanvasScaler>() == null)
            gameObject.AddComponent<CanvasScaler>();

        // Score text (left side)
        GameObject scoreObj = CreateTMPText("ScoreText", "SCORE: 0",
            new Vector2(-200, 60), new Vector2(350, 60), TextAlignmentOptions.Left);
        scoreText = scoreObj.GetComponent<TextMeshProUGUI>();

        // Timer text (right side)
        GameObject timerObj = CreateTMPText("TimerText", "00:00",
            new Vector2(200, 60), new Vector2(350, 60), TextAlignmentOptions.Right);
        timerText = timerObj.GetComponent<TextMeshProUGUI>();

        // Health bar background
        GameObject healthBg = new GameObject("HealthBarBg");
        healthBg.transform.SetParent(transform, false);
        Image bgImage = healthBg.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        RectTransform bgRect = healthBg.GetComponent<RectTransform>();
        bgRect.anchoredPosition = new Vector2(0, -40);
        bgRect.sizeDelta = new Vector2(600, 30);

        // Health bar fill
        GameObject healthFg = new GameObject("HealthBarFill");
        healthFg.transform.SetParent(healthBg.transform, false);
        healthBarFill = healthFg.AddComponent<Image>();
        healthBarFill.color = healthFullColor;
        healthBarFill.type = Image.Type.Filled;
        healthBarFill.fillMethod = Image.FillMethod.Horizontal;
        healthBarFill.fillAmount = 1f;
        RectTransform fgRect = healthFg.GetComponent<RectTransform>();
        fgRect.anchorMin = Vector2.zero;
        fgRect.anchorMax = Vector2.one;
        fgRect.offsetMin = new Vector2(2, 2);
        fgRect.offsetMax = new Vector2(-2, -2);

        // Game Over panel (hidden initially)
        gameOverPanel = new GameObject("GameOverPanel");
        gameOverPanel.transform.SetParent(transform, false);

        Image panelBg = gameOverPanel.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.85f);
        RectTransform panelRect = gameOverPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        GameObject titleObj = CreateTMPText("GameOverTitle", "GAME OVER",
            new Vector2(0, 60), new Vector2(600, 70), TextAlignmentOptions.Center, gameOverPanel.transform);
        gameOverTitleText = titleObj.GetComponent<TextMeshProUGUI>();
        gameOverTitleText.fontSize = 60;
        gameOverTitleText.color = Color.red;

        GameObject fScoreObj = CreateTMPText("FinalScore", "SCORE: 0",
            new Vector2(0, 20), new Vector2(600, 40), TextAlignmentOptions.Center, gameOverPanel.transform);
        finalScoreText = fScoreObj.GetComponent<TextMeshProUGUI>();

        GameObject fKillsObj = CreateTMPText("FinalKills", "ZOMBIES KILLED: 0",
            new Vector2(0, -15), new Vector2(600, 40), TextAlignmentOptions.Center, gameOverPanel.transform);
        finalKillsText = fKillsObj.GetComponent<TextMeshProUGUI>();

        GameObject fTimeObj = CreateTMPText("SurvivalTime", "SURVIVED: 00:00",
            new Vector2(0, -50), new Vector2(600, 40), TextAlignmentOptions.Center, gameOverPanel.transform);
        survivalTimeText = fTimeObj.GetComponent<TextMeshProUGUI>();

        GameObject restartObj = CreateTMPText("RestartHint", "PULL TRIGGER TO RESTART",
            new Vector2(0, -85), new Vector2(600, 35), TextAlignmentOptions.Center, gameOverPanel.transform);
        restartHintText = restartObj.GetComponent<TextMeshProUGUI>();
        restartHintText.fontSize = 24;
        restartHintText.color = Color.yellow;

        gameOverPanel.SetActive(false);
    }

    private GameObject CreateTMPText(string name, string text, Vector2 position,
        Vector2 size, TextAlignmentOptions alignment, Transform parent = null)
    {
        if (parent == null) parent = transform;

        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 36;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        return obj;
    }
}
