using UnityEngine;

/// <summary>
/// Single entry point for the Bus Jam scene.
/// Runs before every other MonoBehaviour (execution order -1000) so managers are
/// initialized in a known order instead of relying on Unity's Start() order.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class GameBootstrap : MonoBehaviour
{
    public static GameBootstrap Instance { get; private set; }

    /// <summary>
    /// True while this bootstrap owns initialization. Managers use it to skip their
    /// own Start() fallback so nothing gets initialized twice.
    /// </summary>
    public static bool IsDriving => Instance != null && Instance.isActiveAndEnabled;

    [Header("Scene References (wired by Tools > Bus Jam > Setup Scene)")]
    [SerializeField] private SettingsManager settingsManager;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private BusStopManager busStopManager;
    [SerializeField] private PassengerQueueManager passengerQueueManager;
    [SerializeField] private LevelLoader levelLoader;
    [SerializeField] private GamePoolManager poolManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private HUDController hudController;
    [SerializeField] private VFXManager vfxManager;
    [SerializeField] private CameraShakeManager cameraShakeManager;

    private bool _initialized;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        ResolveReferences();
    }

    private void Start()
    {
        RunInitialization();
    }

    /// <summary>
    /// Fills any missing serialized reference from the scene. Safe to call multiple times.
    /// </summary>
    public void ResolveReferences()
    {
        if (settingsManager == null) settingsManager = FindObjectOfType<SettingsManager>();
        if (audioManager == null) audioManager = FindObjectOfType<AudioManager>();
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (busStopManager == null) busStopManager = FindObjectOfType<BusStopManager>();
        if (passengerQueueManager == null) passengerQueueManager = FindObjectOfType<PassengerQueueManager>();
        if (levelLoader == null) levelLoader = FindObjectOfType<LevelLoader>();
        if (poolManager == null) poolManager = FindObjectOfType<GamePoolManager>();
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if (uiManager == null) uiManager = FindObjectOfType<UIManager>();
        if (hudController == null) hudController = FindObjectOfType<HUDController>();
        if (vfxManager == null) vfxManager = FindObjectOfType<VFXManager>();
        if (cameraShakeManager == null) cameraShakeManager = FindObjectOfType<CameraShakeManager>();
    }

    /// <summary>
    /// Initializes every manager in a deterministic order and then starts the level.
    /// </summary>
    public void RunInitialization()
    {
        if (_initialized) return;
        _initialized = true;

        EnsureCanvases();
        ResolveReferences();

        if (gameManager == null)
        {
            Debug.LogError("[GameBootstrap] GameManager not found in the scene. " +
                           "Add a GameManager or run Tools > Bus Jam > Setup Scene.");
            return;
        }

        // Settings and Audio first
        settingsManager?.Initialize();
        audioManager?.Initialize();

        // Pools first: level loading requests buses/passengers from them.
        poolManager?.Initialize();

        // Queue subscribes to bus-stop events and seeds the authored passengers
        // before the level overwrites the queue with the level data.
        passengerQueueManager?.Initialize();

        vfxManager?.Initialize();
        cameraShakeManager?.Initialize();

        // GameManager subscribes to gameplay events before the level starts.
        gameManager.Initialize();
        uiManager?.Initialize();

        EnsureSceneBackground();
        levelLoader?.Initialize();
        gameManager.StartGame();
    }

    private void EnsureCanvases()
    {
        if (GameObject.Find("HUDCanvas") == null)
        {
            GameObject hudPrefab = Resources.Load<GameObject>("Prefabs/UI/HUDCanvas");
            if (hudPrefab != null)
            {
                GameObject hud = Instantiate(hudPrefab);
                hud.name = "HUDCanvas";
                hud.transform.localScale = Vector3.one;
            }
        }

        if (GameObject.Find("PopupCanvas") == null)
        {
            GameObject popupPrefab = Resources.Load<GameObject>("Prefabs/UI/PopupCanvas");
            if (popupPrefab != null)
            {
                GameObject popup = Instantiate(popupPrefab);
                popup.name = "PopupCanvas";
                popup.transform.localScale = Vector3.one;
            }
        }

        if (GameObject.Find("VFXCanvas") == null)
        {
            GameObject vfxPrefab = Resources.Load<GameObject>("Prefabs/UI/VFXCanvas");
            if (vfxPrefab != null)
            {
                GameObject vfx = Instantiate(vfxPrefab);
                vfx.name = "VFXCanvas";
                vfx.transform.localScale = Vector3.one;
            }
        }
    }

    private void EnsureSceneBackground()
    {
        GameObject bgGo = GameObject.Find("SceneBackground");
        if (bgGo == null)
        {
            bgGo = new GameObject("SceneBackground");
            bgGo.transform.position = Vector3.zero;
            bgGo.transform.localScale = new Vector3(0.78f, 0.78f, 1f);
            SpriteRenderer sr = bgGo.AddComponent<SpriteRenderer>();
            sr.sortingOrder = -100;
            Sprite bgSprite = Resources.Load<Sprite>("Background/background");
            if (bgSprite != null)
            {
                sr.sprite = bgSprite;
            }
        }
    }
}
