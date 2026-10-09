using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelDatabase", menuName = "WaterSort/Level Database")]
public class LevelDatabaseSO : ScriptableObject
{
    [Header("All Game Levels")]
    [SerializeField] private List<LevelDataSO> _levels = new List<LevelDataSO>();
    public int TotalLevels => _levels.Count;

    public LevelDataSO GetLevel (int levelIndex)
    {
        if (_levels == null || _levels.Count == 0){
            Debug.LogError("[LevelDatabaseSO] Danh sách Levels đang trống!");
            return null;
        }

        int targetIndex = (levelIndex - 1) % _levels.Count;
        if(targetIndex < 0) targetIndex = 0;

        return _levels[targetIndex];
    }
}

