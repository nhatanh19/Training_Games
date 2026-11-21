using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopCatalog", menuName = "BusJam/Data/Shop Catalog")]
public class ShopCatalogSO : ScriptableObject
{
    [Serializable]
    public class BundleOffer
    {
        public string id;
        public string titleKey;
        public string descriptionKey;
        public string priceLabel;
        public int coinReward = 250;
        public int sortBoosters = 2;
        public int shuffleBoosters = 2;
        public int vipBoosters = 1;
        public Sprite iconSprite;
    }

    [Serializable]
    public class CoinPackOffer
    {
        public string id;
        public string titleKey;
        public Sprite iconSprite;
        public int coinAmount = 100;
        public string priceLabel;
        [Tooltip("If > 0, simulates spending coins to claim booster/pack; if 0, grants demo reward.")]
        public int coinCost = 0;
        public bool requiresSimulatedAd = false;
    }

    [Serializable]
    public class BoosterPackOffer
    {
        public string id;
        public BoosterType boosterType;
        public string titleKey;
        public Sprite iconSprite;
        public int quantity = 1;
        public int coinCost = 50;
    }

    [Header("Featured Bundles")]
    [SerializeField] private List<BundleOffer> bundles = new List<BundleOffer>();

    [Header("Coin Packs")]
    [SerializeField] private List<CoinPackOffer> coinPacks = new List<CoinPackOffer>();

    [Header("Booster Packs (Soft Currency)")]
    [SerializeField] private List<BoosterPackOffer> boosterPacks = new List<BoosterPackOffer>();

    public IReadOnlyList<BundleOffer> Bundles => bundles;
    public IReadOnlyList<CoinPackOffer> CoinPacks => coinPacks;
    public IReadOnlyList<BoosterPackOffer> BoosterPacks => boosterPacks;
}
