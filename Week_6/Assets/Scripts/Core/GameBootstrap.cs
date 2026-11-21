using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class GameBootstrap : MonoBehaviour
{
    public static GameBootstrap Instance { get; private set; }
    public static bool IsDriving => Instance != null && Instance.isActiveAndEnabled;

    [Header("Scene Managers (Wired in Inspector)")]
    [SerializeField] private SettingsManager settingsManager;
    [SerializeField] private ProgressManager progressManager;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private HapticManager hapticManager;
    [SerializeField] private CameraViewportFitter cameraViewportFitter;
    [SerializeField] private CameraShakeManager cameraShakeManager;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private BusStopManager busStopManager;
    [SerializeField] private PassengerQueueManager passengerQueueManager;
    [SerializeField] private GamePoolManager poolManager;
    [SerializeField] private LevelLoader levelLoader;
    [SerializeField] private BoosterManager boosterManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private VFXManager vfxManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private AppFlowController appFlowController;

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
    }

    private void Start()
    {
        RunInitialization();
    }

    public void RunInitialization()
    {
        if (_initialized) return;
        _initialized = true;

        if (gameManager == null)
        {
            Debug.LogError("[GameBootstrap] GameManager is not assigned in Inspector.");
            return;
        }

        settingsManager?.Initialize();
        progressManager?.Initialize();
        audioManager?.Initialize();
        hapticManager?.Initialize();
        cameraViewportFitter?.ApplyFit();
        cameraShakeManager?.Initialize();

        poolManager?.Initialize();
        passengerQueueManager?.Initialize();
        vfxManager?.Initialize();

        gameManager.Initialize();
        levelLoader?.Initialize();
        uiManager?.Initialize();

        if (appFlowController != null)
        {
            appFlowController.StartAppFlow();
        }
        else
        {
            gameManager.EnterGameplay();
        }
    }
}
