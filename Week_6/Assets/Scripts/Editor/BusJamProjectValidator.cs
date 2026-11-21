#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BusJamProjectValidator
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string ThemePath = "Assets/Data/GameVisualTheme.asset";
    private const string GridProfilePath = "Assets/Data/GridLayoutProfile.asset";
    private const string LocalizationPath = "Assets/Data/LocalizationTable.asset";

    [MenuItem("Tools/Bus Jam/Validate Project Integrity", false, 50)]
    public static void ValidateFromMenu()
    {
        RunValidation(exitBatchmodeOnFinish: false);
    }

    public static void RunBatchValidation()
    {
        RunValidation(exitBatchmodeOnFinish: true);
    }

    private static void RunValidation(bool exitBatchmodeOnFinish)
    {
        int failures = 0;
        Debug.Log("=== [BUS JAM PROJECT VALIDATOR] STARTING ===");

        failures += CheckForbiddenEditorFiles();
        failures += CheckRuntimeStaticAudit();

        if (!SceneManager.GetActiveScene().path.Equals(ScenePath, StringComparison.OrdinalIgnoreCase))
        {
            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            else
            {
                Debug.LogError($"[Validator] FAIL: Scene missing at {ScenePath}");
                failures++;
            }
        }

        failures += CheckVisualTheme();
        failures += CheckLocalizationBindings();
        failures += CheckSceneSerializedReferences();
        failures += CheckAllLevels();
        failures += CheckDynamicGridAutoScaling();

        if (failures == 0)
        {
            Debug.Log("<color=green>=== [BUS JAM PROJECT VALIDATOR] ALL CHECKS PASSED (0 FAILURES) ===</color>");
        }
        else
        {
            Debug.LogError($"=== [BUS JAM PROJECT VALIDATOR] FAILED WITH {failures} ERROR(S) ===");
        }

        if (exitBatchmodeOnFinish)
        {
            EditorApplication.Exit(failures == 0 ? 0 : 1);
        }
    }

    private static int CheckForbiddenEditorFiles()
    {
        int failures = 0;
        string editorDir = "Assets/Scripts/Editor";
        if (!Directory.Exists(editorDir)) return 0;

        string[] forbiddenPatterns =
        {
            "BusJamAssetBuilder.cs",
            "HUDBuilder.cs",
            "WinPopupBuilder.cs",
            "LosePopupBuilder.cs",
            "ContinuePopupBuilder.cs",
            "SettingsPopupBuilder.cs",
            "BusJamSceneSetup.cs",
            "BusJamSceneWiring.cs",
            "OneTimeBusJamSetupTool.cs"
        };

        foreach (string file in forbiddenPatterns)
        {
            string full = Path.Combine(editorDir, file);
            if (File.Exists(full))
            {
                Debug.LogError($"[Validator] FAIL: Forbidden UI-builder script still exists: {full}");
                failures++;
            }
        }

        if (failures == 0)
        {
            Debug.Log("[Validator] PASS: Zero UI-builder or one-time setup scripts present.");
        }
        return failures;
    }

    private static int CheckRuntimeStaticAudit()
    {
        int failures = 0;
        string[] files = Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories);
        Regex newGoRegex = new Regex(@"\bnew\s+GameObject\b");
        Regex addCompRegex = new Regex(@"\bAddComponent\s*<");
        Regex resLoadRegex = new Regex(@"\bResources\.Load\b");
        Regex goFindRegex = new Regex(@"\bGameObject\.Find\b");
        Regex trFindRegex = new Regex(@"\.Find\s*\(\s*""");

        foreach (string path in files)
        {
            string normalized = path.Replace("\\", "/");
            if (normalized.Contains("/Editor/")) continue;

            string content = File.ReadAllText(path);
            if (newGoRegex.IsMatch(content))
            {
                Debug.LogError($"[Validator] FAIL: Forbidden 'new GameObject' in runtime script: {normalized}");
                failures++;
            }
            if (addCompRegex.IsMatch(content))
            {
                Debug.LogError($"[Validator] FAIL: Forbidden 'AddComponent<' in runtime script: {normalized}");
                failures++;
            }
            if (resLoadRegex.IsMatch(content))
            {
                Debug.LogError($"[Validator] FAIL: Forbidden 'Resources.Load' in runtime script: {normalized}");
                failures++;
            }
            if (goFindRegex.IsMatch(content))
            {
                Debug.LogError($"[Validator] FAIL: Forbidden 'GameObject.Find' in runtime script: {normalized}");
                failures++;
            }
            if (trFindRegex.IsMatch(content))
            {
                Debug.LogError($"[Validator] FAIL: Forbidden 'transform.Find(\"...\")' in runtime script: {normalized}");
                failures++;
            }
        }

        if (failures == 0)
        {
            Debug.Log("[Validator] PASS: Runtime static audit clean (0 UI-hardcode/creation calls).");
        }
        return failures;
    }

    private static int CheckVisualTheme()
    {
        int failures = 0;
        GameVisualThemeSO theme = AssetDatabase.LoadAssetAtPath<GameVisualThemeSO>(ThemePath);
        if (theme == null)
        {
            Debug.LogError($"[Validator] FAIL: Missing {ThemePath}");
            return 1;
        }

        foreach (BusColor c in Enum.GetValues(typeof(BusColor)))
        {
            var entry = theme.GetVehicleEntry(c);
            if (entry == null || entry.sideCarSprite == null || entry.sideBusSprite == null ||
                entry.diagCarSprite == null || entry.diagBusSprite == null)
            {
                Debug.LogError($"[Validator] FAIL: GameVisualTheme missing sprite(s) for BusColor.{c}");
                failures++;
            }
        }

        foreach (LevelDifficulty diff in Enum.GetValues(typeof(LevelDifficulty)))
        {
            var entry = theme.GetDifficultyEntry(diff);
            if (entry == null || entry.homePlayButtonSprite == null || entry.hudLevelBadgeSprite == null)
            {
                Debug.LogError($"[Validator] FAIL: GameVisualTheme missing sprites for LevelDifficulty.{diff}");
                failures++;
            }
        }

        if (theme.DirectionArrowSprite == null || theme.CoinIconSprite == null)
        {
            Debug.LogError("[Validator] FAIL: GameVisualTheme missing DirectionArrowSprite or CoinIconSprite.");
            failures++;
        }

        if (failures == 0)
        {
            Debug.Log("[Validator] PASS: GameVisualThemeSO complete for all 7 colors and 3 difficulties.");
        }
        return failures;
    }

    private static int CheckLocalizationBindings()
    {
        int failures = 0;
        LocalizationTableSO table = AssetDatabase.LoadAssetAtPath<LocalizationTableSO>(LocalizationPath);
        if (table == null)
        {
            Debug.LogError($"[Validator] FAIL: Missing {LocalizationPath}");
            return 1;
        }

        LocalizedTMPText[] texts = UnityEngine.Object.FindObjectsOfType<LocalizedTMPText>(true);
        foreach (LocalizedTMPText lt in texts)
        {
            SerializedObject so = new SerializedObject(lt);
            string key = so.FindProperty("localizationKey").stringValue;
            if (string.IsNullOrEmpty(key)) continue;

            string vi = table.Get(key, true);
            string en = table.Get(key, false);
            if (vi == key || en == key || string.IsNullOrEmpty(vi) || string.IsNullOrEmpty(en))
            {
                Debug.LogError($"[Validator] FAIL: Localization key '{key}' on '{lt.name}' missing VI/EN translation.");
                failures++;
            }
        }

        if (failures == 0)
        {
            Debug.Log($"[Validator] PASS: All {texts.Length} LocalizedTMPText bindings verified in LocalizationTableSO.");
        }
        return failures;
    }

    private static int CheckSceneSerializedReferences()
    {
        int failures = 0;

        failures += VerifyComponentFields<GameBootstrap>();
        failures += VerifyComponentFields<AppFlowController>();
        failures += VerifyComponentFields<MainHubController>();
        failures += VerifyComponentFields<HomeScreenController>();
        failures += VerifyComponentFields<ShopScreenController>(allowNullFields: new[] { "feedbackToastText" });
        failures += VerifyComponentFields<CollectionScreenController>();
        failures += VerifyComponentFields<LoadingScreenController>();
        failures += VerifyComponentFields<HUDController>();
        failures += VerifyComponentFields<BoosterBarController>(allowNullFields: new[] { "feedbackText" });
        failures += VerifyComponentFields<UIManager>();
        failures += VerifyComponentFields<SettingsController>();
        failures += VerifyComponentFields<WinPopupController>();
        failures += VerifyComponentFields<ContinuePopupController>();
        failures += VerifyComponentFields<LosePopupController>(allowNullFields: new[] { "reviveButton" });
        failures += VerifyComponentFields<GridManager>();
        failures += VerifyComponentFields<BusStopManager>();
        failures += VerifyComponentFields<PassengerQueueManager>();
        failures += VerifyComponentFields<LevelLoader>(allowNullFields: new[] { "currentLevelData" });
        failures += VerifyComponentFields<AudioManager>();
        failures += VerifyComponentFields<VFXManager>();
        failures += VerifyComponentFields<CoinFlyEffectManager>();
        failures += VerifyComponentFields<CameraViewportFitter>();

        BusStopManager bsm = UnityEngine.Object.FindObjectOfType<BusStopManager>(true);
        if (bsm == null || bsm.Slots == null || bsm.Slots.Count != 7)
        {
            Debug.LogError($"[Validator] FAIL: BusStopManager must have exactly 7 diagonal slots (found {bsm?.Slots?.Count ?? 0}).");
            failures++;
        }

        PassengerQueueManager pqm = UnityEngine.Object.FindObjectOfType<PassengerQueueManager>(true);
        if (pqm == null || pqm.Waypoints == null || pqm.Waypoints.Count < 4)
        {
            Debug.LogError($"[Validator] FAIL: PassengerQueueManager must have >= 4 waypoints (found {pqm?.Waypoints?.Count ?? 0}).");
            failures++;
        }

        LevelLoader loader = UnityEngine.Object.FindObjectOfType<LevelLoader>(true);
        if (loader == null || loader.LevelCount < 10)
        {
            Debug.LogError($"[Validator] FAIL: LevelLoader must have 10 levels (found {loader?.LevelCount ?? 0}).");
            failures++;
        }

        if (failures == 0)
        {
            Debug.Log("[Validator] PASS: All scene controllers, managers, 7 bays, waypoints, and UI references wired.");
        }
        return failures;
    }

    private static int VerifyComponentFields<T>(string[] allowNullFields = null) where T : MonoBehaviour
    {
        T comp = UnityEngine.Object.FindObjectOfType<T>(true);
        if (comp == null)
        {
            Debug.LogError($"[Validator] FAIL: Component {typeof(T).Name} not found in scene.");
            return 1;
        }

        int failures = 0;
        var allowed = allowNullFields != null ? new HashSet<string>(allowNullFields) : new HashSet<string>();
        SerializedObject so = new SerializedObject(comp);
        SerializedProperty prop = so.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (prop.name == "m_Script") continue;
            if (allowed.Contains(prop.name)) continue;

            if (prop.propertyType == SerializedPropertyType.ObjectReference && prop.objectReferenceValue == null)
            {
                Debug.LogError($"[Validator] FAIL: {typeof(T).Name}.{prop.name} is null on '{comp.name}'.");
                failures++;
            }
        }
        return failures;
    }

    private static int CheckAllLevels()
    {
        int failures = 0;
        string[] guids = AssetDatabase.FindAssets("t:LevelDataSO", new[] { "Assets/Levels" });
        Array.Sort(guids);

        if (guids.Length < 10)
        {
            Debug.LogError($"[Validator] FAIL: Expected >= 10 LevelDataSO assets in Assets/Levels, found {guids.Length}.");
            failures++;
        }

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            LevelDataSO level = AssetDatabase.LoadAssetAtPath<LevelDataSO>(path);
            if (level == null) continue;

            if (!ValidateSingleLevel(level, path))
            {
                failures++;
            }
        }

        if (failures == 0)
        {
            Debug.Log($"[Validator] PASS: All {guids.Length} levels verified (bounds, zero overlap, color parity, BFS solvable).");
        }
        return failures;
    }

    public static bool ValidateSingleLevel(LevelDataSO level, string label)
    {
        var occupied = new HashSet<Vector2Int>();
        var seatCountByColor = new Dictionary<BusColor, int>();
        var paxCountByColor = new Dictionary<BusColor, int>();

        foreach (BusSpawnData bus in level.buses)
        {
            if (bus == null) return false;
            Vector2Int step = bus.direction.ToVector2Int();
            int len = Mathf.Clamp(bus.length, 2, 3);
            for (int k = 0; k < len; k++)
            {
                Vector2Int cell = bus.rootGridPosition + step * k;
                if (cell.x < 0 || cell.x >= level.cols || cell.y < 0 || cell.y >= level.rows)
                {
                    Debug.LogError($"[Validator] FAIL ({label}): Bus at {bus.rootGridPosition} ({bus.direction}, len={len}) exceeds grid {level.cols}x{level.rows} at {cell}.");
                    return false;
                }
                if (!occupied.Add(cell))
                {
                    Debug.LogError($"[Validator] FAIL ({label}): Overlapping vehicle cell at {cell}.");
                    return false;
                }
            }

            seatCountByColor.TryGetValue(bus.color, out int sCount);
            seatCountByColor[bus.color] = sCount + len;
        }

        foreach (BusColor c in level.passengerColors)
        {
            paxCountByColor.TryGetValue(c, out int pCount);
            paxCountByColor[c] = pCount + 1;
        }

        foreach (BusColor c in Enum.GetValues(typeof(BusColor)))
        {
            seatCountByColor.TryGetValue(c, out int seats);
            paxCountByColor.TryGetValue(c, out int pax);
            if (seats != pax)
            {
                Debug.LogError($"[Validator] FAIL ({label}): Color {c} mismatch — seats={seats}, passengers={pax}.");
                return false;
            }
        }

        if (!IsLevelSolvable(level))
        {
            Debug.LogError($"[Validator] FAIL ({label}): BFS solver found NO winning sequence with {level.unlockedSlots} unlocked slots!");
            return false;
        }

        return true;
    }

    private static bool IsLevelSolvable(LevelDataSO level)
    {
        int n = level.buses.Count;
        int slots = Mathf.Clamp(level.unlockedSlots, 1, 7);
        int totalPax = level.passengerColors.Count;
        if (n == 0 && totalPax == 0) return true;

        var cellOwner = new int[level.cols, level.rows];
        for (int x = 0; x < level.cols; x++)
            for (int y = 0; y < level.rows; y++)
                cellOwner[x, y] = -1;

        for (int i = 0; i < n; i++)
        {
            BusSpawnData b = level.buses[i];
            Vector2Int step = b.direction.ToVector2Int();
            for (int k = 0; k < b.length; k++)
            {
                Vector2Int c = b.rootGridPosition + step * k;
                cellOwner[c.x, c.y] = i;
            }
        }

        bool IsBusClear(int busIdx, uint gridMask)
        {
            BusSpawnData b = level.buses[busIdx];
            Vector2Int step = b.direction.ToVector2Int();
            Vector2Int pos = b.rootGridPosition + step * b.length;
            while (pos.x >= 0 && pos.x < level.cols && pos.y >= 0 && pos.y < level.rows)
            {
                int occ = cellOwner[pos.x, pos.y];
                if (occ >= 0 && (gridMask & (1u << occ)) != 0) return false;
                pos += step;
            }
            return true;
        }

        uint initialMask = n >= 32 ? uint.MaxValue : ((1u << n) - 1u);
        var initialParked = new List<(int color, int rem)>();
        var visited = new HashSet<string>();
        var queue = new Queue<(uint mask, List<(int color, int rem)> parked, int qIdx)>();

        void AdvanceBoarding(List<(int color, int rem)> parked, ref int qIdx)
        {
            bool changed = true;
            while (changed && qIdx < totalPax)
            {
                changed = false;
                int wantColor = (int)level.passengerColors[qIdx];
                for (int p = 0; p < parked.Count; p++)
                {
                    if (parked[p].color == wantColor && parked[p].rem > 0)
                    {
                        var updated = (parked[p].color, parked[p].rem - 1);
                        if (updated.Item2 <= 0) parked.RemoveAt(p);
                        else parked[p] = updated;
                        qIdx++;
                        changed = true;
                        break;
                    }
                }
            }
        }

        string MakeKey(uint mask, List<(int color, int rem)> parked, int qIdx)
        {
            var copy = new List<(int color, int rem)>(parked);
            copy.Sort((a, b) => a.color != b.color ? a.color.CompareTo(b.color) : a.rem.CompareTo(b.rem));
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(mask).Append('|').Append(qIdx).Append('|');
            for (int i = 0; i < copy.Count; i++)
            {
                sb.Append(copy[i].color).Append(':').Append(copy[i].rem).Append(',');
            }
            return sb.ToString();
        }

        queue.Enqueue((initialMask, initialParked, 0));
        int explored = 0;
        const int maxStates = 20000;

        while (queue.Count > 0 && explored < maxStates)
        {
            var state = queue.Dequeue();
            explored++;

            if (state.mask == 0 && state.parked.Count == 0 && state.qIdx >= totalPax)
            {
                return true;
            }

            if (state.parked.Count >= slots) continue;

            for (int i = 0; i < n; i++)
            {
                if ((state.mask & (1u << i)) == 0) continue;
                if (!IsBusClear(i, state.mask)) continue;

                uint nextMask = state.mask & ~(1u << i);
                var nextParked = new List<(int color, int rem)>(state.parked)
                {
                    ((int)level.buses[i].color, level.buses[i].length)
                };
                int nextQ = state.qIdx;
                AdvanceBoarding(nextParked, ref nextQ);

                if (nextMask == 0 && nextParked.Count == 0 && nextQ >= totalPax)
                {
                    return true;
                }

                string key = MakeKey(nextMask, nextParked, nextQ);
                if (visited.Add(key))
                {
                    queue.Enqueue((nextMask, nextParked, nextQ));
                }
            }
        }

        return false;
    }

    private static int CheckDynamicGridAutoScaling()
    {
        int failures = 0;
        PlayfieldBounds bounds = UnityEngine.Object.FindObjectOfType<PlayfieldBounds>(true);
        GridLayoutProfileSO profile = AssetDatabase.LoadAssetAtPath<GridLayoutProfileSO>(GridProfilePath);
        LevelDataSO lvl1 = AssetDatabase.LoadAssetAtPath<LevelDataSO>("Assets/Levels/Level_1.asset");
        LevelDataSO lvl5 = AssetDatabase.LoadAssetAtPath<LevelDataSO>("Assets/Levels/Level_5.asset");
        LevelDataSO lvl10 = AssetDatabase.LoadAssetAtPath<LevelDataSO>("Assets/Levels/Level_10.asset");

        if (bounds == null || profile == null || lvl1 == null || lvl5 == null || lvl10 == null)
        {
            Debug.LogError("[Validator] FAIL: Missing PlayfieldBounds, GridLayoutProfileSO, or Level_1/5/10 for scaling check.");
            return 1;
        }

        Rect rect = bounds.WorldRect;
        var l1 = GridManager.ComputeLayout(lvl1, rect, profile);
        var l5 = GridManager.ComputeLayout(lvl5, rect, profile);
        var l10 = GridManager.ComputeLayout(lvl10, rect, profile);

        Debug.Log($"[Validator] Dynamic Grid Scaling: Lv1(small) cell={l1.CellSize:F3} scale={l1.VehicleScaleFactor:F2} | Lv5(6x6) cell={l5.CellSize:F3} scale={l5.VehicleScaleFactor:F2} | Lv10(8x8) cell={l10.CellSize:F3} scale={l10.VehicleScaleFactor:F2}");

        if (!(l1.CellSize > l5.CellSize && l5.CellSize > l10.CellSize))
        {
            Debug.LogError("[Validator] FAIL: Expected Level_1.CellSize > Level_5.CellSize > Level_10.CellSize.");
            failures++;
        }

        foreach (var lay in new[] { l1, l5, l10 })
        {
            if (lay.CellSize < profile.minCellSize - 0.001f || lay.CellSize > profile.maxCellSize + 0.001f)
            {
                Debug.LogError($"[Validator] FAIL: CellSize {lay.CellSize:F3} outside [{profile.minCellSize}, {profile.maxCellSize}].");
                failures++;
            }

            if (lay.LeftLaneX < rect.xMin || lay.RightLaneX > rect.xMax ||
                lay.BottomLaneY < rect.yMin || lay.TopLaneY > rect.yMax)
            {
                Debug.LogError("[Validator] FAIL: Computed road corridor exceeds PlayfieldBounds.");
                failures++;
            }
        }

        if (failures == 0)
        {
            Debug.Log("[Validator] PASS: Dynamic Grid Auto-Scaling verified across small/medium/large levels.");
        }
        return failures;
    }
}
#endif
