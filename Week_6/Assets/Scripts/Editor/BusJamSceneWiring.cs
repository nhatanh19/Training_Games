#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BusJamSceneWiring
{
    private const string VFXPrefabPath = "Assets/Prefabs/UI/VFXCanvas.prefab";
    private const string CoinPrefabPath = "Assets/Prefabs/UI/CoinItem.prefab";
    private const string CoinPoolName = "CoinPool";
    private const string MusicFolder = "Assets/Audio/Music";

    [MenuItem("Tools/Bus Jam/Wire Scene UI References", false, 23)]
    public static void WireFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Bus Jam] Exit Play Mode before wiring scene references.");
            return;
        }

        if (WireSceneReferences(true))
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.isLoaded && scene.isDirty)
            {
                EditorSceneManager.SaveScene(scene);
            }
        }
    }

    public static bool WireSceneReferences(bool verbose = false)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded) return false;

        UIManager uiManager = Object.FindObjectOfType<UIManager>();
        if (uiManager == null)
        {
            if (verbose) Debug.LogError("[Bus Jam] Không có UIManager trong scene — bỏ qua bước nối reference.");
            return false;
        }

        HUDController hud = Object.FindObjectOfType<HUDController>();
        Transform popupCanvas = FindObject("PopupCanvas");

        GameObject win = Child(popupCanvas, "WinPopup");
        GameObject lose = Child(popupCanvas, "LosePopup");
        GameObject settings = Child(popupCanvas, "SettingsPopup");
        GameObject continuePopup = Child(popupCanvas, "ContinuePopup");

        SerializedObject so = new SerializedObject(uiManager);
        bool changed = false;
        changed |= Assign(so, "_hudController", hud);
        changed |= Assign(so, "_winPopup", win);
        changed |= Assign(so, "_losePopup", lose);
        changed |= Assign(so, "_settingsPopup", settings);
        changed |= Assign(so, "_continuePopup", continuePopup);
        changed |= Assign(so, "_nextLevelButton", ButtonIn(win, "Card/MainButton"));
        changed |= Assign(so, "_retryLoseButton", ButtonIn(lose, "Card/MainButton"));

        if (changed)
        {
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scene);
        }

        if (verbose)
        {
            ReportMissing("HUDController", hud);
            ReportMissing("PopupCanvas", popupCanvas != null);
            ReportMissing("WinPopup", win);
            ReportMissing("LosePopup", lose);
            ReportMissing("SettingsPopup", settings);
            ReportMissing("ContinuePopup", continuePopup);
            ReportMissing("WinPopup/Card/MainButton", ButtonIn(win, "Card/MainButton"));
            ReportMissing("LosePopup/Card/MainButton", ButtonIn(lose, "Card/MainButton"));

            Debug.Log(changed
                ? "[Bus Jam] Đã nối reference UI vào UIManager và lưu scene."
                : "[Bus Jam] Reference UI đã đúng sẵn, không cần thay đổi.");
        }

        return changed;
    }

    [MenuItem("Tools/Bus Jam/Install Coin Fly Effect On VFXCanvas", false, 25)]
    public static void InstallCoinFlyEffectFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Bus Jam] Exit Play Mode before editing the VFXCanvas prefab.");
            return;
        }

        InstallCoinFlyEffect(true);
    }

    public static bool InstallCoinFlyEffect(bool verbose = false)
    {
        GameObject coinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoinPrefabPath);
        if (coinPrefab == null)
        {
            Debug.LogError($"[Bus Jam] Không tìm thấy {CoinPrefabPath} — chạy Tools > Bus Jam > Build Coin Prefab trước.");
            return false;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(VFXPrefabPath);
        if (root == null)
        {
            Debug.LogError($"[Bus Jam] Không mở được {VFXPrefabPath}.");
            return false;
        }

        try
        {
            CoinFlyEffectManager manager = root.GetComponent<CoinFlyEffectManager>();
            if (manager == null)
            {
                manager = root.AddComponent<CoinFlyEffectManager>();
            }

            RectTransform pool = root.transform.Find(CoinPoolName) as RectTransform;
            if (pool == null)
            {
                GameObject poolGo = new GameObject(CoinPoolName, typeof(RectTransform));
                poolGo.transform.SetParent(root.transform, false);
                pool = poolGo.GetComponent<RectTransform>();
            }

            SerializedObject so = new SerializedObject(manager);
            bool changed = false;
            changed |= Assign(so, "_coinPrefab", coinPrefab);
            changed |= Assign(so, "_poolContainer", pool);
            if (changed) so.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(root, VFXPrefabPath);

            if (verbose)
            {
                Debug.Log(changed
                    ? $"[Bus Jam] Đã gắn CoinFlyEffectManager + node {CoinPoolName} vào {VFXPrefabPath}."
                    : $"[Bus Jam] {VFXPrefabPath} đã có CoinFlyEffectManager với đủ reference.");
            }

            return changed;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    [MenuItem("Tools/Bus Jam/Wire Background Music", false, 26)]
    public static void WireMusicFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Bus Jam] Exit Play Mode before wiring the background music reference.");
            return;
        }

        if (WireBackgroundMusic(true))
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.isLoaded && scene.isDirty)
            {
                EditorSceneManager.SaveScene(scene);
            }
        }
    }

    public static bool WireBackgroundMusic(bool verbose = false)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded) return false;

        AudioManager audio = Object.FindObjectOfType<AudioManager>();
        if (audio == null)
        {
            if (verbose) Debug.LogError("[Bus Jam] Không có AudioManager trong scene — bỏ qua bước nối nhạc nền.");
            return false;
        }

        AudioClip bgm = FirstAudioClip(MusicFolder);
        if (bgm == null)
        {
            if (verbose) Debug.LogError($"[Bus Jam] Không có AudioClip nào trong {MusicFolder} — copy file nhạc vào folder đó rồi chạy Tools > Bus Jam > Wire Background Music.");
            return false;
        }

        AudioSource[] sources = audio.GetComponents<AudioSource>();
        bool changed = false;

        if (sources.Length < 2)
        {
            AudioSource extra = Undo.AddComponent(audio.gameObject, typeof(AudioSource)) as AudioSource;
            extra.loop = true;
            extra.playOnAwake = false;
            extra.spatialBlend = 0f;
            EditorUtility.SetDirty(extra);
            sources = audio.GetComponents<AudioSource>();
            changed = true;
        }

        SerializedObject so = new SerializedObject(audio);
        changed |= Assign(so, "_sfxSource", sources[0]);
        changed |= Assign(so, "_bgmSource", sources[1]);
        changed |= Assign(so, "_bgmClip", bgm);
        if (changed) so.ApplyModifiedProperties();

        if (changed) EditorSceneManager.MarkSceneDirty(scene);

        if (verbose)
        {
            Debug.Log(changed
                ? $"[Bus Jam] Đã nối nhạc nền: _bgmClip = {bgm.name}, _sfxSource + _bgmSource = 2 AudioSource trên '{audio.gameObject.name}'."
                : $"[Bus Jam] Nhạc nền đã được nối sẵn với {bgm.name}.");
        }

        return changed;
    }

    [MenuItem("Tools/Bus Jam/Validate Scene Wiring", false, 24)]
    public static void ValidateFromMenu()
    {
        int failures = 0;

        failures += Report("Canvas HUDCanvas tồn tại trong scene", CanvasCheck("HUDCanvas"));
        failures += Report("Canvas PopupCanvas tồn tại trong scene", CanvasCheck("PopupCanvas"));
        failures += Report("Canvas VFXCanvas tồn tại trong scene", CanvasCheck("VFXCanvas"));
        failures += Report("LevelLoader.levels có level và không có phần tử null", LevelsCheck());
        failures += Report("UIManager có đủ 7 reference", UIManagerCheck());
        failures += Report("VFXCanvas có CoinFlyEffectManager với _coinPrefab + _poolContainer", CoinFlyCheck());
        failures += Report("HUDCanvas/HUDPanel anchor nằm trong [0,1]", HudAnchorCheck());
        failures += Report("Không còn Assets/Resources/Prefabs trùng lặp", DuplicatePrefabCheck());
        failures += Report("Scene không còn override chết _hudController", DeadOverrideCheck());
        failures += Report("AudioManager có _bgmClip + _sfxSource + _bgmSource đã gán", BgmCheck());

        if (failures == 0)
        {
            Debug.Log("<color=green>=== [Bus Jam] Validate Scene Wiring: PASS ===</color>");
        }
        else
        {
            Debug.LogError($"=== [Bus Jam] Validate Scene Wiring: {failures} mục FAIL — xem chi tiết ở trên ===");
        }
    }

    private static bool CanvasCheck(string canvasName)
    {
        return FindObject(canvasName) != null;
    }

    private static bool LevelsCheck()
    {
        LevelLoader loader = Object.FindObjectOfType<LevelLoader>();
        if (loader == null) return false;

        SerializedProperty levels = new SerializedObject(loader).FindProperty("levels");
        if (levels == null || levels.arraySize == 0) return false;

        for (int i = 0; i < levels.arraySize; i++)
        {
            if (levels.GetArrayElementAtIndex(i).objectReferenceValue == null) return false;
        }

        return true;
    }

    private static bool UIManagerCheck()
    {
        UIManager uiManager = Object.FindObjectOfType<UIManager>();
        if (uiManager == null) return false;

        SerializedObject so = new SerializedObject(uiManager);
        string[] fields =
        {
            "_hudController", "_winPopup", "_losePopup", "_settingsPopup", "_continuePopup",
            "_nextLevelButton", "_retryLoseButton"
        };

        for (int i = 0; i < fields.Length; i++)
        {
            SerializedProperty prop = so.FindProperty(fields[i]);
            if (prop == null || prop.objectReferenceValue == null) return false;
        }

        return true;
    }

    private static bool CoinFlyCheck()
    {
        GameObject vfx = GameObject.Find("VFXCanvas");
        if (vfx == null) return false;

        CoinFlyEffectManager manager = vfx.GetComponent<CoinFlyEffectManager>();
        if (manager == null) return false;

        SerializedObject so = new SerializedObject(manager);
        SerializedProperty prefab = so.FindProperty("_coinPrefab");
        SerializedProperty container = so.FindProperty("_poolContainer");

        return prefab != null && prefab.objectReferenceValue != null &&
               container != null && container.objectReferenceValue != null;
    }

    private static bool BgmCheck()
    {
        AudioManager audio = Object.FindObjectOfType<AudioManager>();
        if (audio == null) return false;

        SerializedObject so = new SerializedObject(audio);
        string[] fields = { "_bgmClip", "_sfxSource", "_bgmSource" };

        for (int i = 0; i < fields.Length; i++)
        {
            SerializedProperty prop = so.FindProperty(fields[i]);
            if (prop == null || prop.objectReferenceValue == null) return false;
        }

        return true;
    }

    private static AudioClip FirstAudioClip(string folder)
    {
        if (!System.IO.Directory.Exists(folder)) return null;

        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folder });
        string[] paths = new string[guids.Length];
        for (int i = 0; i < guids.Length; i++) paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
        System.Array.Sort(paths, System.StringComparer.Ordinal);

        for (int i = 0; i < paths.Length; i++)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(paths[i]);
            if (clip != null) return clip;
        }

        return null;
    }

    private static bool HudAnchorCheck()
    {
        GameObject panel = Child(FindObject("HUDCanvas"), "HUDPanel");
        if (panel == null) return false;

        RectTransform rect = panel.GetComponent<RectTransform>();
        if (rect == null) return false;

        return InRange(rect.anchorMin) && InRange(rect.anchorMax);
    }

    private static bool InRange(Vector2 value)
    {
        return value.x >= 0f && value.x <= 1f && value.y >= 0f && value.y <= 1f;
    }

    private static bool DuplicatePrefabCheck()
    {
        return !System.IO.Directory.Exists("Assets/Resources/Prefabs");
    }

    private static bool DeadOverrideCheck()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || string.IsNullOrEmpty(scene.path)) return false;
        if (!System.IO.File.Exists(scene.path)) return false;

        return !System.IO.File.ReadAllText(scene.path).Contains("propertyPath: _hudController");
    }

    private static int Report(string label, bool passed)
    {
        if (passed)
        {
            Debug.Log($"[Bus Jam] PASS — {label}");
            return 0;
        }

        Debug.LogError($"[Bus Jam] FAIL — {label}");
        return 1;
    }

    private static Transform FindObject(string objectName)
    {
        GameObject go = GameObject.Find(objectName);
        return go != null ? go.transform : null;
    }

    private static GameObject Child(Transform root, string childName)
    {
        if (root == null) return null;
        Transform child = root.Find(childName);
        return child != null ? child.gameObject : null;
    }

    private static Button ButtonIn(GameObject root, string path)
    {
        if (root == null) return null;
        Transform t = root.transform.Find(path);
        return t != null ? t.GetComponent<Button>() : null;
    }

    private static void ReportMissing(string label, Object value)
    {
        if (value == null) Debug.LogWarning($"[Bus Jam] Không tìm thấy '{label}' trong scene.");
    }

    private static void ReportMissing(string label, bool found)
    {
        if (!found) Debug.LogWarning($"[Bus Jam] Không tìm thấy '{label}' trong scene.");
    }

    private static bool Assign(SerializedObject so, string propertyName, Object value)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop == null || value == null) return false;
        if (prop.objectReferenceValue == value) return false;

        prop.objectReferenceValue = value;
        return true;
    }
}
#endif
