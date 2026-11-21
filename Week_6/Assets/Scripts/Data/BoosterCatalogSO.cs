using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BoosterCatalog", menuName = "BusJam/Data/Booster Catalog")]
public class BoosterCatalogSO : ScriptableObject
{
    [Serializable]
    public class BoosterDefinition
    {
        public BoosterType type;
        public string nameKey;
        public string descriptionKey;
        public Sprite iconSprite;
        [Min(0)] public int startingCount = 2;
        [Min(1)] public int unlockLevel = 1;
        [Min(0)] public int refillCoinCost = 50;
    }

    [SerializeField] private List<BoosterDefinition> boosters = new List<BoosterDefinition>();

    public IReadOnlyList<BoosterDefinition> Boosters => boosters;

    public BoosterDefinition GetDefinition(BoosterType type)
    {
        for (int i = 0; i < boosters.Count; i++)
        {
            if (boosters[i] != null && boosters[i].type == type)
                return boosters[i];
        }
        return null;
    }
}
