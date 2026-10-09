using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    private const string LEVEL_SAVE_KEY = "CURRENT_LEVEL_INDEX";
    
    [Header("Database Reference")]
    [SerializeField] private LevelDatabaseSO _levelDatabase;
    [Header("Layout Settings")]
    [SerializeField] private float _rowSpacing = 2.8f;   
    [SerializeField] private float _bottleSpacing = 1.3f; 
    private int _currentLevelIndex = 1;
    private List<BottleController> _activeBottles = new List<BottleController>();

    public int CurrentLevelIndex => _currentLevelIndex;
    public List<BottleController> ActiveBottles => _activeBottles;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        _currentLevelIndex = PlayerPrefs.GetInt(LEVEL_SAVE_KEY, 1);
    }
    void Start()
    {
        LoadLevel(_currentLevelIndex);
    }

    public void LoadLevel(int levelIndex)
    {
        _currentLevelIndex = levelIndex;
        PlayerPrefs.SetInt(LEVEL_SAVE_KEY, _currentLevelIndex);
        PlayerPrefs.Save();

        BottlePoolManager.Instance.ReturnAll(_activeBottles);
        
        LevelDataSO levelData = _levelDatabase.GetLevel(_currentLevelIndex);
        if (levelData == null || levelData.Bottles.Count == 0)
        {
            Debug.LogError($"[LevelManager] Không tìm thấy dữ liệu cho Level {_currentLevelIndex}");
            return;
        }
        
        int totalBottles = levelData.Bottles.Count;
        List<Vector3> positions = CalculateBottlePositions(totalBottles);
        
        for (int i = 0; i < totalBottles; i++)
        {
            BottleController bottle = BottlePoolManager.Instance.GetBottle();
            bottle.name = $"Bottle_{i + 1}";
            bottle.SetupBottle(positions[i], levelData.Bottles[i].LayerColors);
            _activeBottles.Add(bottle);
        }
        
        WaterSortController.Instance.OnLevelLoaded(_activeBottles);
    }

    public void NextLevel()
    {
        LoadLevel(_currentLevelIndex + 1);
    }

    public void RestartCurrentLevel()
    {
        LoadLevel(_currentLevelIndex);
    }

    public void ResetToLevelOne()
    {
        _currentLevelIndex = 1;
        PlayerPrefs.SetInt(LEVEL_SAVE_KEY, 1);
        PlayerPrefs.Save();
        LoadLevel(1);
    }

    private List<Vector3> CalculateBottlePositions(int count)
    {
        List<Vector3> positions = new List<Vector3>();
        if (count <= 4)
        {
            float startX = -((count - 1) * _bottleSpacing) / 2f;
            for (int i = 0; i < count; i++)
            {
                positions.Add(new Vector3(startX + (i * _bottleSpacing), 0f, 0f));
            }
        }
        else
        {
            int topRowCount = Mathf.CeilToInt(count / 2f);
            int bottomRowCount = count - topRowCount;
            float topY = _rowSpacing / 2f;
            float bottomY = -_rowSpacing / 2f;
            // Hàng trên
            float startXTop = -((topRowCount - 1) * _bottleSpacing) / 2f;
            for (int i = 0; i < topRowCount; i++)
            {
                positions.Add(new Vector3(startXTop + (i * _bottleSpacing), topY, 0f));
            }
            // Hàng dưới
            float startXBottom = -((bottomRowCount - 1) * _bottleSpacing) / 2f;
            for (int i = 0; i < bottomRowCount; i++)
            {
                positions.Add(new Vector3(startXBottom + (i * _bottleSpacing), bottomY, 0f));
            }
        }
        return positions;
    }

}