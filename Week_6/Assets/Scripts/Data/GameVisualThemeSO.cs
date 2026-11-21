using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameVisualTheme", menuName = "BusJam/Data/Visual Theme")]
public class GameVisualThemeSO : ScriptableObject
{
    [Serializable]
    public class VehicleVisualEntry
    {
        public BusColor color;
        [Header("Grid Sprites (Side View)")]
        public Sprite sideCarSprite;
        public Sprite sideBusSprite;
        [Header("Parked Sprites (Diagonal 3/4 View)")]
        public Sprite diagCarSprite;
        public Sprite diagBusSprite;
        [Header("Optional Top/Down Override Sprites")]
        public Sprite upCarSprite;
        public Sprite downCarSprite;
        public Sprite upBusSprite;
        public Sprite downBusSprite;
        [Header("Tints")]
        public Color passengerTint = Color.white;
        public Color uiAccentColor = Color.white;
    }

    [Serializable]
    public class DifficultyVisualEntry
    {
        public LevelDifficulty difficulty;
        public Sprite homePlayButtonSprite;
        public Sprite hudLevelBadgeSprite;
        public string badgeTitleKey;
        public Color titleTextColor = Color.white;
    }

    [Header("Vehicle Visuals (7 Colors)")]
    [SerializeField] private List<VehicleVisualEntry> vehicles = new List<VehicleVisualEntry>();

    [Header("Difficulty Visuals (Normal / Hard / Super Hard)")]
    [SerializeField] private List<DifficultyVisualEntry> difficulties = new List<DifficultyVisualEntry>();

    [Header("Shared World & Currency Sprites")]
    [SerializeField] private Sprite directionArrowSprite;
    [SerializeField] private Sprite coinIconSprite;

    public IReadOnlyList<VehicleVisualEntry> Vehicles => vehicles;
    public IReadOnlyList<DifficultyVisualEntry> Difficulties => difficulties;
    public Sprite DirectionArrowSprite => directionArrowSprite;
    public Sprite CoinIconSprite => coinIconSprite;

    public VehicleVisualEntry GetVehicleEntry(BusColor color)
    {
        for (int i = 0; i < vehicles.Count; i++)
        {
            if (vehicles[i] != null && vehicles[i].color == color)
                return vehicles[i];
        }
        return null;
    }

    public DifficultyVisualEntry GetDifficultyEntry(LevelDifficulty difficulty)
    {
        for (int i = 0; i < difficulties.Count; i++)
        {
            if (difficulties[i] != null && difficulties[i].difficulty == difficulty)
                return difficulties[i];
        }
        return null;
    }

    public Sprite GetGridSprite(BusColor color, int length, Direction dir)
    {
        VehicleVisualEntry entry = GetVehicleEntry(color);
        if (entry == null) return null;

        bool isBus = length >= 3;
        switch (dir)
        {
            case Direction.Up:
                Sprite upSp = isBus ? entry.upBusSprite : entry.upCarSprite;
                return upSp != null ? upSp : (isBus ? entry.sideBusSprite : entry.sideCarSprite);
            case Direction.Down:
                Sprite downSp = isBus ? entry.downBusSprite : entry.downCarSprite;
                return downSp != null ? downSp : (isBus ? entry.sideBusSprite : entry.sideCarSprite);
            default:
                return isBus ? entry.sideBusSprite : entry.sideCarSprite;
        }
    }

    public Sprite GetDiagonalSprite(BusColor color, int length)
    {
        VehicleVisualEntry entry = GetVehicleEntry(color);
        if (entry == null) return null;
        return length >= 3 ? entry.diagBusSprite : entry.diagCarSprite;
    }

    public Color GetPassengerTint(BusColor color)
    {
        VehicleVisualEntry entry = GetVehicleEntry(color);
        return entry != null ? entry.passengerTint : Color.white;
    }
}
