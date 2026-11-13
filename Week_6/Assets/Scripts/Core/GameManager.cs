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

            }
            return _instance;
        }
    }

    public GameState CurrentState { get; private set; } = GameState.Loading;
    public LevelDataSO CurrentLevelData => LevelLoader.Instance != null ? LevelLoader.Instance.CurrentLevelData : null;
    public int CurrentLevelIndex { get; private set; } = 0;
    public const string COIN_KEY = "TotalCoins";

    public int TotalCoins
    {
        get => PlayerPrefs.GetInt(COIN_KEY, 0);
        set
        {
            PlayerPrefs.SetInt(COIN_KEY, value);
            PlayerPrefs.Save();
        }
    }

    public void AddCoins(int amount)
    {
        TotalCoins += amount;
        if (UIManager.Instance?.HUD != null)
        {
            UIManager.Instance.HUD.SetCoin(TotalCoins, true, false);
        }
    }

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

        if (GameBootstrap.IsDriving) return;

        Initialize();
        StartGame();
    }

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
            int levelCount = LevelLoader.Instance.LevelCount;
            int dataIndex = levelCount > 0 ? levelIndex % levelCount : 0;
            LevelLoader.Instance.LoadLevelByIndex(dataIndex);
        }

        CurrentState = GameState.Playing;
        OnGameStateChanged?.Invoke(CurrentState);
        OnLevelStarted?.Invoke(CurrentLevelIndex + 1);
    }

    public void RestartLevel()
    {
        StartLevel(CurrentLevelIndex);
    }

    public void ResumeAfterRevive()
    {
        if (CurrentState != GameState.GameOver) return;

        CurrentState = GameState.Playing;
        OnGameStateChanged?.Invoke(CurrentState);
        PassengerQueueManager.Instance?.TryBoardNextPassenger();
        TriggerStatusCheck();
    }

    public void NextLevel()
    {
        StartLevel(CurrentLevelIndex + 1);
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
            HapticManager.Instance?.TriggerSuccess();
            yield break;
        }

        if (CheckDeadlock())
        {
            CurrentState = GameState.GameOver;
            OnGameStateChanged?.Invoke(CurrentState);
            AudioManager.Instance?.PlayLose();
            HapticManager.Instance?.TriggerWarning();
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
        if (PassengerQueueManager.Instance == null || PassengerQueueManager.Instance.RemainingPassengersCount == 0) return false;
        if (PassengerQueueManager.Instance.IsBoarding) return false;

        PassengerController head = PassengerQueueManager.Instance.PeekNextPassenger();
        if (head == null) return false;

        if (BusStopManager.Instance == null) return false;
        if (BusStopManager.Instance.HasBusesInTransit) return false;

        List<BusController> activeBuses = BusStopManager.Instance.GetActiveBuses();

        if (!BusStopManager.Instance.HasAvailableSlot && activeBuses.Count >= BusStopManager.Instance.UnlockedSlotCount)
        {
            bool hasMatchingBus = activeBuses.Exists(b => b.Color == head.Color && b.HasEmptySeat);
            if (!hasMatchingBus)
            {
                return true;
            }
        }

        bool anySlotCanAccept = activeBuses.Exists(b => b.Color == head.Color && b.HasEmptySeat);
        if (!anySlotCanAccept && LevelLoader.Instance != null && LevelLoader.Instance.RemainingBusesCount > 0)
        {
            bool anyGridBusCanMove = false;
            foreach (var bus in LevelLoader.Instance.SpawnedBuses)
            {
                if (bus != null && bus.gameObject.activeInHierarchy && !activeBuses.Contains(bus) && bus.CanExit())
                {
                    anyGridBusCanMove = true;
                    break;
                }
            }

            if (!anyGridBusCanMove)
            {
                return true;
            }
        }

        return false;
    }
}
