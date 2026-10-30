using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
    Loading,
    Playing,
    GameWin,
    GameOver
}

public class GameManager : MonoBehaviour
{
    private const string LEVEL_KEY = "CurrentLevelIndex";

    private static GameManager _instance;

    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    _instance = go.AddComponent<GameManager>();
                }
            }
            return _instance;
        }
    }

    public GameState CurrentState { get; private set; } = GameState.Loading;
    public int CurrentLevelIndex { get; private set; } = 0;

    public event Action<GameState> OnGameStateChanged;
    public event Action<int> OnLevelStarted;

    private Coroutine _statusCheckRoutine;
    private bool _initialized;
    private bool _started;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        CurrentLevelIndex = PlayerPrefs.GetInt(LEVEL_KEY, 0);
    }

    private void Start()
    {
        // When a GameBootstrap exists it calls Initialize()/StartGame() itself.
        if (GameBootstrap.IsDriving) return;

        Initialize();
        StartGame();
    }

    /// <summary>
    /// Subscribes to gameplay events. Called by GameBootstrap before the level starts,
    /// or by Start() when the scene has no bootstrap.
    /// </summary>
    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        if (BusStopManager.Instance != null)
        {
            BusStopManager.Instance.OnBusArrivedAtStop += OnBusArrived;
            BusStopManager.Instance.OnBusDepartedFromStop += OnBusDeparted;
        }

        if (PassengerQueueManager.Instance != null)
        {
            PassengerQueueManager.Instance.OnPassengerBoarded += OnPassengerBoarded;
        }
    }

    /// <summary>
    /// Starts the saved level exactly once.
    /// </summary>
    public void StartGame()
    {
        if (_started) return;
        _started = true;

        StartLevel(CurrentLevelIndex);
    }

    private void OnDestroy()
    {
        if (BusStopManager.Instance != null)
        {
            BusStopManager.Instance.OnBusArrivedAtStop -= OnBusArrived;
            BusStopManager.Instance.OnBusDepartedFromStop -= OnBusDeparted;
        }

        if (PassengerQueueManager.Instance != null)
        {
            PassengerQueueManager.Instance.OnPassengerBoarded -= OnPassengerBoarded;
        }
    }

    public void StartLevel(int levelIndex)
    {
        if (_statusCheckRoutine != null)
        {
            StopCoroutine(_statusCheckRoutine);
            _statusCheckRoutine = null;
        }

        CurrentState = GameState.Loading;
        CurrentLevelIndex = levelIndex;
        PlayerPrefs.SetInt(LEVEL_KEY, CurrentLevelIndex);
        PlayerPrefs.Save();

        if (LevelLoader.Instance != null)
        {
            LevelLoader.Instance.LoadLevelByIndex(levelIndex);
        }

        CurrentState = GameState.Playing;
        OnGameStateChanged?.Invoke(CurrentState);
        OnLevelStarted?.Invoke(CurrentLevelIndex + 1);
    }

    public void RestartLevel()
    {
        StartLevel(CurrentLevelIndex);
    }

    public void NextLevel()
    {
        int totalLevels = LevelLoader.Instance != null ? LevelLoader.Instance.LevelCount : 1;
        if (totalLevels <= 0) totalLevels = 1;
        int nextIndex = (CurrentLevelIndex + 1) % totalLevels;
        StartLevel(nextIndex);
    }

    private void OnBusArrived(BusController bus)
    {
        TriggerStatusCheck();
    }

    private void OnBusDeparted()
    {
        TriggerStatusCheck();
    }

    private void OnPassengerBoarded()
    {
        TriggerStatusCheck();
    }

    public void TriggerStatusCheck()
    {
        if (_statusCheckRoutine != null)
        {
            StopCoroutine(_statusCheckRoutine);
        }
        _statusCheckRoutine = StartCoroutine(CheckStatusRoutine());
    }

    private IEnumerator CheckStatusRoutine()
    {
        yield return new WaitForSeconds(0.45f);

        if (CurrentState != GameState.Playing) yield break;

        if (CheckWin())
        {
            CurrentState = GameState.GameWin;
            OnGameStateChanged?.Invoke(CurrentState);
            VFXManager.Instance?.PlayConfetti();
            AudioManager.Instance?.PlayWin();
            yield break;
        }

        if (CheckDeadlock())
        {
            CurrentState = GameState.GameOver;
            OnGameStateChanged?.Invoke(CurrentState);
            AudioManager.Instance?.PlayLose();
        }
    }

    private bool CheckWin()
    {
        bool noPassengers = PassengerQueueManager.Instance == null || PassengerQueueManager.Instance.RemainingPassengersCount == 0;
        bool noSpawnedBuses = LevelLoader.Instance == null || LevelLoader.Instance.RemainingBusesCount == 0;
        bool noActiveBuses = BusStopManager.Instance == null || BusStopManager.Instance.GetActiveBuses().Count == 0;
        return noPassengers && noSpawnedBuses && noActiveBuses;
    }

    private bool CheckDeadlock()
    {
        if (BusStopManager.Instance == null || BusStopManager.Instance.HasAvailableSlot) return false;
        if (PassengerQueueManager.Instance == null || PassengerQueueManager.Instance.RemainingPassengersCount == 0) return false;
        if (PassengerQueueManager.Instance.IsBoarding) return false;

        PassengerController head = PassengerQueueManager.Instance.PeekNextPassenger();
        if (head == null) return false;

        List<BusController> activeBuses = BusStopManager.Instance.GetActiveBuses();
        if (activeBuses.Count < BusStopManager.Instance.TotalSlotCount) return false;

        bool hasMatchingBus = activeBuses.Exists(b => b.Color == head.Color && b.HasEmptySeat);
        return !hasMatchingBus;
    }
}
