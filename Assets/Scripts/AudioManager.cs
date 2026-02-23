using UnityEngine;

/// <summary>
/// Singleton audio manager for all game sounds.
/// Plays spatial 3D audio for zombie/gun sounds and 2D audio for UI/ambient.
/// Attach to an empty "AudioManager" GameObject.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Clips")]
    public AudioClip gunshotClip;
    public AudioClip zombieGrowlClip;
    public AudioClip zombieHitClip;
    public AudioClip zombieDeathClip;
    public AudioClip zombieAttackClip;
    public AudioClip jumpscareScreamClip;
    public AudioClip playerHurtClip;
    public AudioClip gameOverClip;
    public AudioClip ambientLoopClip;
    public AudioClip killStreakClip;

    [Header("Volume Settings")]
    public float masterVolume = 1f;
    public float sfxVolume = 0.8f;
    public float ambientVolume = 0.3f;

    [Header("Audio Sources")]
    public AudioSource ambientSource;
    public AudioSource uiSource;         // For 2D non-positional sounds

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Setup ambient audio source
        if (ambientSource == null)
        {
            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.loop = true;
            ambientSource.spatialBlend = 0f; // 2D
            ambientSource.volume = ambientVolume;
            ambientSource.playOnAwake = false;
        }

        // Setup UI audio source
        if (uiSource == null)
        {
            uiSource = gameObject.AddComponent<AudioSource>();
            uiSource.loop = false;
            uiSource.spatialBlend = 0f; // 2D
            uiSource.volume = sfxVolume;
            uiSource.playOnAwake = false;
        }
    }

    private void Start()
    {
        // Start ambient sound
        if (ambientLoopClip != null)
        {
            ambientSource.clip = ambientLoopClip;
            ambientSource.volume = ambientVolume;
            ambientSource.Play();
        }
    }

    /// <summary>
    /// Plays a sound at a 3D world position (one-shot, auto-cleanup).
    /// </summary>
    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;

        GameObject sfxObj = new GameObject("SFX_OneShot");
        sfxObj.transform.position = position;

        AudioSource source = sfxObj.AddComponent<AudioSource>();
        source.clip = clip;
        source.spatialBlend = 1f;    // Full 3D
        source.minDistance = 2f;
        source.maxDistance = 40f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.volume = volume * sfxVolume * masterVolume;
        source.Play();

        Destroy(sfxObj, clip.length + 0.1f);
    }

    /// <summary>
    /// Plays a 2D (non-positional) sound.
    /// </summary>
    public void PlaySFX2D(AudioClip clip, float volume = 1f)
    {
        if (clip == null || uiSource == null) return;
        uiSource.PlayOneShot(clip, volume * sfxVolume * masterVolume);
    }

    // ── Convenience Methods ──

    public void PlayGunshot(Vector3 position)
    {
        if (gunshotClip != null)
            PlaySFXAtPosition(gunshotClip, position, 1f);
        else
            Debug.Log("[Audio] Gunshot (no clip assigned)");
    }

    public void PlayZombieHit(Vector3 position)
    {
        if (zombieHitClip != null)
            PlaySFXAtPosition(zombieHitClip, position, 0.7f);
    }

    public void PlayZombieDeath(Vector3 position)
    {
        if (zombieDeathClip != null)
            PlaySFXAtPosition(zombieDeathClip, position, 0.9f);
    }

    public void PlayZombieAttack(Vector3 position)
    {
        if (zombieAttackClip != null)
            PlaySFXAtPosition(zombieAttackClip, position, 1f);
    }

    public void PlayJumpscareScream(Vector3 position)
    {
        if (jumpscareScreamClip != null)
            PlaySFXAtPosition(jumpscareScreamClip, position, 1.2f);
        else
            Debug.Log("[Audio] JUMPSCARE SCREAM! (no clip assigned)");
    }

    public void PlayPlayerHurt()
    {
        if (playerHurtClip != null)
            PlaySFX2D(playerHurtClip, 1f);
    }

    public void PlayGameOver()
    {
        if (gameOverClip != null)
            PlaySFX2D(gameOverClip, 1f);
    }

    public void PlayKillStreak()
    {
        if (killStreakClip != null)
            PlaySFX2D(killStreakClip, 1f);
    }

    /// <summary>
    /// Set ambient volume at runtime.
    /// </summary>
    public void SetAmbientVolume(float volume)
    {
        ambientVolume = volume;
        if (ambientSource != null)
            ambientSource.volume = volume * masterVolume;
    }

    /// <summary>
    /// Set master volume at runtime.
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = volume;
        if (ambientSource != null)
            ambientSource.volume = ambientVolume * masterVolume;
    }
}
