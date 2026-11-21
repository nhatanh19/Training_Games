using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Executes the three Bus Jam gameplay boosters configured in BoosterCatalogSO.
/// </summary>
public class BoosterManager : MonoBehaviour
{
    public static BoosterManager Instance { get; private set; }

    [Header("Catalog")]
    [SerializeField] private BoosterCatalogSO boosterCatalog;

    public bool IsVipTargeting { get; private set; }
    public BoosterCatalogSO Catalog => boosterCatalog;

    public event Action<bool> OnVipTargetingChanged;
    public event Action<BoosterType, string> OnBoosterFeedback;

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

    public bool IsUnlocked(BoosterType type, int currentLevelNumber)
    {
        BoosterCatalogSO.BoosterDefinition def = boosterCatalog != null ? boosterCatalog.GetDefinition(type) : null;
        return def == null || currentLevelNumber >= def.unlockLevel;
    }

    public int GetRefillCost(BoosterType type)
    {
        BoosterCatalogSO.BoosterDefinition def = boosterCatalog != null ? boosterCatalog.GetDefinition(type) : null;
        return def != null ? def.refillCoinCost : 50;
    }

    public void CancelVipTargeting()
    {
        if (!IsVipTargeting) return;
        IsVipTargeting = false;
        OnVipTargetingChanged?.Invoke(false);
    }

    public void OnBoosterButtonClicked(BoosterType type)
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;
        if (UIManager.Instance != null && UIManager.Instance.IsAnyPopupOpen()) return;

        int levelNumber = GameManager.Instance.CurrentLevelIndex + 1;
        if (!IsUnlocked(type, levelNumber))
        {
            AudioManager.Instance?.PlayCrash();
            HapticManager.Instance?.TriggerWarning();
            OnBoosterFeedback?.Invoke(type, "LOCKED");
            return;
        }

        if (type == BoosterType.VipSend && IsVipTargeting)
        {
            CancelVipTargeting();
            AudioManager.Instance?.PlayClick();
            return;
        }

        if (!HasChargeOrCoins(type))
        {
            AudioManager.Instance?.PlayCrash();
            HapticManager.Instance?.TriggerWarning();
            OnBoosterFeedback?.Invoke(type, "NO_COINS");
            return;
        }

