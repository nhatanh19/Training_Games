using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// Populates Shop bundles, coin packs, and booster packs using serialized Item Prefabs and ShopCatalogSO.
/// </summary>
public class ShopScreenController : MonoBehaviour
{
    [Header("Catalog & Localization")]
    [SerializeField] private ShopCatalogSO catalog;
    [SerializeField] private LocalizationTableSO localization;
    [SerializeField] private MainHubController mainHub;

    [Header("Item Prefabs (Authored in Editor)")]
    [SerializeField] private ShopOfferItemView bundleItemPrefab;
    [SerializeField] private ShopOfferItemView coinPackItemPrefab;
    [SerializeField] private ShopOfferItemView boosterPackItemPrefab;

    [Header("Layout Containers")]
    [SerializeField] private Transform bundleContainer;
    [SerializeField] private Transform coinPackContainer;
    [SerializeField] private Transform boosterPackContainer;
    [SerializeField] private TextMeshProUGUI feedbackToastText;

    private readonly List<(ShopOfferItemView view, ShopCatalogSO.BundleOffer data)> _bundleViews = new List<(ShopOfferItemView, ShopCatalogSO.BundleOffer)>();
    private readonly List<(ShopOfferItemView view, ShopCatalogSO.CoinPackOffer data)> _coinViews = new List<(ShopOfferItemView, ShopCatalogSO.CoinPackOffer)>();
    private readonly List<(ShopOfferItemView view, ShopCatalogSO.BoosterPackOffer data)> _boosterViews = new List<(ShopOfferItemView, ShopCatalogSO.BoosterPackOffer)>();
    private bool _populated;
    private Tween _toastTween;

    private void Awake()
    {
        if (feedbackToastText != null) feedbackToastText.gameObject.SetActive(false);
        PopulateOnce();
    }

    private void OnEnable()
    {
        SettingsController.LanguageChanged += OnLanguageChanged;
        PopulateOnce();
        RefreshAllTexts();
    }

    private void OnDisable()
    {
        SettingsController.LanguageChanged -= OnLanguageChanged;
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
        if (visible)
        {
            PopulateOnce();
            RefreshAllTexts();
        }
    }

    private void OnLanguageChanged(string lang) => RefreshAllTexts();

    private void PopulateOnce()
    {
        if (_populated || catalog == null) return;
        _populated = true;

        if (bundleItemPrefab != null && bundleContainer != null)
        {
            foreach (var b in catalog.Bundles)
            {
                if (b == null) continue;
                ShopOfferItemView view = Instantiate(bundleItemPrefab, bundleContainer);
                _bundleViews.Add((view, b));
            }
        }

        if (coinPackItemPrefab != null && coinPackContainer != null)
        {
            foreach (var p in catalog.CoinPacks)
            {
                if (p == null) continue;
                ShopOfferItemView view = Instantiate(coinPackItemPrefab, coinPackContainer);
                _coinViews.Add((view, p));
            }
        }

        if (boosterPackItemPrefab != null && boosterPackContainer != null)
        {
            foreach (var bp in catalog.BoosterPacks)
            {
                if (bp == null) continue;
                ShopOfferItemView view = Instantiate(boosterPackItemPrefab, boosterPackContainer);
                _boosterViews.Add((view, bp));
            }
        }

        RefreshAllTexts();
    }

    public void RefreshAllTexts()
    {
        if (!_populated) return;

        foreach (var pair in _bundleViews)
        {
            var b = pair.data;
            string title = localization != null ? localization.Get(b.titleKey) : b.titleKey;
            string desc = localization != null ? localization.Get(b.descriptionKey) : b.descriptionKey;
            string reward = $"+{b.coinReward}";
            pair.view.Bind(title, desc, reward, b.priceLabel, b.iconSprite, v => PurchaseBundle(v, b));
        }

        foreach (var pair in _coinViews)
        {
            var p = pair.data;
            string title = localization != null ? localization.Get(p.titleKey) : p.titleKey;
            string price = p.requiresSimulatedAd && localization != null ? localization.Get("shop_free_ad") : p.priceLabel;
            pair.view.Bind(title, string.Empty, $"+{p.coinAmount}", price, p.iconSprite, v => PurchaseCoinPack(v, p));
        }

        foreach (var pair in _boosterViews)
        {
            var bp = pair.data;
            string title = localization != null ? localization.Get(bp.titleKey) : bp.titleKey;
            string price = localization != null ? localization.Format("shop_coin_cost", bp.coinCost) : $"{bp.coinCost} COINS";
            pair.view.Bind(title, string.Empty, $"x{bp.quantity}", price, bp.iconSprite, v => PurchaseBoosterPack(v, bp));
        }
    }

    private void PurchaseBundle(ShopOfferItemView view, ShopCatalogSO.BundleOffer bundle)
    {
        view.PlayPurchaseProcess(() =>
        {
            ProgressManager.Instance?.AddCoins(bundle.coinReward);
            ProgressManager.Instance?.AddBooster(BoosterType.SortQueue, bundle.sortBoosters);
            ProgressManager.Instance?.AddBooster(BoosterType.ShuffleColors, bundle.shuffleBoosters);
            ProgressManager.Instance?.AddBooster(BoosterType.VipSend, bundle.vipBoosters);
            PlayCoinFlyFrom(view.RewardFlyOrigin, 10);
            ShowToast("shop_purchase_success");
        });
    }

    private void PurchaseCoinPack(ShopOfferItemView view, ShopCatalogSO.CoinPackOffer pack)
    {
        view.PlayPurchaseProcess(() =>
        {
            ProgressManager.Instance?.AddCoins(pack.coinAmount);
            PlayCoinFlyFrom(view.RewardFlyOrigin, Mathf.Clamp(pack.coinAmount / 25, 4, 12));
            ShowToast("shop_purchase_success");
        });
    }

    private void PurchaseBoosterPack(ShopOfferItemView view, ShopCatalogSO.BoosterPackOffer pack)
    {
        if (ProgressManager.Instance == null || !ProgressManager.Instance.TrySpendCoins(pack.coinCost))
        {
            AudioManager.Instance?.PlayCrash();
            HapticManager.Instance?.TriggerWarning();
            ShowToast("not_enough_coins");
            return;
        }

        view.PlayPurchaseProcess(() =>
        {
            ProgressManager.Instance?.AddBooster(pack.boosterType, pack.quantity);
            AudioManager.Instance?.PlayUnlock();
            HapticManager.Instance?.TriggerSuccess();
            ShowToast("shop_purchase_success");
        });
    }

    private void PlayCoinFlyFrom(Transform origin, int count)
    {
        Transform target = mainHub != null ? mainHub.CoinTargetTransform : null;
        if (CoinFlyEffectManager.Instance != null && origin != null && target != null)
        {
            CoinFlyEffectManager.Instance.PlayFlyCoins(origin.position, target, count);
        }
        else
        {
            AudioManager.Instance?.PlayCoin();
        }
    }

    private void ShowToast(string key)
    {
        if (feedbackToastText == null) return;
        feedbackToastText.text = localization != null ? localization.Get(key) : key;
        _toastTween?.Kill();
        feedbackToastText.gameObject.SetActive(true);
        feedbackToastText.alpha = 1f;
        _toastTween = feedbackToastText.DOFade(0f, 0.35f).SetDelay(1.1f).SetUpdate(true).SetLink(gameObject)
            .OnComplete(() => feedbackToastText.gameObject.SetActive(false));
    }
}
