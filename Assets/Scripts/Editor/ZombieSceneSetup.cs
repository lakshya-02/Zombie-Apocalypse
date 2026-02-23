#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor tool to auto-build the Zombie VR arena scene.
/// Use via menu: Zombie Game > Setup Scene
/// This creates all GameObjects, components, materials, prefabs, spawn points, and UI.
/// </summary>
public class ZombieSceneSetup : EditorWindow
{
    [MenuItem("Zombie Game/Setup Full Scene")]
    public static void SetupScene()
    {
        if (!EditorUtility.DisplayDialog("Setup Zombie VR Scene",
            "This will create the full game scene:\n\n" +
            "• Arena (ground + walls)\n" +
            "• 8 Spawn Points\n" +
            "• Manager objects (GameManager, ZombieSpawner, AudioManager)\n" +
            "• HUD Canvas (world-space)\n" +
            "• Game Over Canvas\n" +
            "• Zombie Prefab\n\n" +
            "NOTE: You must manually add OVRCameraRig from Meta SDK.\n" +
            "Proceed?", "Setup Scene", "Cancel"))
        {
            return;
        }

        CreateMaterials();
        CreateArena();
        CreateSpawnPoints();
        CreateManagers();
        CreateHUD();
        CreateZombiePrefab();
        CreateGunPrefab();

        Debug.Log("=== ZOMBIE VR SCENE SETUP COMPLETE ===\n" +
                  "Next steps:\n" +
                  "1. Add OVRCameraRig to scene (drag from Meta SDK prefabs)\n" +
                  "2. Drag Gun prefab under OVRCameraRig > TrackingSpace > RightHandAnchor\n" +
                  "3. Add PlayerHealth script to OVRCameraRig\n" +
                  "4. Drag Zombie prefab to ZombieSpawner's 'Zombie Prefab' slot\n" +
                  "5. Bake NavMesh on the Ground (select Ground > NavMeshSurface > Bake)\n" +
                  "6. Connect Quest 2 via Quest Link and press Play!");

        EditorUtility.DisplayDialog("Setup Complete!",
            "Scene created! Now do these manual steps:\n\n" +
            "1. Add OVRCameraRig to scene\n" +
            "   (Hierarchy > right-click > search 'OVRCameraRig')\n\n" +
            "2. Drag 'Gun' prefab under:\n" +
            "   OVRCameraRig > TrackingSpace > RightHandAnchor\n\n" +
            "3. Add 'PlayerHealth' component to OVRCameraRig\n\n" +
            "4. On ZombieSpawner: drag Zombie prefab to 'Zombie Prefab'\n\n" +
            "5. Select 'Ground' > click 'Bake' in NavMeshSurface\n\n" +
            "6. Play with Quest Link!",
            "Got it!");
    }

    [MenuItem("Zombie Game/Create Zombie Prefab Only")]
    public static void CreateZombiePrefabMenu()
    {
        CreateMaterials();
        CreateZombiePrefab();
        Debug.Log("Zombie prefab created at Assets/Prefabs/Zombie.prefab");
    }

    [MenuItem("Zombie Game/Create Gun Prefab Only")]
    public static void CreateGunPrefabMenu()
    {
        CreateMaterials();
        CreateGunPrefab();
        Debug.Log("Gun prefab created at Assets/Prefabs/Gun.prefab");
    }

    // ─── MATERIALS ───

    private static Material zombieMat;
    private static Material gunMat;
    private static Material groundMat;
    private static Material wallMat;

    static void CreateMaterials()
    {
        // Find URP shader
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
            urpLit = Shader.Find("Standard"); // Fallback

        // Zombie material - red
        zombieMat = CreateMaterial("ZombieMat", urpLit, new Color(0.7f, 0.15f, 0.1f, 1f));

        // Gun material - dark grey
        gunMat = CreateMaterial("GunMat", urpLit, new Color(0.15f, 0.15f, 0.15f, 1f));

        // Ground material - dark
        groundMat = CreateMaterial("GroundMat", urpLit, new Color(0.12f, 0.12f, 0.14f, 1f));

        // Wall material - slightly lighter
        wallMat = CreateMaterial("WallMat", urpLit, new Color(0.2f, 0.18f, 0.18f, 1f));
    }

