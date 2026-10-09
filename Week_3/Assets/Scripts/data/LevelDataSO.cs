using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelData_", menuName = "WaterSort/Level Data")]
public class LevelDataSO : ScriptableObject
{
    [Header("Level Configuration")]
    [SerializeField] private int _levelIndex = 1;
    [SerializeField] private List<BottleData> _bottles = new List<BottleData>();

    public int LevelIndex => _levelIndex;
    public List<BottleData> Bottles => _bottles;

    public void SetData(int levelIndex, List<BottleData> bottles)
    {
        _levelIndex = levelIndex;
        _bottles = bottles;
    }
}