using UnityEngine;

/// <summary>
/// Handles gun shooting via OVR right-hand trigger.
/// Raycasts from MuzzlePoint, damages ZombieController on hit.
/// Attach to the Gun GameObject under RightHandAnchor of OVRCameraRig.
/// </summary>
public class GunController : MonoBehaviour
{
    [Header("Gun Settings")]
    public float fireRate = 0.2f;       // Seconds between shots
    public int damage = 1;
    public float maxRange = 100f;
    public LayerMask hitLayers = ~0;    // Everything by default

    [Header("References")]
    public Transform muzzlePoint;       // Where raycast originates + muzzle flash
    public GameObject muzzleFlashEffect; // Particle or light to enable briefly
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
    private static readonly float MUZZLE_FLASH_DURATION = 0.05f;
    private static readonly float TRACER_DURATION = 0.05f;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        // Setup tracer line if exists
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
        // Auto-find muzzle point if not assigned
        if (muzzlePoint == null)
        {
            var mp = transform.Find("MuzzlePoint");
            if (mp != null) muzzlePoint = mp;
            else muzzlePoint = transform; // Fallback to gun itself
        }
    }

    private void Update()
    {
        // Don't shoot if game is not active
        if (GameManager.Instance == null || !GameManager.Instance.isGameActive) return;

        // Check right index trigger
        bool triggerPressed = OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger) > 0.5f;

        if (triggerPressed && Time.time - lastFireTime >= fireRate)
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
        Vector3 origin = muzzlePoint.position;
        Vector3 direction = muzzlePoint.forward;

        // Muzzle flash
        if (muzzleFlashEffect != null)
        {
            muzzleFlashEffect.SetActive(true);
            muzzleFlashTimer = MUZZLE_FLASH_DURATION;
        }

        // Gunshot sound
        if (gunshotClip != null)
            audioSource.PlayOneShot(gunshotClip);
        else if (AudioManager.Instance != null)
            AudioManager.Instance.PlayGunshot(origin);

        // Haptic feedback on right controller
        OVRInput.SetControllerVibration(hapticAmplitude, hapticAmplitude, OVRInput.Controller.RTouch);
        Invoke(nameof(StopHaptics), hapticDuration);

        // Raycast
        Vector3 hitPoint = origin + direction * maxRange; // Default end point for tracer

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxRange, hitLayers))
        {
            hitPoint = hit.point;

            // Check if we hit a zombie
            ZombieController zombie = hit.collider.GetComponent<ZombieController>();
            if (zombie == null)
                zombie = hit.collider.GetComponentInParent<ZombieController>();

            if (zombie != null)
            {
                zombie.TakeDamage(damage);
                Debug.Log($"[Gun] Hit zombie at {hit.distance:F1}m");
            }
            else
            {
                Debug.Log($"[Gun] Hit {hit.collider.name} at {hit.distance:F1}m");
            }
        }

        // Visual tracer
        if (tracerLine != null)
        {
            tracerLine.enabled = true;
            tracerLine.SetPosition(0, origin);
            tracerLine.SetPosition(1, hitPoint);
            tracerTimer = TRACER_DURATION;
        }

        // Debug visualization in editor
        Debug.DrawRay(origin, direction * maxRange, Color.red, 0.1f);
    }

    private void StopHaptics()
    {
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
    }
}
