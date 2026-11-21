using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BusSpawnData
{
    public BusColor color;
    public Direction direction;
    [Range(2, 3)] public int length = 2;
    public Vector2Int rootGridPosition;
}

[CreateAssetMenu(fileName = "NewLevelData", menuName = "BusJam/Level Data")]
public class LevelDataSO : ScriptableObject
{
    [Header("Level Identity & Rewards")]
    public LevelDifficulty difficulty = LevelDifficulty.Normal;
    [Range(1, 7)] public int unlockedSlots = 5;
    [Min(0)] public int coinReward = 25;

    [Header("Board Size & Scaling")]
    [Min(2)] public int rows = 6;
    [Min(2)] public int cols = 6;
    [Tooltip("Optional per-level override to cap maximum auto-scaled cell size.")]
    public bool overrideMaxCellSize = false;
    [Min(0.3f)] public float customMaxCellSize = 1.25f;

    [Header("Bus List")]
    public List<BusSpawnData> buses = new List<BusSpawnData>();

    [Header("Passenger Queue Colors (From Front to Back)")]
    public List<BusColor> passengerColors = new List<BusColor>();
}
