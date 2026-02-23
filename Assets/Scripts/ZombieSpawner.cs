using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Spawns zombies with increasing difficulty and periodic jumpscares.
/// Attach to an empty GameObject. Assign zombie prefab and spawn point transforms in Inspector.
/// </summary>
public class ZombieSpawner : MonoBehaviour
{
    public static ZombieSpawner Instance { get; private set; }

    [Header("References")]
    public GameObject zombiePrefab;
    public Transform[] spawnPoints;
    public Transform playerTransform;

    [Header("Spawning")]
    public float initialSpawnInterval = 4f;
    public float minimumSpawnInterval = 1.0f;
    public int initialMaxZombies = 3;
    public int absoluteMaxZombies = 20;

    [Header("Difficulty Scaling")]
    public float spawnIntervalDecreaseRate = 0.05f; // decrease per second
    public float maxZombieIncreaseInterval = 30f;   // add 1 max zombie every X seconds
    public float speedIncreaseRate = 0.02f;         // speed increase per second

    [Header("Zombie Stats")]
    public float baseZombieSpeed = 2.0f;
    public float maxZombieSpeed = 5.0f;

    [Header("Jumpscare Settings")]
    public float jumpscareMinTime = 45f;            // earliest jumpscare
    public float jumpscareMaxTime = 60f;            // latest jumpscare (first interval)
    public float jumpscareMinDistance = 4f;          // how close jumpscare zombie spawns
    public float jumpscareMaxDistance = 7f;
    public float jumpscareIntervalDecrease = 5f;    // interval gets shorter over time

    // Runtime state
    private float currentSpawnInterval;
    private int currentMaxZombies;
    private float currentZombieSpeed;
    private int activeZombieCount = 0;
    private bool isSpawning = false;
    private float gameTime = 0f;
    private float nextMaxZombieIncrease;
    private float nextJumpscareTime;
    private Coroutine spawnCoroutine;

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
        // Find player if not assigned
        if (playerTransform == null)
        {
            if (Camera.main != null)
                playerTransform = Camera.main.transform;
        }

        // Auto-find spawn points if not assigned
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            var spawnParent = GameObject.Find("SpawnPoints");
            if (spawnParent != null)
            {
                var points = new List<Transform>();
                foreach (Transform child in spawnParent.transform)
                    points.Add(child);
                spawnPoints = points.ToArray();
            }
        }

        // Subscribe to game events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStart += BeginSpawning;
            GameManager.Instance.OnGameOver += StopSpawning;
        }

        BeginSpawning();
    }

    public void BeginSpawning()
    {
        // Reset difficulty
        currentSpawnInterval = initialSpawnInterval;
        currentMaxZombies = initialMaxZombies;
        currentZombieSpeed = baseZombieSpeed;
        activeZombieCount = 0;
        gameTime = 0f;
        nextMaxZombieIncrease = maxZombieIncreaseInterval;
        nextJumpscareTime = Random.Range(jumpscareMinTime, jumpscareMaxTime);

        isSpawning = true;

        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);
        spawnCoroutine = StartCoroutine(SpawnLoop());

        Debug.Log("[ZombieSpawner] Spawning started!");
    }

    public void StopSpawning()
    {
        isSpawning = false;
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        Debug.Log("[ZombieSpawner] Spawning stopped.");
    }

    private void Update()
    {
        if (!isSpawning || GameManager.Instance == null || !GameManager.Instance.isGameActive)
            return;

        gameTime += Time.deltaTime;

        // Gradually increase difficulty
        currentSpawnInterval = Mathf.Max(
            minimumSpawnInterval,
            initialSpawnInterval - (gameTime * spawnIntervalDecreaseRate)
        );

        currentZombieSpeed = Mathf.Min(
            maxZombieSpeed,
            baseZombieSpeed + (gameTime * speedIncreaseRate)
        );

        // Increase max zombies periodically
        if (gameTime >= nextMaxZombieIncrease)
        {
            currentMaxZombies = Mathf.Min(absoluteMaxZombies, currentMaxZombies + 1);
            nextMaxZombieIncrease += maxZombieIncreaseInterval;
            Debug.Log($"[ZombieSpawner] Max zombies increased to {currentMaxZombies}");
        }

        // Check for jumpscare
        if (gameTime >= nextJumpscareTime)
        {
            SpawnJumpscareZombie();
            // Schedule next jumpscare (interval decreases over time)
            float interval = Mathf.Max(20f,
                Random.Range(jumpscareMinTime, jumpscareMaxTime) - (gameTime * 0.1f));
            nextJumpscareTime = gameTime + interval;
        }
    }

    private IEnumerator SpawnLoop()
    {
        // Small initial delay
        yield return new WaitForSeconds(2f);

        while (isSpawning)
        {
            if (GameManager.Instance != null && GameManager.Instance.isGameActive)
            {
                if (activeZombieCount < currentMaxZombies && zombiePrefab != null && spawnPoints.Length > 0)
                {
                    SpawnZombie();
                }
            }

            yield return new WaitForSeconds(currentSpawnInterval);
        }
    }

    private void SpawnZombie()
    {
        // Pick random spawn point
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // Random offset so they don't all spawn at the exact same spot
        Vector3 offset = new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
        Vector3 spawnPos = spawnPoint.position + offset;
        spawnPos.y = 0f; // Ensure on ground

        GameObject zombie = Instantiate(zombiePrefab, spawnPos, Quaternion.identity);

        ZombieController controller = zombie.GetComponent<ZombieController>();
        if (controller != null)
        {
            controller.SetSpeed(currentZombieSpeed);
            controller.isJumpscare = false;
        }

        activeZombieCount++;
        Debug.Log($"[ZombieSpawner] Spawned zombie. Active: {activeZombieCount}/{currentMaxZombies}");
    }

    private void SpawnJumpscareZombie()
    {
        if (playerTransform == null) return;

        Debug.Log("[ZombieSpawner] *** JUMPSCARE! ***");

        // Spawn close to player
        float distance = Random.Range(jumpscareMinDistance, jumpscareMaxDistance);
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

        Vector3 spawnPos = playerTransform.position + new Vector3(
            Mathf.Cos(angle) * distance,
            0f,
            Mathf.Sin(angle) * distance
        );
        spawnPos.y = 0f;

        GameObject zombie = Instantiate(zombiePrefab, spawnPos, Quaternion.identity);

        ZombieController controller = zombie.GetComponent<ZombieController>();
        if (controller != null)
        {
            controller.isJumpscare = true;
            controller.SetSpeed(currentZombieSpeed);
            controller.maxHealth = 2; // Dies faster (more fair since it's close)
        }

        activeZombieCount++;

        // Screen flash via AudioManager
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayJumpscareScream(spawnPos);
    }

    /// <summary>
    /// Called by ZombieController.OnDestroy to track active count.
    /// </summary>
    public void OnZombieDestroyed()
    {
        activeZombieCount = Mathf.Max(0, activeZombieCount - 1);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStart -= BeginSpawning;
            GameManager.Instance.OnGameOver -= StopSpawning;
        }
    }
}
