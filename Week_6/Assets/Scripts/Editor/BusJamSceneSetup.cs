#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Wires the GameBootstrap component in the currently open scene.
/// Runs automatically after a script reload and can be re-run from Tools > Bus Jam > Setup Scene.
/// </summary>
[InitializeOnLoad]
public static class BusJamSceneSetup
{
    private const string BootstrapObjectName = "GameBootstrap";

    static BusJamSceneSetup()
    {
        EditorApplication.delayCall += AutoSetup;
    }

    [MenuItem("Tools/Bus Jam/Setup Scene")]
    public static void SetupSceneFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Bus Jam] Exit Play Mode before running scene setup.");
            return;
        }

        if (SetupScene(true, true))
        {
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("[Bus Jam] Scene setup complete and saved.");
        }
    }

    private static void AutoSetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer) return;

        SetupScene(false, false);
    }

    private static bool SetupScene(bool verbose, bool registerUndo)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded) return false;

        GridManager gridManager = Object.FindObjectOfType<GridManager>();
        BusStopManager busStopManager = Object.FindObjectOfType<BusStopManager>();
        PassengerQueueManager queueManager = Object.FindObjectOfType<PassengerQueueManager>();
        LevelLoader levelLoader = Object.FindObjectOfType<LevelLoader>();
        GamePoolManager poolManager = Object.FindObjectOfType<GamePoolManager>();
        GameManager gameManager = Object.FindObjectOfType<GameManager>();
        UIManager uiManager = Object.FindObjectOfType<UIManager>();
        VFXManager vfxManager = Object.FindObjectOfType<VFXManager>();
        CameraShakeManager cameraShakeManager = Object.FindObjectOfType<CameraShakeManager>();

        bool isBusJamScene = gridManager != null || busStopManager != null || queueManager != null ||
                             levelLoader != null || poolManager != null || gameManager != null;
        if (!isBusJamScene) return false;

        GameObject bootstrapGo = GameObject.Find(BootstrapObjectName);
        if (bootstrapGo == null)
        {
            GameBootstrap existing = Object.FindObjectOfType<GameBootstrap>();
            if (existing != null) bootstrapGo = existing.gameObject;
        }

        bool changed = false;

        if (bootstrapGo == null)
        {
            bootstrapGo = new GameObject(BootstrapObjectName);
            if (registerUndo) Undo.RegisterCreatedObjectUndo(bootstrapGo, "Create GameBootstrap");
            changed = true;
        }

        GameBootstrap bootstrap = bootstrapGo.GetComponent<GameBootstrap>();
        if (bootstrap == null)
        {
            bootstrap = registerUndo
                ? Undo.AddComponent<GameBootstrap>(bootstrapGo)
                : bootstrapGo.AddComponent<GameBootstrap>();
            changed = true;
        }

        SerializedObject bootstrapSo = new SerializedObject(bootstrap);
        changed |= Assign(bootstrapSo, "gridManager", gridManager);
        changed |= Assign(bootstrapSo, "busStopManager", busStopManager);
        changed |= Assign(bootstrapSo, "passengerQueueManager", queueManager);
        changed |= Assign(bootstrapSo, "levelLoader", levelLoader);
        changed |= Assign(bootstrapSo, "poolManager", poolManager);
        changed |= Assign(bootstrapSo, "gameManager", gameManager);
        changed |= Assign(bootstrapSo, "uiManager", uiManager);
        changed |= Assign(bootstrapSo, "vfxManager", vfxManager);
        changed |= Assign(bootstrapSo, "cameraShakeManager", cameraShakeManager);
        bootstrapSo.ApplyModifiedPropertiesWithoutUndo();

        Camera mainCamera = Camera.main;
        if (cameraShakeManager != null && mainCamera != null)
        {
            SerializedObject shakeSo = new SerializedObject(cameraShakeManager);
            SerializedProperty cameraProp = shakeSo.FindProperty("targetCamera");
            if (cameraProp != null && cameraProp.objectReferenceValue == null)
            {
                cameraProp.objectReferenceValue = mainCamera;
                shakeSo.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }
        }

        if (poolManager != null && levelLoader != null)
        {
            SerializedObject poolSo = new SerializedObject(poolManager);
            changed |= AssignIfNull(poolSo, "bus2x1Prefab", levelLoader.Bus2x1Prefab);
            changed |= AssignIfNull(poolSo, "bus3x1Prefab", levelLoader.Bus3x1Prefab);
            changed |= AssignIfNull(poolSo, "passengerPrefab", levelLoader.PassengerPrefab);
            poolSo.ApplyModifiedPropertiesWithoutUndo();
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (verbose) Debug.Log("[Bus Jam] GameBootstrap references wired.");
        }
        else if (verbose)
        {
            Debug.Log("[Bus Jam] Scene is already configured; nothing to change.");
        }

        return isBusJamScene;
    }

    private static bool Assign(SerializedObject so, string propertyName, Object value)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop == null || value == null) return false;
        if (prop.objectReferenceValue == value) return false;

        prop.objectReferenceValue = value;
        return true;
    }

    private static bool AssignIfNull(SerializedObject so, string propertyName, Object value)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop == null || value == null) return false;
        if (prop.objectReferenceValue != null) return false;

        prop.objectReferenceValue = value;
        return true;
    }
}
#endif
