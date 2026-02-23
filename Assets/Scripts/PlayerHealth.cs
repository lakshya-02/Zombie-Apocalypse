using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Manages player health, damage effects (red vignette), and triggers game over.
/// Attach to the OVRCameraRig or a child GameObject.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Damage Vignette")]
    [Tooltip("A red UI Image covering the screen, parented to CenterEyeAnchor")]
    public Image damageVignette;
    public float vignetteMaxAlpha = 0.5f;
    public float vignetteFadeDuration = 0.3f;

    [Header("Low Health Warning")]
    public float lowHealthThreshold = 0.3f;  // 30% HP
    public float lowHealthPulseSpeed = 2f;

    [Header("Audio")]
    public AudioClip hurtClip;
    private AudioSource audioSource;

    // Runtime
    private bool isFlashing = false;
    private Coroutine vignetteCoroutine;

    public float HealthPercent => (float)currentHealth / maxHealth;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound for player
    }

    private void Start()
    {
        currentHealth = maxHealth;

        // Set vignette to transparent
        if (damageVignette != null)
        {
            Color c = damageVignette.color;
            c.a = 0f;
            damageVignette.color = c;
        }

        // Auto-create damage vignette if not assigned
        if (damageVignette == null)
        {
            CreateDamageVignette();
        }
    }

    private void Update()
    {
        // Low health pulsing effect
        if (damageVignette != null && !isFlashing && currentHealth > 0)
        {
            if (HealthPercent <= lowHealthThreshold)
            {
                float pulse = Mathf.Abs(Mathf.Sin(Time.time * lowHealthPulseSpeed));
                float alpha = Mathf.Lerp(0f, vignetteMaxAlpha * 0.3f, pulse);
                Color c = damageVignette.color;
                c.a = alpha;
                damageVignette.color = c;
            }
        }
    }

    public void TakeDamage(int amount)
    {
        if (currentHealth <= 0) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);

        Debug.Log($"[PlayerHealth] Took {amount} damage! HP: {currentHealth}/{maxHealth}");

        // Play hurt sound
        if (hurtClip != null)
            audioSource.PlayOneShot(hurtClip);
        else if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPlayerHurt();

        // Haptic feedback on both controllers
        OVRInput.SetControllerVibration(0.6f, 0.6f, OVRInput.Controller.LTouch);
        OVRInput.SetControllerVibration(0.6f, 0.6f, OVRInput.Controller.RTouch);
        Invoke(nameof(StopHaptics), 0.2f);

        // Red flash effect
        if (vignetteCoroutine != null)
            StopCoroutine(vignetteCoroutine);
        vignetteCoroutine = StartCoroutine(DamageFlash());

        // Check death
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("[PlayerHealth] Player died!");

        if (GameManager.Instance != null)
            GameManager.Instance.GameOver();
    }

    private IEnumerator DamageFlash()
    {
        if (damageVignette == null) yield break;

        isFlashing = true;

        // Flash to max red
        float elapsed = 0f;
        float halfDuration = vignetteFadeDuration * 0.5f;

        // Fade in
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime; // unscaled so it works even in slow-mo
            float t = elapsed / halfDuration;
            Color c = damageVignette.color;
            c.a = Mathf.Lerp(0f, vignetteMaxAlpha, t);
            damageVignette.color = c;
            yield return null;
        }

        // Fade out
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / halfDuration;
            Color c = damageVignette.color;
            c.a = Mathf.Lerp(vignetteMaxAlpha, 0f, t);
            damageVignette.color = c;
            yield return null;
        }

        // Ensure fully transparent
        Color final_c = damageVignette.color;
        final_c.a = 0f;
        damageVignette.color = final_c;

        isFlashing = false;
    }

    private void StopHaptics()
    {
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
    }

    /// <summary>
    /// Auto-creates a damage vignette canvas parented to the camera.
    /// </summary>
    private void CreateDamageVignette()
    {
        Transform cameraTransform = Camera.main != null ? Camera.main.transform : transform;

        // Create Canvas
        GameObject canvasObj = new GameObject("DamageVignetteCanvas");
        canvasObj.transform.SetParent(cameraTransform, false);
        canvasObj.transform.localPosition = new Vector3(0f, 0f, 0.5f); // 0.5m in front
        canvasObj.transform.localRotation = Quaternion.identity;
        canvasObj.transform.localScale = Vector3.one * 0.001f;

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1200, 800);

        // Create Image
        GameObject imageObj = new GameObject("VignetteImage");
        imageObj.transform.SetParent(canvasObj.transform, false);

        damageVignette = imageObj.AddComponent<Image>();
        damageVignette.color = new Color(0.8f, 0f, 0f, 0f); // Red, transparent
        damageVignette.raycastTarget = false;

        RectTransform imgRect = imageObj.GetComponent<RectTransform>();
        imgRect.anchorMin = Vector2.zero;
        imgRect.anchorMax = Vector2.one;
        imgRect.offsetMin = Vector2.zero;
        imgRect.offsetMax = Vector2.zero;
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }
}
