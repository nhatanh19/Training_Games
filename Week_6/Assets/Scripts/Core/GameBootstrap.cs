using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class GameBootstrap : MonoBehaviour
{
    public static GameBootstrap Instance { get; private set; }

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
    [SerializeField] private HapticManager hapticManager;

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
        if (hapticManager == null) hapticManager = FindObjectOfType<HapticManager>();
        if (hapticManager == null)
        {
            GameObject hapticGo = new GameObject("HapticManager");
            hapticManager = hapticGo.AddComponent<HapticManager>();
        }
    }

    public void RunInitialization()
    {
        if (_initialized) return;
        _initialized = true;

        ValidateCanvases();
        ResolveReferences();

        if (gameManager == null)
        {
            Debug.LogError("[GameBootstrap] GameManager not found in the scene. " +
                           "Add a GameManager or run Tools > Bus Jam > Setup Scene.");
            return;
        }

        settingsManager?.Initialize();
        audioManager?.Initialize();
        hapticManager?.Initialize();

        poolManager?.Initialize();

        passengerQueueManager?.Initialize();

        vfxManager?.Initialize();
        cameraShakeManager?.Initialize();

        gameManager.Initialize();
        uiManager?.Initialize();

        EnsureSceneBackground();
        levelLoader?.Initialize();
        gameManager.StartGame();
    }

    private void ValidateCanvases()
    {
        ReportMissingCanvas("HUDCanvas");
        ReportMissingCanvas("PopupCanvas");
        ReportMissingCanvas("VFXCanvas");

        if (CoinFlyEffectManager.Instance == null)
        {
            Debug.LogError("[GameBootstrap] CoinFlyEffectManager không tồn tại trong scene — chạy Tools > Bus Jam > Install Coin Fly Effect On VFXCanvas để gắn component vào VFXCanvas.prefab.");
        }
    }

    private static void ReportMissingCanvas(string canvasName)
    {
        if (GameObject.Find(canvasName) == null)
        {
            Debug.LogError($"[GameBootstrap] '{canvasName}' không có trong scene — kéo Assets/Prefabs/UI/{canvasName}.prefab vào hierarchy.");
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
