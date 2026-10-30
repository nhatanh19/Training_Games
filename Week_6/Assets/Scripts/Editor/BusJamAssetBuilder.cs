#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class BusJamAssetBuilder
{
    private const string VFXDir = "Assets/Prefabs/VFX";
    private const string UIDir = "Assets/Prefabs/UI";

    static BusJamAssetBuilder()
    {
        EditorApplication.delayCall += CheckAndAutoBuild;
    }

    private static void CheckAndAutoBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer) return;

        bool needsVFX = !File.Exists(VFXDir + "/VFX_Dust.prefab") ||
                         !File.Exists(VFXDir + "/VFX_HitSpark.prefab") ||
                         !File.Exists(VFXDir + "/VFX_Confetti.prefab");

        bool needsUI = !File.Exists(UIDir + "/UICanvas.prefab");

        if (needsVFX || needsUI)
        {
            Debug.Log("[Bus Jam] Auto-building missing prefabs and setting up scene...");
            BuildAllAndSetupScene();
        }
    }

    [MenuItem("Tools/Bus Jam/Build All Prefabs and Setup Scene", false, 1)]
    public static void BuildAllAndSetupScene()
    {
        GenerateVFXPrefabs();
        BuildUICanvasAndWire();
        SetupAudioSourceAndSlots();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Bus Jam] Completed: All VFX prefabs generated, UI Canvas created, and Scene wired successfully!");
    }

    [MenuItem("Tools/Bus Jam/1. Generate VFX Prefabs", false, 20)]
    public static void GenerateVFXPrefabs()
    {
        EnsureFolderExists(VFXDir);

        CreateDustPrefab(VFXDir + "/VFX_Dust.prefab");
        CreateHitSparkPrefab(VFXDir + "/VFX_HitSpark.prefab");
        CreateConfettiPrefab(VFXDir + "/VFX_Confetti.prefab");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Bus Jam] VFX Prefabs generated in " + VFXDir);
    }

    [MenuItem("Tools/Bus Jam/2. Build UI Canvas & Prefab", false, 21)]
    public static void BuildUICanvasAndWire()
    {
        EnsureFolderExists(UIDir);

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded) return;

        // Check if EventSystem exists, create if not
        EventSystem es = Object.FindObjectOfType<EventSystem>();
        if (es == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            es = esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(esGo, "Create EventSystem");
        }

        // Check if Canvas exists
        Canvas existingCanvas = Object.FindObjectOfType<Canvas>();
        GameObject canvasGo;

        if (existingCanvas == null)
        {
            canvasGo = new GameObject("UICanvas");
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create UICanvas");

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvasGo = existingCanvas.gameObject;
        }

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // HUD Panel
        Transform hudTransform = canvasGo.transform.Find("HUDPanel");
        GameObject hudPanelGo;
        if (hudTransform == null)
        {
            hudPanelGo = new GameObject("HUDPanel", typeof(RectTransform));
            hudPanelGo.transform.SetParent(canvasGo.transform, false);
            RectTransform hudRect = hudPanelGo.GetComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(0f, 1f);
            hudRect.anchorMax = new Vector2(1f, 1f);
            hudRect.pivot = new Vector2(0.5f, 1f);
            hudRect.anchoredPosition = Vector2.zero;
            hudRect.sizeDelta = new Vector2(0f, 220f);
        }
        else
        {
            hudPanelGo = hudTransform.gameObject;
        }

        // Level Text
        Transform lvlTextTransform = hudPanelGo.transform.Find("LevelText");
        Text lvlText;
        if (lvlTextTransform == null)
        {
            GameObject lvlGo = new GameObject("LevelText", typeof(RectTransform), typeof(Text));
            lvlGo.transform.SetParent(hudPanelGo.transform, false);
            lvlText = lvlGo.GetComponent<Text>();
            lvlText.font = defaultFont;
            lvlText.fontSize = 54;
            lvlText.fontStyle = FontStyle.Bold;
            lvlText.alignment = TextAnchor.MiddleCenter;
            lvlText.color = new Color(0.15f, 0.15f, 0.2f, 1f);
            lvlText.text = "LEVEL 1";

            RectTransform lvlRect = lvlGo.GetComponent<RectTransform>();
            lvlRect.anchorMin = new Vector2(0.5f, 0.5f);
            lvlRect.anchorMax = new Vector2(0.5f, 0.5f);
            lvlRect.pivot = new Vector2(0.5f, 0.5f);
            lvlRect.anchoredPosition = new Vector2(0f, -20f);
            lvlRect.sizeDelta = new Vector2(600f, 120f);
        }
        else
        {
            lvlText = lvlTextTransform.GetComponent<Text>();
        }

        // Restart HUD Button
        Transform restartBtnTransform = hudPanelGo.transform.Find("RestartHUDButton");
        Button restartHudBtn;
        if (restartBtnTransform == null)
        {
            GameObject restartGo = CreateSimpleButton("RestartHUDButton", "RESTART", defaultFont, new Color(0.9f, 0.45f, 0.2f), 28);
            restartGo.transform.SetParent(hudPanelGo.transform, false);
            RectTransform rRect = restartGo.GetComponent<RectTransform>();
            rRect.anchorMin = new Vector2(1f, 0.5f);
            rRect.anchorMax = new Vector2(1f, 0.5f);
            rRect.pivot = new Vector2(1f, 0.5f);
            rRect.anchoredPosition = new Vector2(-40f, -20f);
            rRect.sizeDelta = new Vector2(200f, 80f);
            restartHudBtn = restartGo.GetComponent<Button>();
        }
        else
        {
            restartHudBtn = restartBtnTransform.GetComponent<Button>();
        }

        // Win Popup
        Button nextBtn, replayWinBtn;
        GameObject winPopupGo = GetOrCreatePopup(canvasGo.transform, "WinPopup", defaultFont,
            "LEVEL COMPLETED!", "All passengers are on their way!",
            "NEXT LEVEL", out nextBtn,
            "REPLAY", out replayWinBtn,
            new Color(0.2f, 0.75f, 0.35f, 0.98f));

        // Lose Popup
        Button retryLoseBtn, unusedBtn;
        GameObject losePopupGo = GetOrCreatePopup(canvasGo.transform, "LosePopup", defaultFont,
            "OUT OF MOVES!", "Bus stop is full! Try a different strategy.",
            "TRY AGAIN", out retryLoseBtn,
            null, out unusedBtn,
            new Color(0.85f, 0.25f, 0.25f, 0.98f));

        // Save as Prefab
        string uiPrefabPath = UIDir + "/UICanvas.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(canvasGo, uiPrefabPath, InteractionMode.AutomatedAction);

        // Wire to UIManager in Scene
        UIManager uiMgr = Object.FindObjectOfType<UIManager>();
        if (uiMgr != null)
        {
            SerializedObject so = new SerializedObject(uiMgr);
            SetObjProp(so, "winPopup", winPopupGo);
            SetObjProp(so, "losePopup", losePopupGo);
            SetObjProp(so, "hudPanel", hudPanelGo);
            SetObjProp(so, "levelText", lvlText);
            SetObjProp(so, "nextLevelButton", nextBtn);
            SetObjProp(so, "replayWinButton", replayWinBtn);
            SetObjProp(so, "retryLoseButton", retryLoseBtn);
            SetObjProp(so, "restartHudButton", restartHudBtn);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(uiMgr);
        }

        Debug.Log("[Bus Jam] UI Canvas and Prefab setup complete!");
    }

    [MenuItem("Tools/Bus Jam/3. Setup AudioSource, VFX & Slots", false, 22)]
    public static void SetupAudioSourceAndSlots()
    {
        // 1. AudioSource on AudioManager
        AudioManager audioMgr = Object.FindObjectOfType<AudioManager>();
        if (audioMgr != null)
        {
            AudioSource source = audioMgr.GetComponent<AudioSource>();
            if (source == null)
            {
                source = Undo.AddComponent<AudioSource>(audioMgr.gameObject);
                source.playOnAwake = false;
            }
            EditorUtility.SetDirty(audioMgr);
        }

        // 2. Wire VFX Prefabs to VFXManager
        VFXManager vfxMgr = Object.FindObjectOfType<VFXManager>();
        if (vfxMgr != null)
        {
            ParticleSystem dustPs = AssetDatabase.LoadAssetAtPath<ParticleSystem>(VFXDir + "/VFX_Dust.prefab");
            ParticleSystem hitSparkPs = AssetDatabase.LoadAssetAtPath<ParticleSystem>(VFXDir + "/VFX_HitSpark.prefab");
            ParticleSystem confettiPs = AssetDatabase.LoadAssetAtPath<ParticleSystem>(VFXDir + "/VFX_Confetti.prefab");

            SerializedObject vfxSo = new SerializedObject(vfxMgr);
            if (dustPs != null) SetObjProp(vfxSo, "dustPrefab", dustPs);
            if (hitSparkPs != null) SetObjProp(vfxSo, "hitSparkPrefab", hitSparkPs);
            if (confettiPs != null) SetObjProp(vfxSo, "confettiPrefab", confettiPs);
            vfxSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(vfxMgr);
        }

        // 3. Parent Slots to BusStopManager
        BusStopManager busStopMgr = Object.FindObjectOfType<BusStopManager>();
        if (busStopMgr != null)
        {
            BusStopSlot[] existingSlots = Object.FindObjectsOfType<BusStopSlot>();
            System.Array.Sort(existingSlots, (a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

            SerializedObject bsmSo = new SerializedObject(busStopMgr);
            SerializedProperty slotsProp = bsmSo.FindProperty("slots");
            slotsProp.ClearArray();

            for (int i = 0; i < existingSlots.Length; i++)
            {
                existingSlots[i].transform.SetParent(busStopMgr.transform, true);
                slotsProp.InsertArrayElementAtIndex(i);
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = existingSlots[i];
            }
            bsmSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(busStopMgr);
        }
    }

    private static GameObject GetOrCreatePopup(Transform canvasTransform, string popupName, Font font,
        string titleText, string subtitleText,
        string mainBtnText, out Button mainBtn,
        string subBtnText, out Button subBtn,
        Color cardColor)
    {
        Transform existingPopup = canvasTransform.Find(popupName);
        GameObject popupGo;
        if (existingPopup == null)
        {
            popupGo = new GameObject(popupName, typeof(RectTransform), typeof(Image));
            popupGo.transform.SetParent(canvasTransform, false);
            RectTransform pRect = popupGo.GetComponent<RectTransform>();
            pRect.anchorMin = Vector2.zero;
            pRect.anchorMax = Vector2.one;
            pRect.offsetMin = Vector2.zero;
            pRect.offsetMax = Vector2.zero;

            Image pImage = popupGo.GetComponent<Image>();
            pImage.color = new Color(0f, 0f, 0f, 0.75f);
        }
        else
        {
            popupGo = existingPopup.gameObject;
        }

        // Card
        Transform cardTransform = popupGo.transform.Find("Card");
        GameObject cardGo;
        if (cardTransform == null)
        {
            cardGo = new GameObject("Card", typeof(RectTransform), typeof(Image));
            cardGo.transform.SetParent(popupGo.transform, false);
            RectTransform cRect = cardGo.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.5f, 0.5f);
            cRect.anchorMax = new Vector2(0.5f, 0.5f);
            cRect.pivot = new Vector2(0.5f, 0.5f);
            cRect.sizeDelta = new Vector2(850f, 650f);

            Image cImage = cardGo.GetComponent<Image>();
            cImage.color = cardColor;
        }
        else
        {
            cardGo = cardTransform.gameObject;
        }

        // Title
        Transform titleTransform = cardGo.transform.Find("Title");
        if (titleTransform == null)
        {
            GameObject tGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            tGo.transform.SetParent(cardGo.transform, false);
            Text tText = tGo.GetComponent<Text>();
            tText.font = font;
            tText.fontSize = 52;
            tText.fontStyle = FontStyle.Bold;
            tText.alignment = TextAnchor.MiddleCenter;
            tText.color = Color.white;
            tText.text = titleText;

            RectTransform tRect = tGo.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.5f, 0.5f);
            tRect.anchorMax = new Vector2(0.5f, 0.5f);
            tRect.anchoredPosition = new Vector2(0f, 190f);
            tRect.sizeDelta = new Vector2(800f, 100f);
        }

        // Subtitle
        Transform subTransform = cardGo.transform.Find("Subtitle");
        if (subTransform == null)
        {
            GameObject sGo = new GameObject("Subtitle", typeof(RectTransform), typeof(Text));
            sGo.transform.SetParent(cardGo.transform, false);
            Text sText = sGo.GetComponent<Text>();
            sText.font = font;
            sText.fontSize = 32;
            sText.alignment = TextAnchor.MiddleCenter;
            sText.color = new Color(1f, 1f, 1f, 0.9f);
            sText.text = subtitleText;

            RectTransform sRect = sGo.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.5f, 0.5f);
            sRect.anchorMax = new Vector2(0.5f, 0.5f);
            sRect.anchoredPosition = new Vector2(0f, 90f);
            sRect.sizeDelta = new Vector2(750f, 80f);
        }

        // Main Button
        Transform mainBtnTrans = cardGo.transform.Find("MainButton");
        if (mainBtnTrans == null)
        {
            GameObject mbGo = CreateSimpleButton("MainButton", mainBtnText, font, new Color(0.95f, 0.75f, 0.1f, 1f), 38);
            mbGo.transform.SetParent(cardGo.transform, false);
            RectTransform mbRect = mbGo.GetComponent<RectTransform>();
            mbRect.anchorMin = new Vector2(0.5f, 0.5f);
            mbRect.anchorMax = new Vector2(0.5f, 0.5f);
            mbRect.anchoredPosition = string.IsNullOrEmpty(subBtnText) ? new Vector2(0f, -80f) : new Vector2(0f, -40f);
            mbRect.sizeDelta = new Vector2(500f, 110f);
            mainBtn = mbGo.GetComponent<Button>();
        }
        else
        {
            mainBtn = mainBtnTrans.GetComponent<Button>();
        }

        // Sub Button
        if (!string.IsNullOrEmpty(subBtnText))
        {
            Transform subBtnTrans = cardGo.transform.Find("SubButton");
            if (subBtnTrans == null)
            {
                GameObject sbGo = CreateSimpleButton("SubButton", subBtnText, font, new Color(1f, 1f, 1f, 0.85f), 34);
                sbGo.transform.SetParent(cardGo.transform, false);
                RectTransform sbRect = sbGo.GetComponent<RectTransform>();
                sbRect.anchorMin = new Vector2(0.5f, 0.5f);
                sbRect.anchorMax = new Vector2(0.5f, 0.5f);
                sbRect.anchoredPosition = new Vector2(0f, -170f);
                sbRect.sizeDelta = new Vector2(400f, 85f);
                subBtn = sbGo.GetComponent<Button>();
            }
            else
            {
                subBtn = subBtnTrans.GetComponent<Button>();
            }
        }
        else
        {
            subBtn = null;
        }

        popupGo.SetActive(false);
        return popupGo;
    }

    private static GameObject CreateSimpleButton(string name, string text, Font font, Color bgColor, int fontSize)
    {
        GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.GetComponent<Image>().color = bgColor;

        GameObject txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txtGo.transform.SetParent(btnGo.transform, false);
        Text txt = txtGo.GetComponent<Text>();
        txt.font = font;
        txt.text = text;
        txt.fontSize = fontSize;
        txt.fontStyle = FontStyle.Bold;
        txt.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        txt.alignment = TextAnchor.MiddleCenter;

        RectTransform tRect = txtGo.GetComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.offsetMin = Vector2.zero;
        tRect.offsetMax = Vector2.zero;

        return btnGo;
    }

    private static void CreateDustPrefab(string path)
    {
        GameObject go = new GameObject("VFX_Dust");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startLifetime = 0.35f;
        main.startSpeed = 1.5f;
        main.startSize = 0.25f;
        main.startColor = new Color(0.85f, 0.85f, 0.85f, 0.6f);

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 0.1f;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    private static void CreateHitSparkPrefab(string path)
    {
        GameObject go = new GameObject("VFX_HitSpark");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startLifetime = 0.2f;
        main.startSpeed = 3.5f;
        main.startSize = 0.15f;
        main.startColor = new Color(1f, 0.8f, 0.2f, 1f);

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.15f;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    private static void CreateConfettiPrefab(string path)
    {
        GameObject go = new GameObject("VFX_Confetti");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startLifetime = 2.0f;
        main.startSpeed = 2.5f;
        main.startSize = 0.18f;
        main.gravityModifier = 0.5f;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.2f, 0.2f, 1f),
            new Color(0.2f, 0.8f, 1f, 1f)
        );

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(8f, 1f, 1f);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    private static void EnsureFolderExists(string folderPath)
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            string parent = Path.GetDirectoryName(folderPath).Replace("\\", "/");
            string leaf = Path.GetFileName(folderPath);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolderExists(parent);
            }
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }

    private static void SetObjProp(SerializedObject so, string propName, Object value)
    {
        SerializedProperty prop = so.FindProperty(propName);
        if (prop != null)
        {
            prop.objectReferenceValue = value;
        }
    }
}
#endif
