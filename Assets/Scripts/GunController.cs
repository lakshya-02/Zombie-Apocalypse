using UnityEngine;

/// <summary>
/// Handles gun shooting via OVR right-hand trigger.
/// AIM MODE: Uses Camera (head) forward direction — look at the zombie to shoot it.
/// TRIGGER: Uses RawAxis1D.RIndexTrigger (unambiguous right hand trigger).
/// Attach to the Gun GameObject under RightHandAnchor of OVRCameraRig.
/// </summary>
public class GunController : MonoBehaviour
{
    [Header("Gun Settings")]
    public float fireRate = 0.2f;       // Seconds between shots
    public int damage = 1;
    public float maxRange = 100f;
    public LayerMask hitLayers = ~0;    // Everything by default

    [Header("Aim Mode")]
    [Tooltip("TRUE = shoot where you LOOK (camera/head forward). FALSE = shoot where gun barrel points.")]
    public bool aimWithHead = true;

    [Header("References")]
    public Transform muzzlePoint;       // Visual origin of tracer/flash
    public GameObject muzzleFlashEffect; // Light to enable briefly
    public LineRenderer tracerLine;      // Optional: visual tracer

    [Header("Audio")]
    public AudioClip gunshotClip;
    private AudioSource audioSource;

    [Header("Haptics")]
    public float hapticAmplitude = 0.4f;
    public float hapticDuration = 0.1f;

    // Runtime
    private float lastFireTime = 0f;
    private float muzzleFlashTimer = 0f;
    private float tracerTimer = 0f;
    private Camera vrCamera;
    private static readonly float MUZZLE_FLASH_DURATION = 0.05f;
    private static readonly float TRACER_DURATION = 0.08f;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        // Setup tracer line
        if (tracerLine != null)
        {
            tracerLine.enabled = false;
            tracerLine.startWidth = 0.01f;
            tracerLine.endWidth = 0.005f;
        }

        // Disable muzzle flash initially
        if (muzzleFlashEffect != null)
            muzzleFlashEffect.SetActive(false);
    }

    private void Start()
    {
        // Cache the VR camera
        vrCamera = Camera.main;

        // Auto-find muzzle point if not assigned
        if (muzzlePoint == null)
        {
            Transform mp = transform.Find("MuzzlePoint");
            muzzlePoint = mp != null ? mp : transform;
        }
    }

    private void Update()
    {
        // Allow shooting even if GameManager isn't ready yet, so you can test
        bool gameActive = GameManager.Instance == null || GameManager.Instance.isGameActive;
        if (!gameActive) return;

        // ── TRIGGER INPUT ──
        // RawAxis1D.RIndexTrigger = unambiguously the RIGHT index finger trigger
        // Keyboard fallback: hold Space bar for testing in editor without headset
        bool triggerHeld = OVRInput.Get(OVRInput.RawAxis1D.RIndexTrigger) > 0.5f
                        || Input.GetKey(KeyCode.Space)
                        || Input.GetMouseButton(0);   // Left click also works in editor

        if (triggerHeld && Time.time - lastFireTime >= fireRate)
        {
            Fire();
            lastFireTime = Time.time;
        }

        // Handle muzzle flash timer
        if (muzzleFlashTimer > 0f)
        {
            muzzleFlashTimer -= Time.deltaTime;
            if (muzzleFlashTimer <= 0f && muzzleFlashEffect != null)
                muzzleFlashEffect.SetActive(false);
        }

        // Handle tracer timer
        if (tracerTimer > 0f)
        {
            tracerTimer -= Time.deltaTime;
            if (tracerTimer <= 0f && tracerLine != null)
                tracerLine.enabled = false;
        }
    }

    private void Fire()
    {
        // ── DIRECTION ──
        // If aimWithHead: shoot from camera center toward wherever you're looking.
        // This is the most reliable for a VR showcase — look at a zombie, pull trigger, it dies.
        Vector3 origin;
        Vector3 direction;

        if (aimWithHead && vrCamera != null)
        {
            // Shoot from camera position in camera forward direction (look-to-aim)
            origin = vrCamera.transform.position;
            direction = vrCamera.transform.forward;
        }
        else
        {
            // Shoot from muzzle point along barrel forward
            origin = muzzlePoint.position;
            direction = muzzlePoint.forward;
        }

        // ── MUZZLE FLASH ──
        if (muzzleFlashEffect != null)
        {
            muzzleFlashEffect.SetActive(true);
            muzzleFlashTimer = MUZZLE_FLASH_DURATION;
        }

        // ── SOUND ──
        if (gunshotClip != null)
            audioSource.PlayOneShot(gunshotClip);
        else if (AudioManager.Instance != null)
            AudioManager.Instance.PlayGunshot(muzzlePoint.position);

        // ── HAPTICS ──
        OVRInput.SetControllerVibration(hapticAmplitude, hapticAmplitude, OVRInput.Controller.RTouch);
        Invoke(nameof(StopHaptics), hapticDuration);

        // ── RAYCAST ──
        Vector3 hitPoint = origin + direction * maxRange;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxRange, hitLayers))
        {
            hitPoint = hit.point;

            // Walk up the hierarchy to find ZombieController on hit object or any parent
            ZombieController zombie = hit.collider.GetComponentInParent<ZombieController>();

            if (zombie != null)
            {
                zombie.TakeDamage(damage);
                Debug.Log($"[Gun] ✓ Hit ZOMBIE '{zombie.name}' at {hit.distance:F1}m | HP left: {zombie.currentHealth - damage}");
            }
            else
            {
                Debug.Log($"[Gun] Hit '{hit.collider.name}' (tag: {hit.collider.tag}) — NOT a zombie");
            }
        }
        else
        {
            Debug.Log($"[Gun] Fired — no hit (direction: {direction})");
        }

        // ── TRACER LINE ──
        if (tracerLine != null)
        {
            tracerLine.enabled = true;
            tracerLine.SetPosition(0, muzzlePoint.position); // Always start from barrel visually
            tracerLine.SetPosition(1, hitPoint);
            tracerTimer = TRACER_DURATION;
        }

        // Scene view debug ray (visible in Unity editor Scene panel)
        Debug.DrawRay(origin, direction * maxRange, Color.red, 0.15f);
    }

    private void StopHaptics()
    {
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
    }
}
