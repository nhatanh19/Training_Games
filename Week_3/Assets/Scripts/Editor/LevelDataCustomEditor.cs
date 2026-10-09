#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelDataSO))]
public class LevelDataCustomEditor : Editor
{
    private static readonly Color[] PresetColors = new Color[]
    {
        new Color(0.90f, 0.20f, 0.20f, 1f), // Đỏ
        new Color(0.20f, 0.80f, 0.20f, 1f), // Xanh lá
        new Color(0.20f, 0.50f, 0.95f, 1f), // Xanh dương
        new Color(0.95f, 0.85f, 0.10f, 1f), // Vàng
        new Color(0.65f, 0.20f, 0.85f, 1f), // Tím
        new Color(0.95f, 0.50f, 0.10f, 1f), // Cam
        new Color(0.00f, 0.85f, 0.90f, 1f)  // Xanh ngọc
    };

    private int _generateColorCount = 3;
    private int _generateEmptyBottles = 2;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LevelDataSO levelData = (LevelDataSO)target;

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("LEVEL EDITOR TOOLING", EditorStyles.boldLabel);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
        if (GUILayout.Button("Validate Level Logic", GUILayout.Height(32)))
        {
            ValidateLevel(levelData);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Sinh Màn Chơi Tự Động", EditorStyles.boldLabel);
        _generateColorCount = EditorGUILayout.IntSlider("Số lượng màu:", _generateColorCount, 2, PresetColors.Length);
        _generateEmptyBottles = EditorGUILayout.IntSlider("Số ống rỗng:", _generateEmptyBottles, 1, 3);

        GUI.backgroundColor = new Color(0.4f, 1f, 0.4f);
        if (GUILayout.Button("Generate Random Solvable Level", GUILayout.Height(32)))
        {
            GenerateRandomLevel(levelData, _generateColorCount, _generateEmptyBottles);
        }

        EditorGUILayout.Space(10);
        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("Clear All Data", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("Xác nhận", "Bạn có chắc chắn muốn xóa trắng dữ liệu màn này?", "Xóa", "Hủy"))
            {
                levelData.Bottles.Clear();
                EditorUtility.SetDirty(levelData);
            }
        }
        GUI.backgroundColor = Color.white;
    }

    private void ValidateLevel(LevelDataSO levelData)
    {
        if (levelData.Bottles == null || levelData.Bottles.Count == 0)
        {
            EditorUtility.DisplayDialog("Validation Error", "Level chưa có ống nghiệm nào!", "OK");
            return;
        }

        Dictionary<Color, int> colorCounts = new Dictionary<Color, int>();
        int emptyBottleCount = 0;
        bool hasPreSolvedBottle = false;

        foreach (var bottle in levelData.Bottles)
        {
            if (bottle.LayerColors.Count == 0)
            {
                emptyBottleCount++;
                continue;
            }

            if (bottle.LayerColors.Count > 4)
            {
                EditorUtility.DisplayDialog("Validation Error", "Có ống vượt quá sức chứa tối đa (4 tầng)!", "OK");
                return;
            }

            if (bottle.LayerColors.Count == 4)
            {
                Color first = bottle.LayerColors[0];
                bool allSame = true;
                foreach (var c in bottle.LayerColors)
                {
                    if (!AreColorsSimilar(c, first)) { allSame = false; break; }
                }
                if (allSame) hasPreSolvedBottle = true;
            }

            foreach (var color in bottle.LayerColors)
            {
                Color matchedKey = FindMatchingColorKey(colorCounts, color);
                if (matchedKey != Color.clear)
                {
                    colorCounts[matchedKey]++;
                }
                else
                {
                    colorCounts[color] = 1;
                }
            }
        }

        List<string> errorMessages = new List<string>();
        foreach (var kvp in colorCounts)
        {
            if (kvp.Value != 4)
            {
                errorMessages.Add($"- Màu {kvp.Key} có {kvp.Value}/4 tầng (không chia hết cho 4)");
            }
        }

        if (emptyBottleCount < 1)
        {
            errorMessages.Add("- Cần có ít nhất 1 ống rỗng để đảm bảo có thể giải được!");
        }

        if (hasPreSolvedBottle)
        {
            errorMessages.Add("- Cảnh báo: Có ống đã hoàn thành sẵn 4 tầng cùng màu ngay từ đầu!");
        }

        if (errorMessages.Count > 0)
        {
            string detail = string.Join("\n", errorMessages);
            EditorUtility.DisplayDialog("Validation FAILED", $"Level {levelData.LevelIndex} không hợp lệ:\n\n{detail}", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Validation SUCCESS", $"Level {levelData.LevelIndex} hoàn toàn hợp lệ!\n- Tổng số ống: {levelData.Bottles.Count}\n- Số màu: {colorCounts.Count}\n- Số ống rỗng: {emptyBottleCount}", "Tuyệt vời");
        }
    }

    private void GenerateRandomLevel(LevelDataSO levelData, int colorCount, int emptyCount)
    {
        List<Color> colorPool = new List<Color>();
        for (int i = 0; i < colorCount; i++)
        {
            Color c = PresetColors[i];
            for (int k = 0; k < 4; k++) colorPool.Add(c); // Mỗi màu đúng 4 đơn vị
        }

        for (int i = colorPool.Count - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            Color temp = colorPool[i];
            colorPool[i] = colorPool[rnd];
            colorPool[rnd] = temp;
        }

        List<BottleData> newBottles = new List<BottleData>();
        for (int i = 0; i < colorCount; i++)
        {
            List<Color> tubeColors = new List<Color>();
            for (int k = 0; k < 4; k++)
            {
                tubeColors.Add(colorPool[i * 4 + k]);
            }
            newBottles.Add(new BottleData(tubeColors));
        }
        for (int i = 0; i < emptyCount; i++)
        {
            newBottles.Add(new BottleData());
        }

        levelData.SetData(levelData.LevelIndex, newBottles);
        EditorUtility.SetDirty(levelData);
        EditorUtility.DisplayDialog("Thành Công", $"Đã sinh thành công level với {colorCount} màu và {emptyCount} ống rỗng!", "OK");
    }

    private bool AreColorsSimilar(Color c1, Color c2)
    {
        return Mathf.Abs(c1.r - c2.r) < 0.05f && Mathf.Abs(c1.g - c2.g) < 0.05f && Mathf.Abs(c1.b - c2.b) < 0.05f;
    }

    private Color FindMatchingColorKey(Dictionary<Color, int> dict, Color target)
    {
        foreach (var key in dict.Keys)
        {
            if (AreColorsSimilar(key, target)) return key;
        }
        return Color.clear;
    }
}
#endif