        switch (type)
        {
            case BoosterType.SortQueue:
                ExecuteSortQueue();
                break;

            case BoosterType.ShuffleColors:
                ExecuteShuffleColors();
                break;

            case BoosterType.VipSend:
                if (BusStopManager.Instance == null || !BusStopManager.Instance.HasAvailableSlot)
                {
                    HapticManager.Instance?.TriggerWarning();
                    BusStopManager.Instance?.ShowFullFeedback();
                    OnBoosterFeedback?.Invoke(type, "BAY_FULL");
                    return;
                }
                IsVipTargeting = true;
                AudioManager.Instance?.PlayClick();
                HapticManager.Instance?.TriggerLight();
                OnVipTargetingChanged?.Invoke(true);
                break;
        }
    }

    public void ConfirmVipTarget(BusController bus)
    {
        if (!IsVipTargeting || bus == null || bus.IsParkedOrInTransit) return;

        if (BusStopManager.Instance == null || !BusStopManager.Instance.HasAvailableSlot)
        {
            CancelVipTargeting();
            BusStopManager.Instance?.ShowFullFeedback();
            return;
        }

        if (!ConsumeChargeOrSpendCoins(BoosterType.VipSend))
        {
            CancelVipTargeting();
            OnBoosterFeedback?.Invoke(BoosterType.VipSend, "NO_COINS");
            return;
        }

        IsVipTargeting = false;
        OnVipTargetingChanged?.Invoke(false);

        if (bus.SendViaVipBooster())
        {
            AudioManager.Instance?.PlayBooster();
            HapticManager.Instance?.TriggerSuccess();
        }
    }

    private void ExecuteSortQueue()
    {
        CancelVipTargeting();
        if (PassengerQueueManager.Instance == null) return;

        var priorityColors = new List<BusColor>();
        if (BusStopManager.Instance != null)
        {
            foreach (BusController parked in BusStopManager.Instance.GetActiveBuses())
            {
                if (parked != null && parked.HasEmptySeat && !priorityColors.Contains(parked.Color))
                {
                    priorityColors.Add(parked.Color);
                }
            }
        }

        if (priorityColors.Count == 0 && LevelLoader.Instance != null)
        {
            foreach (BusController bus in LevelLoader.Instance.SpawnedBuses)
            {
                if (bus != null && !bus.IsParkedOrInTransit && bus.CanExit() && !priorityColors.Contains(bus.Color))
                {
                    priorityColors.Add(bus.Color);
                }
            }
        }

        if (priorityColors.Count == 0)
        {
            OnBoosterFeedback?.Invoke(BoosterType.SortQueue, "NO_TARGET");
            return;
        }

        if (!PassengerQueueManager.Instance.ReorderMatchingToFront(priorityColors))
        {
            OnBoosterFeedback?.Invoke(BoosterType.SortQueue, "ALREADY_SORTED");
            return;
        }

        if (!ConsumeChargeOrSpendCoins(BoosterType.SortQueue)) return;

        AudioManager.Instance?.PlayBooster();
        HapticManager.Instance?.TriggerSuccess();
        GameManager.Instance?.TriggerStatusCheck();
    }

    private void ExecuteShuffleColors()
    {
        CancelVipTargeting();
        if (LevelLoader.Instance == null) return;

        var group2 = new List<BusController>();
        var group3 = new List<BusController>();

        foreach (BusController bus in LevelLoader.Instance.SpawnedBuses)
        {
            if (bus == null || !bus.gameObject.activeInHierarchy || bus.IsParkedOrInTransit) continue;
            if (bus.Length >= 3) group3.Add(bus);
            else group2.Add(bus);
        }

        bool changed = TryShuffleGroup(group2) | TryShuffleGroup(group3);
        if (!changed)
        {
            AudioManager.Instance?.PlayCrash();
            OnBoosterFeedback?.Invoke(BoosterType.ShuffleColors, "NO_TARGET");
            return;
        }

        if (!ConsumeChargeOrSpendCoins(BoosterType.ShuffleColors)) return;

        AudioManager.Instance?.PlayBooster();
        HapticManager.Instance?.TriggerSuccess();
        PassengerQueueManager.Instance?.TryBoardNextPassenger();
        GameManager.Instance?.TriggerStatusCheck();
    }

    private static bool TryShuffleGroup(List<BusController> buses)
    {
        if (buses == null || buses.Count < 2) return false;

        var colors = new List<BusColor>(buses.Count);
        bool hasVariety = false;
        BusColor first = buses[0].Color;
        for (int i = 0; i < buses.Count; i++)
        {
            colors.Add(buses[i].Color);
            if (buses[i].Color != first) hasVariety = true;
        }

        if (!hasVariety) return false;

        for (int i = colors.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (colors[i], colors[j]) = (colors[j], colors[i]);
        }

        bool anyDifference = false;
        for (int i = 0; i < buses.Count; i++)
        {
            if (colors[i] != buses[i].Color)
            {
                anyDifference = true;
                break;
            }
        }

        if (!anyDifference)
        {
            for (int i = 1; i < colors.Count; i++)
            {
                if (colors[i] != colors[0])
                {
                    (colors[0], colors[i]) = (colors[i], colors[0]);
                    break;
                }
            }
        }

        for (int i = 0; i < buses.Count; i++)
        {
            buses[i].SetColorVisual(colors[i]);
        }

        return true;
    }

    private bool HasChargeOrCoins(BoosterType type)
    {
        if (ProgressManager.Instance == null) return false;
        if (ProgressManager.Instance.GetBoosterCount(type) > 0) return true;
        return ProgressManager.Instance.TotalCoins >= GetRefillCost(type);
    }

    private bool ConsumeChargeOrSpendCoins(BoosterType type)
    {
        if (ProgressManager.Instance == null) return false;
        if (ProgressManager.Instance.TryConsumeBooster(type)) return true;
        return ProgressManager.Instance.TrySpendCoins(GetRefillCost(type));
    }
}
