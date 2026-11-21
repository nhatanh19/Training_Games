using System.Collections.Generic;
using UnityEngine;

public class LevelLoader : MonoBehaviour
{
    public static LevelLoader Instance { get; private set; }

    [Header("Current Level")]
    [SerializeField] private LevelDataSO currentLevelData;

    [Header("Visual Theme & Prefabs")]
    [SerializeField] private GameVisualThemeSO visualTheme;
    [SerializeField] private BusController bus2x1Prefab;
    [SerializeField] private BusController bus3x1Prefab;
    [SerializeField] private PassengerController passengerPrefab;

    [Header("Level Collection (Authored in Inspector)")]
    [SerializeField] private List<LevelDataSO> levels = new List<LevelDataSO>();

    public BusController Bus2x1Prefab => bus2x1Prefab;
    public BusController Bus3x1Prefab => bus3x1Prefab;
    public PassengerController PassengerPrefab => passengerPrefab;
    public LevelDataSO CurrentLevelData => currentLevelData;
    public GameVisualThemeSO VisualTheme => visualTheme;
    public int LevelCount => levels != null ? levels.Count : 0;
    public int RemainingBusesCount => _spawnedBuses.Count;
    public IReadOnlyList<BusController> SpawnedBuses => _spawnedBuses;

    private readonly List<BusController> _spawnedBuses = new List<BusController>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void Initialize()
    {
        if (levels == null) levels = new List<LevelDataSO>();
        if (levels.Count == 0 && currentLevelData == null)
        {
            Debug.LogError("[LevelLoader] No levels assigned in Inspector.");
        }
    }

    public LevelDataSO GetLevelDataByIndex(int index)
    {
        if (levels != null && levels.Count > 0)
        {
            int safeIndex = index % levels.Count;
            if (safeIndex < 0) safeIndex += levels.Count;
            return levels[safeIndex];
        }
        return currentLevelData;
    }

    public void RemoveSpawnedBus(BusController bus)
    {
        if (bus != null)
        {
            _spawnedBuses.Remove(bus);
        }
    }

    public void LoadLevelByIndex(int index)
    {
        LevelDataSO data = GetLevelDataByIndex(index);
        if (data != null)
        {
            LoadLevel(data);
        }
    }

    public void LoadLevel(LevelDataSO levelData)
    {
        if (levelData == null) return;

        GameInputManager.SetLock(true);
        currentLevelData = levelData;
        ClearCurrentLevel();

        GamePoolManager.Instance?.Initialize();

        if (BusStopManager.Instance != null)
        {
            BusStopManager.Instance.ConfigureSlots(levelData.unlockedSlots);
        }

        float scaleFactor = 1f;
        if (GridManager.Instance != null)
        {
            GridManager.Instance.InitializeGrid(levelData);
            scaleFactor = GridManager.Instance.VehicleScaleFactor;
        }

        if (levelData.buses != null)
        {
            foreach (var data in levelData.buses)
            {
                if (data == null) continue;
                BusController bus = GamePoolManager.Instance.GetBus(data.length, transform);
                if (bus != null)
                {
                    bus.Initialize(data.color, data.direction, data.length, data.rootGridPosition, scaleFactor, visualTheme);
                    _spawnedBuses.Add(bus);
                }
            }
        }

        if (PassengerQueueManager.Instance != null)
        {
            PassengerQueueManager.Instance.LoadPassengers(levelData.passengerColors, visualTheme);
        }

        GameInputManager.SetLock(false);
    }

    public void ClearCurrentLevel()
    {
        VFXManager.Instance?.ClearLevelEffects();
        CameraShakeManager.Instance?.ResetShake();

        if (PassengerQueueManager.Instance != null)
        {
            PassengerQueueManager.Instance.ClearQueue();
        }

        if (BusStopManager.Instance != null)
        {
            BusStopManager.Instance.ClearAllSlots();
        }

        foreach (var bus in _spawnedBuses)
        {
            if (bus != null)
            {
                GamePoolManager.Instance.ReleaseBus(bus);
            }
        }
        _spawnedBuses.Clear();

        BusController[] allActiveBuses = FindObjectsOfType<BusController>();
        foreach (var bus in allActiveBuses)
        {
            if (bus != null && bus.gameObject.activeInHierarchy)
            {
                GamePoolManager.Instance.ReleaseBus(bus);
            }
        }
    }
}
