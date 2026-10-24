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
    public int LevelCount => levels != null ? levels.Count : 0;
    public int RemainingBusesCount => _spawnedBuses.Count;

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

        if (levels.Count == 0 && currentLevelData != null)
        {
            levels.Add(currentLevelData);
        }

#if UNITY_EDITOR
        if (levels.Count == 0)
        {
            AutoAssignLevels();
        }
#endif
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (levels == null || levels.Count == 0)
        {
            AutoAssignLevels();
        }
    }

    [ContextMenu("Auto Assign All Levels")]
    public void AutoAssignLevels()
    {
        levels = new List<LevelDataSO>();
        for (int i = 1; i <= 5; i++)
        {
            LevelDataSO lvl = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelDataSO>($"Assets/Levels/Level_{i}.asset");
            if (lvl != null)
            {
                levels.Add(lvl);
            }
        }
        UnityEditor.EditorUtility.SetDirty(this);
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

    /// <summary>
    /// Ensures the level collection is usable. Called by GameBootstrap before the first level.
    /// </summary>
    public void Initialize()
    {
        if (levels == null) levels = new List<LevelDataSO>();

        if (levels.Count == 0 && currentLevelData != null)
        {
            levels.Add(currentLevelData);
        }
    }

    public void LoadLevel(LevelDataSO levelData)
    {
        if (levelData == null) return;

        GameInputManager.SetLock(true);
        currentLevelData = levelData;
        ClearCurrentLevel();

        // Pools are created once from the serialized prefabs; do not reset them per level.
        GamePoolManager.Instance.Initialize();

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
        foreach (var bus in _spawnedBuses)
        {
            if (bus != null)
            {
                GamePoolManager.Instance.ReleaseBus(bus);
            }
        }
        _spawnedBuses.Clear();

        if (BusStopManager.Instance != null)
        {
            BusStopManager.Instance.ClearAllSlots();
        }

        if (PassengerQueueManager.Instance != null)
        {
            PassengerQueueManager.Instance.ClearQueue();
        }
    }
}
