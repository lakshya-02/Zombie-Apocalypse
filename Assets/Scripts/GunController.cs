using UnityEngine;

/// <summary>
/// Gun shooting controller.
/// Attach this script to a GameObject that is a CHILD of RightControllerAnchor.
/// 
/// OVRCameraRig hierarchy:
///   OVRCameraRig
///   └── TrackingSpace
///       └── RightControllerAnchor   <-- parent your Gun here (NOT RightHandAnchor)
///           └── Gun                 <-- this script lives here
/// 
/// Trigger detection uses OVRInput.RawButton.RHandTrigger + RIndexTrigger.
/// Aiming uses Camera (head) forward — look at zombie and pull trigger.
/// </summary>
public class GunController : MonoBehaviour
{
    [Header("Gun Settings")]
    public float fireRate = 0.25f;
    public int damage = 1;
    public float maxRange = 150f;

    [Header("Aim Mode")]
    [Tooltip("Shoot where you LOOK. Recommended for a stationary VR showcase.")]
    public bool aimWithHead = true;

    [Header("References")]
    public Transform muzzlePoint;
    public GameObject muzzleFlashEffect;
    public LineRenderer tracerLine;

    [Header("Haptics")]
    public float hapticAmplitude = 0.5f;
    public float hapticDuration = 0.1f;

    // Runtime
    private float lastFireTime;
    private float muzzleFlashTimer;
    private float tracerTimer;
    private Camera vrCamera;
    private AudioSource audioSource;

    private void Start()
    {
        vrCamera = Camera.main;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Auto-find MuzzlePoint child
        if (muzzlePoint == null)
        {
            Transform mp = transform.Find("MuzzlePoint");
            muzzlePoint = (mp != null) ? mp : transform;
        }

        if (muzzleFlashEffect != null)
            muzzleFlashEffect.SetActive(false);

        if (tracerLine != null)
            tracerLine.enabled = false;

        Debug.Log("[Gun] GunController initialized. Parent: " + transform.parent?.name);
    }

    private void Update()
    {
        // ── TRIGGER DETECTION ──
        // We check THREE different ways to catch the trigger no matter what
        // OVR controller handedness / profile is active.

        bool ovrTrigger =
            OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger) ||   // right index trigger press
            OVRInput.Get(OVRInput.RawAxis1D.RIndexTrigger) > 0.7f;  // right index trigger held

        // Editor / keyboard fallback (for testing without headset)
        bool editorTrigger = Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space);

        bool wantFire = (ovrTrigger || editorTrigger) && (Time.time - lastFireTime >= fireRate);

        if (wantFire)
        {
            Fire();
            lastFireTime = Time.time;
        }

        // Muzzle flash off timer
        if (muzzleFlashTimer > 0f)
        {
            muzzleFlashTimer -= Time.deltaTime;
            if (muzzleFlashTimer <= 0f && muzzleFlashEffect != null)
                muzzleFlashEffect.SetActive(false);
        }

        // Tracer off timer
        if (tracerTimer > 0f)
        {
            tracerTimer -= Time.deltaTime;
            if (tracerTimer <= 0f && tracerLine != null)
                tracerLine.enabled = false;
        }
    }

    private void Fire()
    {
        // ── DETERMINE RAY ORIGIN & DIRECTION ──
        Vector3 origin, direction;

        if (aimWithHead && vrCamera != null)
        {
            // Shoot from camera center in look direction (most reliable for VR showcase)
            origin    = vrCamera.transform.position;
            direction = vrCamera.transform.forward;
        }
        else
        {
            origin    = muzzlePoint.position;
            direction = muzzlePoint.forward;
        }

        // ── MUZZLE FLASH ──
        if (muzzleFlashEffect != null)
        {
            muzzleFlashEffect.SetActive(true);
            muzzleFlashTimer = 0.06f;
        }

        // ── HAPTICS ──
        OVRInput.SetControllerVibration(hapticAmplitude, hapticAmplitude, OVRInput.Controller.RTouch);
        Invoke(nameof(StopHaptics), hapticDuration);

        // ── SOUND ──
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayGunshot(muzzlePoint.position);

        // ── RAYCAST ──
        Vector3 hitPoint = origin + direction * maxRange;

        // Ignore the gun itself by excluding its layer — cast against everything else
        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxRange))
        {
            hitPoint = hit.point;

            ZombieController zombie = hit.collider.GetComponentInParent<ZombieController>();
            if (zombie != null)
            {
                zombie.TakeDamage(damage);
                Debug.Log($"[Gun] HIT zombie '{zombie.name}' at {hit.distance:F1}m");
            }
            else
            {
                Debug.Log($"[Gun] Hit '{hit.collider.name}' — not a zombie");
            }
        }
        else
        {
            Debug.Log("[Gun] Shot fired — no collision hit");
        }

        // ── TRACER ──
        if (tracerLine != null)
        {
            tracerLine.SetPosition(0, muzzlePoint.position);
            tracerLine.SetPosition(1, hitPoint);
            tracerLine.enabled = true;
            tracerTimer = 0.08f;
        }

        Debug.DrawRay(origin, direction * maxRange, Color.red, 0.2f);
    }

    private void StopHaptics()
    {
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
    }
}
