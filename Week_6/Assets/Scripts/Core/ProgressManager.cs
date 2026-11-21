using System;
using UnityEngine;

/// <summary>
/// Single source of truth for player coins, level progression, booster inventory, and collection claims.
/// </summary>
public class ProgressManager : MonoBehaviour
{
    private const string CoinsPrefKey = "TotalCoins";
    private const string LevelPrefKey = "CurrentLevelIndex";
    private const string HighestCompletedKey = "HighestCompletedLevel";
    private const string BoosterPrefix = "BoosterCount_";
    private const string BoosterInitKey = "BoostersInitialized_v1";
    private const string CollectionClaimedPrefix = "CollectionClaimed_";

    public static ProgressManager Instance { get; private set; }

    [Header("Catalogs")]
    [SerializeField] private BoosterCatalogSO boosterCatalog;
    [SerializeField, Min(0)] private int initialCoins = 100;

    public int TotalCoins { get; private set; }
    public int CurrentLevelIndex { get; private set; }
    public int HighestCompletedLevel { get; private set; }

    public event Action<int> OnCoinsChanged;
    public event Action<int> OnLevelChanged;
    public event Action OnBoostersChanged;
    public event Action OnCollectionChanged;

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

        Initialize();
    }

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        if (!PlayerPrefs.HasKey(CoinsPrefKey))
        {
            PlayerPrefs.SetInt(CoinsPrefKey, Mathf.Max(0, initialCoins));
        }

        TotalCoins = Mathf.Max(0, PlayerPrefs.GetInt(CoinsPrefKey, initialCoins));
        CurrentLevelIndex = Mathf.Max(0, PlayerPrefs.GetInt(LevelPrefKey, 0));
        HighestCompletedLevel = Mathf.Max(0, PlayerPrefs.GetInt(HighestCompletedKey, 0));

        if (PlayerPrefs.GetInt(BoosterInitKey, 0) == 0 && boosterCatalog != null)
        {
            foreach (var def in boosterCatalog.Boosters)
            {
                if (def != null)
                {
                    PlayerPrefs.SetInt(BoosterPrefix + def.type, Mathf.Max(0, def.startingCount));
                }
            }
            PlayerPrefs.SetInt(BoosterInitKey, 1);
            PlayerPrefs.Save();
        }
    }

    public void AddCoins(int amount)
    {
        if (amount == 0) return;
        TotalCoins = Mathf.Max(0, TotalCoins + amount);
        PlayerPrefs.SetInt(CoinsPrefKey, TotalCoins);
        PlayerPrefs.Save();
        OnCoinsChanged?.Invoke(TotalCoins);
    }

    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0) return true;
        if (TotalCoins < amount) return false;
        TotalCoins -= amount;
        PlayerPrefs.SetInt(CoinsPrefKey, TotalCoins);
        PlayerPrefs.Save();
        OnCoinsChanged?.Invoke(TotalCoins);
        return true;
    }

    public void SetCurrentLevelIndex(int levelIndex)
    {
        CurrentLevelIndex = Mathf.Max(0, levelIndex);
        PlayerPrefs.SetInt(LevelPrefKey, CurrentLevelIndex);
        PlayerPrefs.Save();
        OnLevelChanged?.Invoke(CurrentLevelIndex);
    }

    public void MarkLevelCompleted(int completedLevelNumber)
    {
        if (completedLevelNumber > HighestCompletedLevel)
        {
            HighestCompletedLevel = completedLevelNumber;
            PlayerPrefs.SetInt(HighestCompletedKey, HighestCompletedLevel);
            PlayerPrefs.Save();
            OnCollectionChanged?.Invoke();
        }
    }

    public int GetBoosterCount(BoosterType type)
    {
        return Mathf.Max(0, PlayerPrefs.GetInt(BoosterPrefix + type, 0));
    }

    public void AddBooster(BoosterType type, int amount)
    {
        if (amount <= 0) return;
        int next = GetBoosterCount(type) + amount;
        PlayerPrefs.SetInt(BoosterPrefix + type, next);
        PlayerPrefs.Save();
        OnBoostersChanged?.Invoke();
    }

    public bool TryConsumeBooster(BoosterType type)
    {
        int count = GetBoosterCount(type);
        if (count <= 0) return false;
        PlayerPrefs.SetInt(BoosterPrefix + type, count - 1);
        PlayerPrefs.Save();
        OnBoostersChanged?.Invoke();
        return true;
    }

    public bool IsCollectionUnlocked(CollectionCatalogSO.CollectionEntry entry)
    {
        if (entry == null || entry.comingSoon) return false;
        int reachedLevel = Mathf.Max(CurrentLevelIndex + 1, HighestCompletedLevel + 1);
        return reachedLevel >= entry.unlockAtLevel;
    }

    public bool IsCollectionClaimed(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        return PlayerPrefs.GetInt(CollectionClaimedPrefix + id, 0) == 1;
    }

    public bool TryClaimCollectionReward(CollectionCatalogSO.CollectionEntry entry)
    {
        if (entry == null || entry.comingSoon || string.IsNullOrEmpty(entry.id)) return false;
        if (!IsCollectionUnlocked(entry) || IsCollectionClaimed(entry.id)) return false;

        PlayerPrefs.SetInt(CollectionClaimedPrefix + entry.id, 1);
        PlayerPrefs.Save();
        if (entry.claimCoinReward > 0) AddCoins(entry.claimCoinReward);
        OnCollectionChanged?.Invoke();
        return true;
    }
}
