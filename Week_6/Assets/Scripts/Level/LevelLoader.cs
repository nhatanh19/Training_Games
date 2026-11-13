using System.Collections.Generic;
using UnityEngine;

public class LevelLoader : MonoBehaviour
{
    public static LevelLoader Instance { get; private set; }

    [Header("Current Level")]
    [SerializeField] private LevelDataSO currentLevelData;

    [Header("Prefabs")]
    [SerializeField] private BusController bus2x1Prefab;
    [SerializeField] private BusController bus3x1Prefab;
    [SerializeField] private PassengerController passengerPrefab;

    [Header("Level Collection")]
    [SerializeField] private List<LevelDataSO> levels = new List<LevelDataSO>();

    public BusController Bus2x1Prefab => bus2x1Prefab;
    public BusController Bus3x1Prefab => bus3x1Prefab;
    public PassengerController PassengerPrefab => passengerPrefab;
    public LevelDataSO CurrentLevelData => currentLevelData;
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
        else
        {
            Destroy(gameObject);
        }

        if (levels.Count == 0 && currentLevelData == null)
        {
            Debug.LogError("[LevelLoader] 'levels' rỗng và currentLevelData chưa gán — kéo Assets/Levels/Level_*.asset vào list 'Levels' trên Inspector.");
        }
    }

#if UNITY_EDITOR

    [ContextMenu("Append Missing Levels From Assets/Levels")]
    public void AppendMissingLevels()
    {
        if (levels == null)
        {
            levels = new List<LevelDataSO>();
        }

        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:LevelDataSO", new[] { "Assets/Levels" });
        var paths = new List<string>(guids.Length);
        for (int i = 0; i < guids.Length; i++)
        {
            paths.Add(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]));
        }
        paths.Sort(System.StringComparer.Ordinal);

        int added = 0;
        for (int i = 0; i < paths.Count; i++)
        {
            LevelDataSO level = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelDataSO>(paths[i]);
            if (level != null && !levels.Contains(level))
            {
                levels.Add(level);
                added++;
            }
        }

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[LevelLoader] Đã bổ sung {added} level vào list 'levels' (tổng: {levels.Count}).");
    }
#endif

    public void RemoveSpawnedBus(BusController bus)
    {
        if (bus != null)
        {
            _spawnedBuses.Remove(bus);
        }
    }

    public void LoadLevelByIndex(int index)
    {
        if (levels != null && levels.Count > 0)
        {
            int safeIndex = Mathf.Clamp(index, 0, levels.Count - 1);
            LoadLevel(levels[safeIndex]);
        }
        else if (currentLevelData != null)
        {
            LoadLevel(currentLevelData);
        }
    }

    public void Initialize()
    {
        if (levels == null)
        {
            levels = new List<LevelDataSO>();
        }

        if (levels.Count == 0)
        {
            Debug.LogError(currentLevelData != null
                ? "[LevelLoader] 'levels' rỗng — game chỉ chơi được 1 level (currentLevelData). Kéo Assets/Levels/Level_*.asset vào list 'Levels' trên Inspector."
                : "[LevelLoader] 'levels' rỗng và currentLevelData chưa gán — kéo Assets/Levels/Level_*.asset vào list 'Levels' trên Inspector.");
        }
    }

    public void LoadLevel(LevelDataSO levelData)
    {
        if (levelData == null) return;

        GameInputManager.SetLock(true);
        currentLevelData = levelData;
        ClearCurrentLevel();

        GamePoolManager.Instance.Initialize();

        if (BusStopManager.Instance != null)
        {
            BusStopManager.Instance.ConfigureSlots(levelData.unlockedSlots);
        }

        if (GridManager.Instance != null)
        {
            GridManager.Instance.InitializeGrid(levelData.rows, levelData.cols);
        }

        foreach (var data in levelData.buses)
        {
            BusController bus = GamePoolManager.Instance.GetBus(data.length, transform);
            if (bus != null)
            {
                bus.Initialize(data.color, data.direction, data.length, data.rootGridPosition);
                _spawnedBuses.Add(bus);
            }
        }

        if (PassengerQueueManager.Instance != null)
        {
            PassengerQueueManager.Instance.LoadPassengers(levelData.passengerColors, passengerPrefab);
        }

        GameInputManager.SetLock(false);
    }

    public void ClearCurrentLevel()
    {
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
