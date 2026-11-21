using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
    Loading,
    Menu,
    Playing,
    GameWin,
    GameOver
}

public class GameManager : MonoBehaviour
{
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

    public int CurrentLevelIndex => ProgressManager.Instance != null ? ProgressManager.Instance.CurrentLevelIndex : 0;
    public int TotalCoins => ProgressManager.Instance != null ? ProgressManager.Instance.TotalCoins : 0;

    public event Action<GameState> OnGameStateChanged;
    public event Action<int> OnLevelStarted;

    private Coroutine _statusCheckRoutine;
    private bool _initialized;

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
    }

    private void Start()
    {
        if (GameBootstrap.IsDriving) return;
        Initialize();
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

    public void AddCoins(int amount)
    {
        ProgressManager.Instance?.AddCoins(amount);
    }

    public void EnterMenuState()
    {
        StopStatusCheck();
        BoosterManager.Instance?.CancelVipTargeting();
        LevelLoader.Instance?.ClearCurrentLevel();
        CurrentState = GameState.Menu;
        OnGameStateChanged?.Invoke(CurrentState);
    }

    public void EnterGameplay()
    {
        StartLevel(CurrentLevelIndex);
    }

    public void StartLevel(int levelIndex)
    {
        StopStatusCheck();
        BoosterManager.Instance?.CancelVipTargeting();

        CurrentState = GameState.Loading;
        ProgressManager.Instance?.SetCurrentLevelIndex(levelIndex);

        if (LevelLoader.Instance != null)
        {
            LevelLoader.Instance.LoadLevelByIndex(levelIndex);
        }

        CurrentState = GameState.Playing;
        OnGameStateChanged?.Invoke(CurrentState);
        OnLevelStarted?.Invoke(levelIndex + 1);
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

    private void StopStatusCheck()
    {
        if (_statusCheckRoutine != null)
        {
            StopCoroutine(_statusCheckRoutine);
            _statusCheckRoutine = null;
        }
    }

    private void OnBusArrived(BusController bus) => TriggerStatusCheck();
    private void OnBusDeparted() => TriggerStatusCheck();
    private void OnPassengerBoarded() => TriggerStatusCheck();

    public void TriggerStatusCheck()
    {
        StopStatusCheck();
        _statusCheckRoutine = StartCoroutine(CheckStatusRoutine());
    }

    private IEnumerator CheckStatusRoutine()
    {
        yield return new WaitForSeconds(0.45f);

        if (CurrentState != GameState.Playing) yield break;

        if (CheckWin())
        {
            BoosterManager.Instance?.CancelVipTargeting();
            ProgressManager.Instance?.MarkLevelCompleted(CurrentLevelIndex + 1);
            CurrentState = GameState.GameWin;
            OnGameStateChanged?.Invoke(CurrentState);
            VFXManager.Instance?.PlayConfetti();
            AudioManager.Instance?.PlayWin();
            HapticManager.Instance?.TriggerSuccess();
            yield break;
        }

        if (CheckDeadlock())
        {
            BoosterManager.Instance?.CancelVipTargeting();
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
        if (GridManager.Instance != null && GridManager.Instance.HasImpactReservations) return false;

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
                if (bus != null && bus.gameObject.activeInHierarchy && !bus.IsParkedOrInTransit && bus.CanExit())
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
