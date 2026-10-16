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
    }

    private void Start()
    {
        if (currentLevelData != null)
        {
            LoadLevel(currentLevelData);
        }
    }

    public void LoadLevel(LevelDataSO levelData)
    {
        if (levelData == null) return;

        currentLevelData = levelData;
        ClearCurrentLevel();

        if (GridManager.Instance != null)
        {
            GridManager.Instance.InitializeGrid(levelData.rows, levelData.cols);
        }

        foreach (var data in levelData.buses)
        {
            BusController prefab = data.length == 3 ? bus3x1Prefab : bus2x1Prefab;
            if (prefab != null)
            {
                BusController bus = Instantiate(prefab, transform);
                bus.Initialize(data.color, data.direction, data.length, data.rootGridPosition);
                _spawnedBuses.Add(bus);
            }
        }

        if (PassengerQueueManager.Instance != null && passengerPrefab != null)
        {
            PassengerQueueManager.Instance.LoadPassengers(levelData.passengerColors, passengerPrefab);
        }
    }

    public void ClearCurrentLevel()
    {
        foreach (var bus in _spawnedBuses)
        {
            if (bus != null)
            {
                Destroy(bus.gameObject);
            }
        }
        _spawnedBuses.Clear();

        if (PassengerQueueManager.Instance != null)
        {
            PassengerQueueManager.Instance.ClearQueue();
        }
    }
}