    static Material CreateMaterial(string name, Shader shader, Color color)
    {
        string path = $"Assets/Materials/{name}.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Material mat = new Material(shader);
        mat.color = color;
        mat.SetFloat("_Smoothness", 0.3f);

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // ─── ARENA ───

    static void CreateArena()
    {
        // Clean up existing arena
        DestroyExisting("Arena");

        GameObject arena = new GameObject("Arena");

        // Ground plane (50x50 meters)
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(arena.transform);
        ground.transform.localPosition = Vector3.zero;
        ground.transform.localScale = new Vector3(5f, 1f, 5f); // Plane is 10x10 by default, *5 = 50x50
        ground.GetComponent<Renderer>().material = groundMat;
        ground.isStatic = true;

        // Add NavMeshSurface
        var navSurface = ground.AddComponent<NavMeshSurface>();
        navSurface.collectObjects = CollectObjects.Children;

        // Mark it to collect all in children scope — we'll fix this
        // Actually set it to collect "All" so it picks up the ground plane
        navSurface.collectObjects = CollectObjects.All;

        // Walls
        CreateWall("Wall_North", arena.transform, new Vector3(0, 1.5f, 25f), new Vector3(50f, 3f, 0.5f));
        CreateWall("Wall_South", arena.transform, new Vector3(0, 1.5f, -25f), new Vector3(50f, 3f, 0.5f));
        CreateWall("Wall_East", arena.transform, new Vector3(25f, 1.5f, 0), new Vector3(0.5f, 3f, 50f));
        CreateWall("Wall_West", arena.transform, new Vector3(-25f, 1.5f, 0), new Vector3(0.5f, 3f, 50f));

        Undo.RegisterCreatedObjectUndo(arena, "Create Arena");
    }

    static void CreateWall(string name, Transform parent, Vector3 position, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent);
        wall.transform.localPosition = position;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().material = wallMat;
        wall.isStatic = true;
    }

    // ─── SPAWN POINTS ───

    static void CreateSpawnPoints()
    {
        DestroyExisting("SpawnPoints");

        GameObject spawnParent = new GameObject("SpawnPoints");

        // 8 points in a circle at radius 20m
        float radius = 20f;
        for (int i = 0; i < 8; i++)
        {
            float angle = i * (360f / 8f) * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

            GameObject sp = new GameObject($"Spawn_{i + 1:00}");
            sp.transform.SetParent(spawnParent.transform);
            sp.transform.localPosition = pos;

            // Add a gizmo icon for visibility in editor
            // We'll add a small visual indicator
        }

        Undo.RegisterCreatedObjectUndo(spawnParent, "Create Spawn Points");
    }

    // ─── MANAGERS ───

    static void CreateManagers()
    {
        DestroyExisting("Managers");

        GameObject managers = new GameObject("Managers");

        // GameManager
        GameObject gmObj = new GameObject("GameManager");
        gmObj.transform.SetParent(managers.transform);
        var gm = gmObj.AddComponent<GameManager>();

        // ZombieSpawner
        GameObject zsObj = new GameObject("ZombieSpawner");
        zsObj.transform.SetParent(managers.transform);
        var zs = zsObj.AddComponent<ZombieSpawner>();

        // AudioManager
        GameObject amObj = new GameObject("AudioManager");
        amObj.transform.SetParent(managers.transform);
        var am = amObj.AddComponent<AudioManager>();

        // Wire up references
        gm.zombieSpawner = zs;

        Undo.RegisterCreatedObjectUndo(managers, "Create Managers");
    }

    // ─── HUD ───

