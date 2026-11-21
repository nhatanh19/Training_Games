using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CollectionCatalog", menuName = "BusJam/Data/Collection Catalog")]
public class CollectionCatalogSO : ScriptableObject
{
    [Serializable]
    public class CollectionEntry
    {
        public string id;
        public string titleKey;
        public BusColor vehicleColor;
        public Sprite vehicleSprite;
        public Sprite lockedOverlaySprite;
        [Min(1)] public int unlockAtLevel = 1;
        [Min(0)] public int claimCoinReward = 30;
        public bool comingSoon = false;
    }

    [SerializeField] private List<CollectionEntry> entries = new List<CollectionEntry>();

    public IReadOnlyList<CollectionEntry> Entries => entries;
}
