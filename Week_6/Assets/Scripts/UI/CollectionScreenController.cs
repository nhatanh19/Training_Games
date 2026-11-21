using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Populates vehicle collection cards from CollectionCatalogSO using an Editor-authored CollectionCardItem prefab.
/// </summary>
public class CollectionScreenController : MonoBehaviour
{
    [Header("Catalog & References")]
    [SerializeField] private CollectionCatalogSO catalog;
    [SerializeField] private LocalizationTableSO localization;
    [SerializeField] private MainHubController mainHub;
    [SerializeField] private CollectionCardView cardItemPrefab;
    [SerializeField] private Transform gridContainer;

    private readonly List<(CollectionCardView view, CollectionCatalogSO.CollectionEntry entry)> _cards =
        new List<(CollectionCardView, CollectionCatalogSO.CollectionEntry)>();
    private bool _populated;

    private void Awake()
    {
        PopulateOnce();
    }

    private void OnEnable()
    {
        SettingsController.LanguageChanged += OnLanguageChanged;
        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.OnCollectionChanged += RefreshAll;
            ProgressManager.Instance.OnLevelChanged += OnLevelChanged;
        }
        PopulateOnce();
        RefreshAll();
    }

    private void OnDisable()
    {
        SettingsController.LanguageChanged -= OnLanguageChanged;
        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.OnCollectionChanged -= RefreshAll;
            ProgressManager.Instance.OnLevelChanged -= OnLevelChanged;
        }
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
        if (visible)
        {
            PopulateOnce();
            RefreshAll();
        }
    }

    private void OnLanguageChanged(string lang) => RefreshAll();
    private void OnLevelChanged(int idx) => RefreshAll();

    private void PopulateOnce()
    {
        if (_populated || catalog == null || cardItemPrefab == null || gridContainer == null) return;
        _populated = true;

        foreach (var entry in catalog.Entries)
        {
            if (entry == null) continue;
            CollectionCardView card = Instantiate(cardItemPrefab, gridContainer);
            _cards.Add((card, entry));
        }

        RefreshAll();
    }

    public void RefreshAll()
    {
        if (!_populated) return;

        foreach (var pair in _cards)
        {
            var entry = pair.entry;
            bool unlocked = ProgressManager.Instance != null && ProgressManager.Instance.IsCollectionUnlocked(entry);
            bool claimed = ProgressManager.Instance != null && ProgressManager.Instance.IsCollectionClaimed(entry.id);

            string title = localization != null ? localization.Get(entry.titleKey) : entry.titleKey;
            string status;
            if (entry.comingSoon)
            {
                status = localization != null ? localization.Get("collection_coming_soon") : "COMING SOON";
            }
            else if (unlocked)
            {
                status = claimed
                    ? (localization != null ? localization.Get("collection_claimed") : "CLAIMED")
                    : (localization != null ? localization.Get("collection_unlocked") : "UNLOCKED");
            }
            else
            {
                status = localization != null
                    ? localization.Format("collection_unlock_level", entry.unlockAtLevel)
                    : $"Lv.{entry.unlockAtLevel}";
            }

            pair.view.Bind(entry, title, status, unlocked, claimed, cardView => OnClaimCard(cardView, entry));
        }
    }

    private void OnClaimCard(CollectionCardView cardView, CollectionCatalogSO.CollectionEntry entry)
    {
        if (ProgressManager.Instance == null || !ProgressManager.Instance.TryClaimCollectionReward(entry)) return;

        Transform target = mainHub != null ? mainHub.CoinTargetTransform : null;
        if (CoinFlyEffectManager.Instance != null && target != null)
        {
            CoinFlyEffectManager.Instance.PlayFlyCoins(cardView.CoinOrigin.position, target, 8);
        }
        else
        {
            AudioManager.Instance?.PlayCoin();
        }
        HapticManager.Instance?.TriggerSuccess();
    }
}
