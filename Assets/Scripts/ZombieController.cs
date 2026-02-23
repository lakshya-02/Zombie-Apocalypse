using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Controls zombie behavior: NavMesh movement toward player, health, damage, death.
/// Attach to the Zombie prefab along with NavMeshAgent, CapsuleCollider, Rigidbody(kinematic).
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class ZombieController : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 3;
    public int currentHealth;
    public int damageToPlayer = 20;
    public float attackRange = 1.8f;

    [Header("Jumpscare")]
    public bool isJumpscare = false;
    public float jumpscareSpeedMultiplier = 2f;

    [Header("Visual Feedback")]
    public Renderer zombieRenderer;
    private Color originalColor;
    private float flashTimer = 0f;
    private static readonly float FLASH_DURATION = 0.15f;

    [Header("Audio")]
    public AudioClip growlClip;
    public AudioClip deathClip;
    public AudioClip jumpscareScreamClip;

    private NavMeshAgent agent;
    private Transform playerTarget;
    private float updateDestinationInterval = 0.5f;
    private float destinationTimer;
    private bool isDead = false;
    private AudioSource audioSource;

    // Base speed from NavMeshAgent (set in inspector/prefab)
    private float baseSpeed;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.spatialBlend = 1f; // Full 3D audio
        audioSource.minDistance = 2f;
        audioSource.maxDistance = 30f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
    }

    private void Start()
    {
        currentHealth = maxHealth;
        baseSpeed = agent.speed;

        // Find the player (OVRCameraRig's CenterEyeAnchor or main camera)
        if (Camera.main != null)
            playerTarget = Camera.main.transform;
        else
        {
            var cameraRig = FindFirstObjectByType<OVRCameraRig>();
            if (cameraRig != null)
                playerTarget = cameraRig.centerEyeAnchor;
        }

        // Get renderer for flash effect
        if (zombieRenderer == null)
            zombieRenderer = GetComponentInChildren<Renderer>();
        if (zombieRenderer != null)
            originalColor = zombieRenderer.material.color;

        // Apply jumpscare modifiers
        if (isJumpscare)
        {
            agent.speed = baseSpeed * jumpscareSpeedMultiplier;

            // Play jumpscare scream
            if (jumpscareScreamClip != null)
                audioSource.PlayOneShot(jumpscareScreamClip);
            else if (AudioManager.Instance != null)
                AudioManager.Instance.PlayJumpscareScream(transform.position);
        }
        else
        {
            // Play ambient growl (looping)
            if (growlClip != null)
            {
                audioSource.clip = growlClip;
                audioSource.loop = true;
                audioSource.volume = 0.4f;
                audioSource.Play();
            }
        }

        // Initial destination
        if (playerTarget != null)
            agent.SetDestination(playerTarget.position);

        destinationTimer = updateDestinationInterval;
    }

    private void Update()
    {
        if (isDead || !GameManager.Instance.isGameActive) return;

        // Update destination periodically (not every frame for performance)
        destinationTimer -= Time.deltaTime;
        if (destinationTimer <= 0f && playerTarget != null)
        {
            agent.SetDestination(playerTarget.position);
            destinationTimer = updateDestinationInterval;
        }

        // Check if close enough to attack player
        if (playerTarget != null)
        {
            float distToPlayer = Vector3.Distance(transform.position, playerTarget.position);
            if (distToPlayer <= attackRange)
            {
                AttackPlayer();
            }
        }

        // Handle hit flash
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f && zombieRenderer != null)
            {
                zombieRenderer.material.color = originalColor;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        // Visual flash white
        if (zombieRenderer != null)
        {
            zombieRenderer.material.color = Color.white;
            flashTimer = FLASH_DURATION;
        }

        // Hit sound
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayZombieHit(transform.position);

        Debug.Log($"[Zombie] Hit! HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("[Zombie] Died!");

        // Award points
        if (GameManager.Instance != null)
            GameManager.Instance.AddKill(isJumpscare);

        // Death sound
        if (deathClip != null)
            AudioSource.PlayClipAtPoint(deathClip, transform.position);
        else if (AudioManager.Instance != null)
            AudioManager.Instance.PlayZombieDeath(transform.position);

        // Stop moving
        agent.isStopped = true;
        agent.enabled = false;

        // Death effect: shrink and sink
        StartCoroutine(DeathAnimation());
    }

    private System.Collections.IEnumerator DeathAnimation()
    {
        float elapsed = 0f;
        float duration = 0.8f;
        Vector3 startScale = transform.localScale;
        Vector3 startPos = transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Shrink
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            // Sink into ground
            transform.position = new Vector3(
                startPos.x,
                Mathf.Lerp(startPos.y, startPos.y - 1f, t),
                startPos.z
            );

            yield return null;
        }

        Destroy(gameObject);
    }

    private void AttackPlayer()
    {
        if (isDead) return;

        // Damage the player
        var playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damageToPlayer);
        }

        // Zombie dies after attacking (suicide attack)
        isDead = true;
        agent.isStopped = true;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayZombieAttack(transform.position);

        Destroy(gameObject, 0.2f);
    }

    /// <summary>
    /// Set zombie speed externally (used by ZombieSpawner for difficulty scaling).
    /// </summary>
    public void SetSpeed(float speed)
    {
        baseSpeed = speed;
        if (agent != null)
        {
            agent.speed = isJumpscare ? speed * jumpscareSpeedMultiplier : speed;
        }
    }

    private void OnDestroy()
    {
        // Notify spawner that this zombie is gone
        if (ZombieSpawner.Instance != null)
            ZombieSpawner.Instance.OnZombieDestroyed();
    }
}