    static void CreateHUD()
    {
        DestroyExisting("HUD");

        GameObject hudParent = new GameObject("HUD");

        // Create HUD Canvas
        GameObject hudCanvasObj = new GameObject("HUD_Canvas");
        hudCanvasObj.transform.SetParent(hudParent.transform);

        Canvas hudCanvas = hudCanvasObj.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.WorldSpace;
        hudCanvasObj.AddComponent<CanvasScaler>();
        hudCanvasObj.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = hudCanvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(800, 200);
        hudCanvasObj.transform.position = new Vector3(0f, 2.5f, 3f);
        hudCanvasObj.transform.localScale = Vector3.one * 0.002f;

        // Add HUDManager
        var hudManager = hudCanvasObj.AddComponent<HUDManager>();

        // Wire up to GameManager
        var gm = Object.FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            gm.hudManager = hudManager;
        }

        // Game Over Canvas (separate, disabled)
        GameObject goCanvasObj = new GameObject("GameOver_Canvas");
        goCanvasObj.transform.SetParent(hudParent.transform);

        Canvas goCanvas = goCanvasObj.AddComponent<Canvas>();
        goCanvas.renderMode = RenderMode.WorldSpace;
        goCanvasObj.AddComponent<CanvasScaler>();

        RectTransform goRect = goCanvasObj.GetComponent<RectTransform>();
        goRect.sizeDelta = new Vector2(800, 400);
        goCanvasObj.transform.position = new Vector3(0f, 1.5f, 2.5f);
        goCanvasObj.transform.localScale = Vector3.one * 0.002f;

        goCanvasObj.SetActive(false);

        // Wire game over canvas to GameManager
        if (gm != null)
        {
            gm.gameOverCanvas = goCanvasObj;
        }

