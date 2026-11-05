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
    [Header("Level Settings")]
    [Range(1, 5)] public int unlockedSlots = 3;
    [Range(5, 10)] public int coinReward = 10;

    [Header("Board Size")]
    public int rows = 6;
    public int cols = 6;

    [Header("Bus List")]
    public List<BusSpawnData> buses = new List<BusSpawnData>();

    [Header("Passenger Queue Colors (From Front to Back)")]
    public List<BusColor> passengerColors = new List<BusColor>();
}
