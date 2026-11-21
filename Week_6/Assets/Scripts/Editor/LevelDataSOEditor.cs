#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelDataSO))]
public class LevelDataSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LevelDataSO level = (LevelDataSO)target;
        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Level Board Preview & Validation", EditorStyles.boldLabel);

        DrawGridPreview(level);
        DrawBalanceSummary(level);

        if (GUILayout.Button("Validate & Test BFS Solvability"))
        {
            bool ok = BusJamProjectValidator.ValidateSingleLevel(level, level.name);
            Debug.Log(ok
                ? $"<color=green>[LevelPreview] {level.name} is VALID and SOLVABLE!</color>"
                : $"<color=red>[LevelPreview] {level.name} FAILED validation (see Console).</color>");
        }
    }

    private static void DrawGridPreview(LevelDataSO level)
    {
        int cols = Mathf.Max(2, level.cols);
        int rows = Mathf.Max(2, level.rows);

        var cellTexts = new string[cols, rows];
        var cellColors = new Color[cols, rows];
        var overlapCells = new HashSet<Vector2Int>();

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                cellTexts[x, y] = ".";
                cellColors[x, y] = new Color(0.22f, 0.24f, 0.28f, 1f);
            }
        }

        if (level.buses != null)
        {
            for (int i = 0; i < level.buses.Count; i++)
            {
                BusSpawnData bus = level.buses[i];
                if (bus == null) continue;
                Vector2Int step = bus.direction.ToVector2Int();
                string dirArrow = bus.direction switch
                {
                    Direction.Right => "→",
                    Direction.Left => "←",
                    Direction.Up => "↑",
                    Direction.Down => "↓",
                    _ => "?"
                };

                Color c = GetPreviewColor(bus.color);
                int len = Mathf.Clamp(bus.length, 2, 3);
                for (int k = 0; k < len; k++)
                {
                    Vector2Int pos = bus.rootGridPosition + step * k;
                    if (pos.x < 0 || pos.x >= cols || pos.y < 0 || pos.y >= rows) continue;

                    if (cellTexts[pos.x, pos.y] != ".")
                    {
                        overlapCells.Add(pos);
                        cellTexts[pos.x, pos.y] = "X";
                        cellColors[pos.x, pos.y] = Color.black;
                    }
                    else
                    {
                        cellTexts[pos.x, pos.y] = k == len - 1 ? $"{i}{dirArrow}" : $"{i}";
                        cellColors[pos.x, pos.y] = c;
                    }
                }
            }
        }

        GUIStyle cellStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        for (int y = rows - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"y{y}", GUILayout.Width(24));
            for (int x = 0; x < cols; x++)
            {
                Color prevBg = GUI.backgroundColor;
                GUI.backgroundColor = cellColors[x, y];
                GUILayout.Box(cellTexts[x, y], cellStyle, GUILayout.Width(34), GUILayout.Height(28));
                GUI.backgroundColor = prevBg;
            }
            EditorGUILayout.EndHorizontal();
        }

        if (overlapCells.Count > 0)
        {
            EditorGUILayout.HelpBox($"Detected {overlapCells.Count} overlapping grid cell(s)!", MessageType.Error);
        }
    }

    private static void DrawBalanceSummary(LevelDataSO level)
    {
        int totalSeats = 0;
        var seatsByColor = new Dictionary<BusColor, int>();
        if (level.buses != null)
        {
            foreach (var b in level.buses)
            {
                if (b == null) continue;
                int len = Mathf.Clamp(b.length, 2, 3);
                totalSeats += len;
                seatsByColor.TryGetValue(b.color, out int s);
                seatsByColor[b.color] = s + len;
            }
        }

        int totalPax = level.passengerColors != null ? level.passengerColors.Count : 0;
        var paxByColor = new Dictionary<BusColor, int>();
        if (level.passengerColors != null)
        {
            foreach (var c in level.passengerColors)
            {
                paxByColor.TryGetValue(c, out int p);
                paxByColor[c] = p + 1;
            }
        }

        bool balanced = totalSeats == totalPax;
        foreach (BusColor c in System.Enum.GetValues(typeof(BusColor)))
        {
            seatsByColor.TryGetValue(c, out int s);
            paxByColor.TryGetValue(c, out int p);
            if (s != p) balanced = false;
        }

        string msg = $"Vehicles: {level.buses?.Count ?? 0} | Total Seats: {totalSeats} | Passengers: {totalPax} | Slots: {level.unlockedSlots}/7";
        EditorGUILayout.HelpBox(msg, balanced ? MessageType.Info : MessageType.Warning);
    }

    private static Color GetPreviewColor(BusColor c)
    {
        return c switch
        {
            BusColor.Red => new Color(0.85f, 0.22f, 0.22f),
            BusColor.Blue => new Color(0.18f, 0.45f, 0.92f),
            BusColor.Green => new Color(0.22f, 0.78f, 0.28f),
            BusColor.Yellow => new Color(0.85f, 0.68f, 0.10f),
            BusColor.Purple => new Color(0.65f, 0.25f, 0.88f),
            BusColor.Pink => new Color(0.92f, 0.28f, 0.62f),
            BusColor.Cyan => new Color(0.12f, 0.75f, 0.85f),
            _ => Color.gray
        };
    }
}
#endif