        Undo.RegisterCreatedObjectUndo(hudParent, "Create HUD");
    }

    // ─── ZOMBIE PREFAB ───

    static void CreateZombiePrefab()
    {
        string prefabPath = "Assets/Prefabs/Zombie.prefab";

        // Create zombie in scene temporarily
        GameObject zombie = new GameObject("Zombie");

        // Body (capsule)
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(zombie.transform);
        body.transform.localPosition = new Vector3(0f, 1f, 0f);
        body.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
        body.GetComponent<Renderer>().material = zombieMat;

        // Head (sphere)
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(zombie.transform);
        head.transform.localPosition = new Vector3(0f, 2.0f, 0f);
        head.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        head.GetComponent<Renderer>().material = zombieMat;

        // Arms (capsules)
        GameObject leftArm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        leftArm.name = "LeftArm";
        leftArm.transform.SetParent(zombie.transform);
        leftArm.transform.localPosition = new Vector3(-0.45f, 1.3f, 0.3f);
        leftArm.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
        leftArm.transform.localScale = new Vector3(0.15f, 0.4f, 0.15f);
        leftArm.GetComponent<Renderer>().material = zombieMat;

        GameObject rightArm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        rightArm.name = "RightArm";
        rightArm.transform.SetParent(zombie.transform);
        rightArm.transform.localPosition = new Vector3(0.45f, 1.3f, 0.3f);
        rightArm.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
        rightArm.transform.localScale = new Vector3(0.15f, 0.4f, 0.15f);
        rightArm.GetComponent<Renderer>().material = zombieMat;

        // Remove individual colliders from children (we use one on root)
        foreach (var col in zombie.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(col);

        // Root components
        CapsuleCollider mainCollider = zombie.AddComponent<CapsuleCollider>();
        mainCollider.center = new Vector3(0f, 1f, 0f);
        mainCollider.radius = 0.4f;
        mainCollider.height = 2f;

        Rigidbody rb = zombie.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        NavMeshAgent agent = zombie.AddComponent<NavMeshAgent>();
        agent.speed = 2.5f;
        agent.acceleration = 4f;
        agent.stoppingDistance = 1.5f;
        agent.angularSpeed = 360f;
        agent.radius = 0.4f;
        agent.height = 2f;

        zombie.AddComponent<AudioSource>();
        zombie.AddComponent<ZombieController>();

        // Set tag
        CreateTagIfNeeded("Zombie");
        zombie.tag = "Zombie";

        // Save as prefab
        EnsureDirectory("Assets/Prefabs");
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(zombie, prefabPath);
        Object.DestroyImmediate(zombie);

        // Wire up to ZombieSpawner
        var spawner = Object.FindFirstObjectByType<ZombieSpawner>();
        if (spawner != null)
        {
            spawner.zombiePrefab = prefab;
            EditorUtility.SetDirty(spawner);
        }

        Debug.Log($"Zombie prefab saved to {prefabPath}");
    }

    // ─── GUN PREFAB ───

    static void CreateGunPrefab()
    {
        string prefabPath = "Assets/Prefabs/Gun.prefab";

        GameObject gun = new GameObject("Gun");

        // Gun body (elongated cube)
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "GunBody";
        body.transform.SetParent(gun.transform);
        body.transform.localPosition = new Vector3(0f, 0f, 0.12f);
        body.transform.localScale = new Vector3(0.04f, 0.06f, 0.25f);
        body.GetComponent<Renderer>().material = gunMat;

        // Gun grip
        GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        grip.name = "GunGrip";
        grip.transform.SetParent(gun.transform);
        grip.transform.localPosition = new Vector3(0f, -0.05f, 0.02f);
        grip.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
        grip.transform.localScale = new Vector3(0.035f, 0.08f, 0.04f);
        grip.GetComponent<Renderer>().material = gunMat;

        // Muzzle point (empty at barrel tip)
        GameObject muzzlePoint = new GameObject("MuzzlePoint");
        muzzlePoint.transform.SetParent(gun.transform);
        muzzlePoint.transform.localPosition = new Vector3(0f, 0f, 0.25f);

        // Muzzle flash (point light, disabled)
        GameObject muzzleFlash = new GameObject("MuzzleFlash");
        muzzleFlash.transform.SetParent(muzzlePoint.transform);
        muzzleFlash.transform.localPosition = Vector3.zero;
        Light flashLight = muzzleFlash.AddComponent<Light>();
        flashLight.type = LightType.Point;
        flashLight.color = new Color(1f, 0.8f, 0.3f);
        flashLight.intensity = 3f;
        flashLight.range = 5f;
        muzzleFlash.SetActive(false);

        // Remove colliders from gun parts (don't want to shoot yourself)
        foreach (var col in gun.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(col);

        // Tracer line renderer
        LineRenderer tracer = gun.AddComponent<LineRenderer>();
        tracer.startWidth = 0.01f;
        tracer.endWidth = 0.005f;
        tracer.positionCount = 2;
        tracer.material = new Material(Shader.Find("Sprites/Default"));
        tracer.startColor = Color.yellow;
        tracer.endColor = new Color(1f, 1f, 0f, 0.3f);
        tracer.enabled = false;

        // Gun controller
        var gc = gun.AddComponent<GunController>();
        gc.muzzlePoint = muzzlePoint.transform;
        gc.muzzleFlashEffect = muzzleFlash;
        gc.tracerLine = tracer;

        // Audio source
        gun.AddComponent<AudioSource>();

        // Save as prefab
        EnsureDirectory("Assets/Prefabs");
        PrefabUtility.SaveAsPrefabAsset(gun, prefabPath);
        Object.DestroyImmediate(gun);

        Debug.Log($"Gun prefab saved to {prefabPath}");
    }

    // ─── HELPERS ───

    static void DestroyExisting(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }
    }

    static void EnsureDirectory(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }

    static void CreateTagIfNeeded(string tag)
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tags = tagManager.FindProperty("tags");

        bool found = false;
        for (int i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == tag)
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            tagManager.ApplyModifiedProperties();
            Debug.Log($"Created tag: {tag}");
        }
    }
}
#endif
