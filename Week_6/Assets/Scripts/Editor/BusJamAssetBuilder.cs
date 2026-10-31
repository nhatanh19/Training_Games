#if UNITY_EDITOR
using System.IO;
using TMPro;
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

        bool needsUI = !File.Exists(UIDir + "/HUDCanvas.prefab") ||
                       !File.Exists(UIDir + "/PopupCanvas.prefab") ||
                       !File.Exists(UIDir + "/VFXCanvas.prefab") ||
                       !File.Exists(UIDir + "/CoinItem.prefab") ||
                       GameObject.Find("HUDCanvas") == null ||
                       GameObject.Find("PopupCanvas") == null;

        bool needsVisuals = GameObject.Find("SceneBackground") == null;

        if (needsVFX || needsUI || needsVisuals)
        {
            Debug.Log("[Bus Jam] Auto-building Task 1 UI Architecture, Prefabs & Visuals...");
            BuildAllAndSetupScene();
        }
    }

    [MenuItem("Tools/Bus Jam/Build All Prefabs and Setup Scene", false, 1)]
    public static void BuildAllAndSetupScene()
    {
        GenerateVFXPrefabs();
        BuildTask1UIArchitecture();
        SetupAudioSourceAndSlots();
        UpgradeVisuals();

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Bus Jam] Task 1 UI Architecture and Prefabs successfully configured and saved!");
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
    }

    [MenuItem("Tools/Bus Jam/Safe Area/Simulate iPhone 14 Pro", false, 10)]
    public static void SimulateIPhone14Pro()
    {
        SafeAreaFitter[] fitters = Object.FindObjectsOfType<SafeAreaFitter>(true);
        foreach (var f in fitters) f.SimulateIPhone14Pro();
        Debug.Log("[Bus Jam] Simulated iPhone 14 Pro Dynamic Island Safe Area on all fitters.");
    }

    [MenuItem("Tools/Bus Jam/Safe Area/Simulate iPhone X Notch", false, 11)]
    public static void SimulateIPhoneX()
    {
        SafeAreaFitter[] fitters = Object.FindObjectsOfType<SafeAreaFitter>(true);
        foreach (var f in fitters) f.SimulateIPhoneX();
        Debug.Log("[Bus Jam] Simulated iPhone X/11 Notch Safe Area on all fitters.");
    }

    [MenuItem("Tools/Bus Jam/Safe Area/Reset Safe Area (Normal)", false, 12)]
    public static void ResetSafeAreaSimulation()
    {
        SafeAreaFitter[] fitters = Object.FindObjectsOfType<SafeAreaFitter>(true);
        foreach (var f in fitters) f.DisableSimulation();
        Debug.Log("[Bus Jam] Reset Safe Area simulation.");
    }

    [MenuItem("Tools/Bus Jam/2. Build Task 1 UI Architecture", false, 21)]
    public static void BuildTask1UIArchitecture()
    {
        EnsureFolderExists(UIDir);
                TMP_FontAsset defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (defaultFont == null)
        {
            defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }
        if (defaultFont == null)
        {
            try
            {
                if (TMP_Settings.instance != null)
                {
                    defaultFont = TMP_Settings.defaultFontAsset;
                }
            }
            catch
            {
                defaultFont = null;
            }
        }

        // 1. EventSystem
        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(esGo, "Create EventSystem");
        }

        // 2. SettingsManager in Scene
        SettingsManager settingsMgr = Object.FindObjectOfType<SettingsManager>();
        if (settingsMgr == null)
        {
            GameObject smGo = new GameObject("SettingsManager");
            settingsMgr = smGo.AddComponent<SettingsManager>();
            Undo.RegisterCreatedObjectUndo(smGo, "Create SettingsManager");
        }

        // Clean up legacy monolithic UICanvas if present
        Canvas legacyCanvas = Object.FindObjectOfType<Canvas>();
        if (legacyCanvas != null && legacyCanvas.name == "UICanvas")
        {
            Object.DestroyImmediate(legacyCanvas.gameObject);
        }

        // 3. HUDCanvas (Sorting Order: 10)
        GameObject hudCanvasGo = GetOrCreateCanvasObject("HUDCanvas", 10);
        HUDController hudController = BuildHUDContent(hudCanvasGo, defaultFont);
        PrefabUtility.SaveAsPrefabAssetAndConnect(hudCanvasGo, UIDir + "/HUDCanvas.prefab", InteractionMode.AutomatedAction);

        // 4. PopupCanvas (Sorting Order: 20)
        GameObject popupCanvasGo = GetOrCreateCanvasObject("PopupCanvas", 20);
        UIManager uiManager = BuildPopupContent(popupCanvasGo, hudController, defaultFont);
        PrefabUtility.SaveAsPrefabAssetAndConnect(popupCanvasGo, UIDir + "/PopupCanvas.prefab", InteractionMode.AutomatedAction);

        // 5. VFXCanvas (Sorting Order: 30)
        GameObject vfxCanvasGo = GetOrCreateCanvasObject("VFXCanvas", 30);
        if (vfxCanvasGo.GetComponent<CoinFlyEffectManager>() == null)
        {
            vfxCanvasGo.AddComponent<CoinFlyEffectManager>();
        }
        PrefabUtility.SaveAsPrefabAssetAndConnect(vfxCanvasGo, UIDir + "/VFXCanvas.prefab", InteractionMode.AutomatedAction);
        if (System.IO.Directory.Exists("Assets/Resources/Prefabs/UI"))
        {
            PrefabUtility.SaveAsPrefabAsset(vfxCanvasGo, "Assets/Resources/Prefabs/UI/VFXCanvas.prefab");
        }

        // 5b. Build CoinItem Prefab
        BuildCoinPrefab();

        // 6. Connect to GameBootstrap
        GameBootstrap bootstrap = Object.FindObjectOfType<GameBootstrap>();
        if (bootstrap != null)
        {
            SerializedObject bSo = new SerializedObject(bootstrap);
            SetObjProp(bSo, "settingsManager", settingsMgr);
            SetObjProp(bSo, "uiManager", uiManager);
            SetObjProp(bSo, "hudController", hudController);
            bSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(bootstrap);
        }
    }

    private static GameObject GetOrCreateCanvasObject(string canvasName, int sortingOrder)
    {
        GameObject go = GameObject.Find(canvasName);
        if (go == null)
        {
            go = new GameObject(canvasName);
            Undo.RegisterCreatedObjectUndo(go, "Create " + canvasName);
        }

        Canvas canvas = go.GetComponent<Canvas>();
        if (canvas == null) canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = go.GetComponent<GraphicRaycaster>();
        if (raycaster == null) go.AddComponent<GraphicRaycaster>();

        go.transform.localScale = Vector3.one;

        return go;
    }

    [MenuItem("Tools/Bus Jam/Build Coin Prefab")]
    public static GameObject BuildCoinPrefab()
    {
        EnsureFolderExists(UIDir);
        EnsureFolderExists("Assets/Resources/Prefabs/UI");

        GameObject coinObj = new GameObject("CoinItem", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rt = coinObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(60f, 60f);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        Image img = coinObj.GetComponent<Image>();
        img.raycastTarget = false;
        img.preserveAspect = true;

        Sprite coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/coin_icon.png");
        if (coinSprite == null)
        {
            coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/coin_icon.png");
        }

        if (coinSprite != null)
        {
            img.sprite = coinSprite;
            img.color = Color.white;
        }
        else
        {
            img.color = new Color(1f, 0.84f, 0.1f, 1f);
        }

        string prefabPath = UIDir + "/CoinItem.prefab";
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(coinObj, prefabPath);

        string resourcesPrefabDir = "Assets/Resources/Prefabs/UI";
        if (System.IO.Directory.Exists(resourcesPrefabDir))
        {
            PrefabUtility.SaveAsPrefabAsset(coinObj, resourcesPrefabDir + "/CoinItem.prefab");
        }

        Object.DestroyImmediate(coinObj);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Bus Jam] CoinItem.prefab successfully created by code at: " + prefabPath);
        return savedPrefab;
    }

    private static HUDController BuildHUDContent(GameObject canvasGo, TMP_FontAsset font)
    {
        HUDController hud = canvasGo.GetComponent<HUDController>();
        if (hud == null) hud = canvasGo.AddComponent<HUDController>();

        // Top Safe Area Panel
        Transform hudPanelTrans = canvasGo.transform.Find("HUDPanel");
        GameObject hudPanel;
        if (hudPanelTrans == null)
        {
            hudPanel = new GameObject("HUDPanel", typeof(RectTransform));
            hudPanel.transform.SetParent(canvasGo.transform, false);
            RectTransform r = hudPanel.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(1f, 1f);
            r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = new Vector2(0f, 240f);
        }
        else
        {
            hudPanel = hudPanelTrans.gameObject;
        }

        SafeAreaFitter saf = hudPanel.GetComponent<SafeAreaFitter>();
        if (saf == null) saf = hudPanel.AddComponent<SafeAreaFitter>();

        // Settings Button (Top-Left)
        Button settingsBtn = GetOrCreateButton(hudPanel.transform, "SettingsButton", "SETTINGS", font, new Vector2(0f, 0.5f), new Vector2(90f, -30f), new Vector2(150f, 75f), new Color(0.2f, 0.2f, 0.25f, 0.85f), Color.white, 24);

        // Level Text (Top-Center)
        TextMeshProUGUI levelText = GetOrCreateTMPText(hudPanel.transform, "LevelText", "LEVEL 1", font,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 25f), new Vector2(500f, 80f),
            new Color(0.15f, 0.15f, 0.2f, 1f), 48, FontStyles.Bold);

        // Coin Counter Container (Top-Center below Level)
        Transform coinContainerTrans = hudPanel.transform.Find("CoinContainer");
        GameObject coinContainer;
        if (coinContainerTrans == null)
        {
            coinContainer = new GameObject("CoinContainer", typeof(RectTransform), typeof(Image));
            coinContainer.transform.SetParent(hudPanel.transform, false);
            RectTransform cRect = coinContainer.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.5f, 0.5f);
            cRect.anchorMax = new Vector2(0.5f, 0.5f);
            cRect.pivot = new Vector2(0.5f, 0.5f);
            cRect.anchoredPosition = new Vector2(0f, -40f);
            cRect.sizeDelta = new Vector2(220f, 65f);

            Image cImg = coinContainer.GetComponent<Image>();
            cImg.color = new Color(0f, 0f, 0f, 0.35f);
        }
        else
        {
            coinContainer = coinContainerTrans.gameObject;
        }

        TextMeshProUGUI coinText = GetOrCreateTMPText(coinContainer.transform, "CoinText", "0", font,
            new Vector2(0.5f, 0.5f), new Vector2(20f, 0f), new Vector2(140f, 55f),
            new Color(1f, 0.85f, 0.2f, 1f), 34, FontStyles.Bold);

        // Coin Icon indicator
        Transform coinIconTrans = coinContainer.transform.Find("CoinIcon");
        if (coinIconTrans == null)
        {
            GameObject iconGo = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(coinContainer.transform, false);
            RectTransform iRect = iconGo.GetComponent<RectTransform>();
            iRect.anchorMin = new Vector2(0f, 0.5f);
            iRect.anchorMax = new Vector2(0f, 0.5f);
            iRect.pivot = new Vector2(0.5f, 0.5f);
            iRect.anchoredPosition = new Vector2(35f, 0f);
            iRect.sizeDelta = new Vector2(40f, 40f);

            Image iconImg = iconGo.GetComponent<Image>();
            iconImg.color = new Color(1f, 0.8f, 0.1f, 1f);
        }

        // Restart Button (Top-Right)
        Button restartBtn = GetOrCreateButton(hudPanel.transform, "RestartButton", "RESTART", font, new Vector2(1f, 0.5f), new Vector2(-90f, -30f), new Vector2(150f, 75f), new Color(0.9f, 0.45f, 0.2f, 0.95f), Color.white, 24);

        // Wire HUDController
        SerializedObject hudSo = new SerializedObject(hud);
        SetObjProp(hudSo, "_levelText", levelText);
        SetObjProp(hudSo, "_coinText", coinText);
        SetObjProp(hudSo, "_coinTargetTransform", coinContainer.transform);
        SetObjProp(hudSo, "_restartButton", restartBtn);
        SetObjProp(hudSo, "_settingsButton", settingsBtn);
        hudSo.ApplyModifiedProperties();
        EditorUtility.SetDirty(hud);

        return hud;
    }

    private static UIManager BuildPopupContent(GameObject canvasGo, HUDController hudController, TMP_FontAsset font)
    {
        UIManager uiMgr = canvasGo.GetComponent<UIManager>();
        if (uiMgr == null) uiMgr = canvasGo.AddComponent<UIManager>();

        // 1. Win Popup
        Button nextBtn, replayWinBtn;
        GameObject winPopup = CreatePopupWithTMP(canvasGo.transform, "WinPopup", font,
            "LEVEL COMPLETED!", "All passengers are on their way!",
            "NEXT LEVEL", out nextBtn,
            "REPLAY", out replayWinBtn,
            new Color(0.2f, 0.75f, 0.35f, 0.98f));

        // 2. Lose Popup
        Button retryLoseBtn, unusedBtn;
        GameObject losePopup = CreatePopupWithTMP(canvasGo.transform, "LosePopup", font,
            "OUT OF MOVES!", "Bus stop is full! Try a different strategy.",
            "TRY AGAIN", out retryLoseBtn,
            null, out unusedBtn,
            new Color(0.85f, 0.25f, 0.25f, 0.98f));

        // 3. Settings Popup
        GameObject settingsPopup = CreateSettingsPopupWithTMP(canvasGo.transform, font);

        // Wire UIManager
        SerializedObject uiSo = new SerializedObject(uiMgr);
        SetObjProp(uiSo, "_hudController", hudController);
        SetObjProp(uiSo, "_winPopup", winPopup);
        SetObjProp(uiSo, "_losePopup", losePopup);
        SetObjProp(uiSo, "_settingsPopup", settingsPopup);
        SetObjProp(uiSo, "_nextLevelButton", nextBtn);
        SetObjProp(uiSo, "_replayWinButton", replayWinBtn);
        SetObjProp(uiSo, "_retryLoseButton", retryLoseBtn);
        uiSo.ApplyModifiedProperties();
        EditorUtility.SetDirty(uiMgr);

        return uiMgr;
    }

    private static GameObject CreateSettingsPopupWithTMP(Transform parent, TMP_FontAsset font)
    {
        Transform existing = parent.Find("SettingsPopup");
        GameObject popupGo;
        if (existing == null)
        {
            popupGo = new GameObject("SettingsPopup", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            popupGo.transform.SetParent(parent, false);
            RectTransform pRect = popupGo.GetComponent<RectTransform>();
            pRect.anchorMin = Vector2.zero;
            pRect.anchorMax = Vector2.one;
            pRect.offsetMin = Vector2.zero;
            pRect.offsetMax = Vector2.zero;

            Image bgImg = popupGo.GetComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.75f);
        }
        else
        {
            popupGo = existing.gameObject;
        }

        CanvasGroup cg = popupGo.GetComponent<CanvasGroup>();

        // Settings Card
        Transform cardTrans = popupGo.transform.Find("Card");
        GameObject cardGo;
        if (cardTrans == null)
        {
            cardGo = new GameObject("Card", typeof(RectTransform), typeof(Image));
            cardGo.transform.SetParent(popupGo.transform, false);
            RectTransform cRect = cardGo.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.5f, 0.5f);
            cRect.anchorMax = new Vector2(0.5f, 0.5f);
            cRect.pivot = new Vector2(0.5f, 0.5f);
            cRect.sizeDelta = new Vector2(850f, 750f);

            Image cImg = cardGo.GetComponent<Image>();
            cImg.color = new Color(0.18f, 0.2f, 0.28f, 0.98f);
        }
        else
        {
            cardGo = cardTrans.gameObject;
        }

        // Title
        GetOrCreateTMPText(cardGo.transform, "Title", "SETTINGS", font,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(600f, 90f),
            Color.white, 46, FontStyles.Bold);

        // Toggles Container
        Toggle soundToggle = CreateToggleItem(cardGo.transform, "SoundToggle", "Sound (SFX)", font, 110f);
        Toggle musicToggle = CreateToggleItem(cardGo.transform, "MusicToggle", "Music (BGM)", font, 0f);
        Toggle hapticToggle = CreateToggleItem(cardGo.transform, "HapticToggle", "Haptic (Rung)", font, -110f);

        // Close Button ("X")
        Button closeBtn = GetOrCreateButton(cardGo.transform, "CloseButton", "X", font,
            new Vector2(0.5f, 0.5f), new Vector2(0f, -240f), new Vector2(280f, 90f),
            new Color(0.9f, 0.35f, 0.35f, 1f), Color.white, 38);

        // Wire SettingsController
        SettingsController sc = popupGo.GetComponent<SettingsController>();
        if (sc == null) sc = popupGo.AddComponent<SettingsController>();

        SerializedObject scSo = new SerializedObject(sc);
        SetObjProp(scSo, "_soundToggle", soundToggle);
        SetObjProp(scSo, "_musicToggle", musicToggle);
        SetObjProp(scSo, "_hapticToggle", hapticToggle);
        SetObjProp(scSo, "_closeButton", closeBtn);
        SetObjProp(scSo, "_canvasGroup", cg);
        SetObjProp(scSo, "_cardRect", cardGo.GetComponent<RectTransform>());
        scSo.ApplyModifiedProperties();
        EditorUtility.SetDirty(sc);

        popupGo.SetActive(false);
        return popupGo;
    }

    private static Toggle CreateToggleItem(Transform parent, string name, string label, TMP_FontAsset font, float posY)
    {
        Transform existing = parent.Find(name);
        GameObject toggleGo;
        if (existing == null)
        {
            toggleGo = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            toggleGo.transform.SetParent(parent, false);
            RectTransform r = toggleGo.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0.5f, 0.5f);
            r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(0f, posY);
            r.sizeDelta = new Vector2(650f, 85f);
        }
        else
        {
            toggleGo = existing.gameObject;
        }

        Toggle toggle = toggleGo.GetComponent<Toggle>();

        // Label
        GetOrCreateTMPText(toggleGo.transform, "Label", label, font,
            new Vector2(0f, 0.5f), new Vector2(220f, 0f), new Vector2(400f, 70f),
            Color.white, 36, FontStyles.Normal);

        // Background
        Transform bgTrans = toggleGo.transform.Find("Background");
        GameObject bgGo;
        if (bgTrans == null)
        {
            bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(toggleGo.transform, false);
            RectTransform bRect = bgGo.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(1f, 0.5f);
            bRect.anchorMax = new Vector2(1f, 0.5f);
            bRect.pivot = new Vector2(1f, 0.5f);
            bRect.anchoredPosition = new Vector2(-40f, 0f);
            bRect.sizeDelta = new Vector2(110f, 60f);

            Image bgImg = bgGo.GetComponent<Image>();
            bgImg.color = new Color(0.3f, 0.35f, 0.45f, 1f);
        }
        else
        {
            bgGo = bgTrans.gameObject;
        }

        // Checkmark
        Transform chkTrans = bgGo.transform.Find("Checkmark");
        GameObject chkGo;
        if (chkTrans == null)
        {
            chkGo = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            chkGo.transform.SetParent(bgGo.transform, false);
            RectTransform chkRect = chkGo.GetComponent<RectTransform>();
            chkRect.anchorMin = new Vector2(0.5f, 0.5f);
            chkRect.anchorMax = new Vector2(0.5f, 0.5f);
            chkRect.pivot = new Vector2(0.5f, 0.5f);
            chkRect.anchoredPosition = Vector2.zero;
            chkRect.sizeDelta = new Vector2(80f, 44f);

            Image chkImg = chkGo.GetComponent<Image>();
            chkImg.color = new Color(0.2f, 0.85f, 0.4f, 1f);
        }
        else
        {
            chkGo = chkTrans.gameObject;
        }

        toggle.targetGraphic = bgGo.GetComponent<Image>();
        toggle.graphic = chkGo.GetComponent<Image>();
        toggle.isOn = true;

        return toggle;
    }

    private static GameObject CreatePopupWithTMP(Transform canvasTransform, string popupName, TMP_FontAsset font,
        string titleText, string subtitleText,
        string mainBtnText, out Button mainBtn,
        string subBtnText, out Button subBtn,
        Color cardColor)
    {
        Transform existingPopup = canvasTransform.Find(popupName);
        GameObject popupGo;
        if (existingPopup == null)
        {
            popupGo = new GameObject(popupName, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
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

        GetOrCreateTMPText(cardGo.transform, "Title", titleText, font,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(800f, 100f),
            Color.white, 50, FontStyles.Bold);

        GetOrCreateTMPText(cardGo.transform, "Subtitle", subtitleText, font,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(750f, 80f),
            new Color(1f, 1f, 1f, 0.9f), 32, FontStyles.Normal);

        Vector2 mainBtnPos = string.IsNullOrEmpty(subBtnText) ? new Vector2(0f, -80f) : new Vector2(0f, -40f);
        mainBtn = GetOrCreateButton(cardGo.transform, "MainButton", mainBtnText, font,
            new Vector2(0.5f, 0.5f), mainBtnPos, new Vector2(500f, 110f),
            new Color(0.95f, 0.75f, 0.1f, 1f), new Color(0.15f, 0.15f, 0.15f, 1f), 38);

        if (!string.IsNullOrEmpty(subBtnText))
        {
            subBtn = GetOrCreateButton(cardGo.transform, "SubButton", subBtnText, font,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(400f, 85f),
                new Color(1f, 1f, 1f, 0.85f), new Color(0.2f, 0.2f, 0.2f, 1f), 34);
        }
        else
        {
            subBtn = null;
        }

        popupGo.SetActive(false);
        return popupGo;
    }

    private static Button GetOrCreateButton(Transform parent, string name, string text, TMP_FontAsset font,
        Vector2 anchor, Vector2 anchoredPos, Vector2 size, Color bgColor, Color textColor, int fontSize)
    {
        Transform existing = parent.Find(name);
        GameObject btnGo;
        if (existing == null)
        {
            btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
        }
        else
        {
            btnGo = existing.gameObject;
        }

        RectTransform r = btnGo.GetComponent<RectTransform>();
        r.anchorMin = anchor;
        r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = anchoredPos;
        r.sizeDelta = size;

        Image img = btnGo.GetComponent<Image>();
        img.color = bgColor;

        Button btn = btnGo.GetComponent<Button>();

        GetOrCreateTMPText(btnGo.transform, "Text", text, font,
            new Vector2(0.5f, 0.5f), Vector2.zero, size,
            textColor, fontSize, FontStyles.Bold);

        return btn;
    }

    private static TextMeshProUGUI GetOrCreateTMPText(Transform parent, string name, string text, TMP_FontAsset font,
        Vector2 anchor, Vector2 anchoredPos, Vector2 size, Color color, int fontSize, FontStyles style)
    {
        Transform existing = parent.Find(name);
        GameObject txtGo;
        if (existing == null)
        {
            txtGo = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(parent, false);
        }
        else
        {
            txtGo = existing.gameObject;
        }

        RectTransform r = txtGo.GetComponent<RectTransform>();
        r.anchorMin = anchor;
        r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = anchoredPos;
        r.sizeDelta = size;

        TextMeshProUGUI tmp = txtGo.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;

        return tmp;
    }

    [MenuItem("Tools/Bus Jam/3. Setup AudioSource, VFX & Slots", false, 22)]
    public static void SetupAudioSourceAndSlots()
    {
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

    [MenuItem("Tools/Bus Jam/3. Upgrade Visuals (Art, Slots, Prefabs, Background)", false, 22)]
    public static void UpgradeVisuals()
    {
        ConfigureSpriteImporters("Assets/Art/Sprites/Generated");
        ConfigureSpriteImporters("Assets/Resources/Vehicles");
        AssetDatabase.Refresh();

        UpgradePassengerPrefab();
        UpgradeBusPrefabs();
        SetupSceneVisuals();

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Bus Jam] All visual upgrades applied successfully!");
    }

    private static void ConfigureSpriteImporters(string rootFolder)
    {
        if (!Directory.Exists(rootFolder)) return;

        string[] files = Directory.GetFiles(rootFolder, "*.png", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            string assetPath = file.Replace("\\", "/");
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    changed = true;
                }
                if (importer.spritePixelsPerUnit != 256)
                {
                    importer.spritePixelsPerUnit = 256;
                    changed = true;
                }
                if (importer.filterMode != FilterMode.Bilinear)
                {
                    importer.filterMode = FilterMode.Bilinear;
                    changed = true;
                }
                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    changed = true;
                }
                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    changed = true;
                }
                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }
    }

    private static void UpgradePassengerPrefab()
    {
        string path = "Assets/Prefabs/Passenger.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) return;

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null) return;

        Sprite passSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Generated/Passengers/passenger_white.png");
        SpriteRenderer sr = instance.GetComponent<SpriteRenderer>();
        if (sr == null) sr = instance.AddComponent<SpriteRenderer>();

        if (passSprite != null)
        {
            sr.sprite = passSprite;
            sr.sortingOrder = 30;
        }

        instance.transform.localScale = new Vector3(0.42f, 0.42f, 1f);

        SerializedObject so = new SerializedObject(instance.GetComponent<PassengerController>());
        SerializedProperty scaleProp = so.FindProperty("defaultScale");
        if (scaleProp != null)
        {
            scaleProp.vector3Value = new Vector3(0.42f, 0.42f, 1f);
        }
        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(instance, path);
        Object.DestroyImmediate(instance);
        Debug.Log("[Bus Jam] Passenger.prefab upgraded with original 3D cartoon silhouette!");
    }

    private static void UpgradeBusPrefabs()
    {
        UpgradeSingleBusPrefab("Assets/Prefabs/Bus_2x1.prefab", 2);
        UpgradeSingleBusPrefab("Assets/Prefabs/Bus_3x1.prefab", 3);
    }

    private static void UpgradeSingleBusPrefab(string path, int length)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) return;

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null) return;

        Transform frontGlass = instance.transform.Find("FrontGlass");
        if (frontGlass != null)
        {
            Object.DestroyImmediate(frontGlass.gameObject);
        }

        for (int i = 0; i < 3; i++)
        {
            Transform seat = instance.transform.Find($"Seat_{i}");
            if (seat != null)
            {
                SpriteRenderer seatSr = seat.GetComponent<SpriteRenderer>();
                if (seatSr != null)
                {
                    Object.DestroyImmediate(seatSr);
                }
            }
        }

        Transform s0 = instance.transform.Find("Seat_0");
        Transform s1 = instance.transform.Find("Seat_1");
        Transform s2 = instance.transform.Find("Seat_2");
        if (length == 3)
        {
            if (s0 != null) s0.localPosition = new Vector3(-0.75f, 0f, 0f);
            if (s1 != null) s1.localPosition = new Vector3(-0.15f, 0f, 0f);
            if (s2 != null) s2.localPosition = new Vector3(0.45f, 0f, 0f);
        }
        else
        {
            if (s0 != null) s0.localPosition = new Vector3(-0.50f, 0f, 0f);
            if (s1 != null) s1.localPosition = new Vector3(-0.15f, 0f, 0f);
            if (s2 != null) s2.localPosition = new Vector3(0.20f, 0f, 0f);
        }

        SpriteRenderer rootSr = instance.GetComponent<SpriteRenderer>();
        if (rootSr == null) rootSr = instance.AddComponent<SpriteRenderer>();
        rootSr.sortingOrder = 10;
        string vType = length == 3 ? "bus" : "car";
        Sprite defaultSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Sprites/Generated/Vehicles/{vType}_seat/{vType}_seat_blue_right.png");
        if (defaultSprite != null)
        {
            rootSr.sprite = defaultSprite;
            rootSr.color = Color.white;
        }

        float arrowX = length == 3 ? 0.95f : 0.55f;
        Transform arrowTr = instance.transform.Find("DirectionArrow");
        if (arrowTr == null)
        {
            GameObject arrowGo = new GameObject("DirectionArrow");
            arrowTr = arrowGo.transform;
            arrowTr.SetParent(instance.transform, false);
        }
        arrowTr.localPosition = new Vector3(arrowX, 0f, 0f);
        arrowTr.localRotation = Quaternion.identity;
        arrowTr.localScale = new Vector3(0.42f, 0.42f, 1f);

        SpriteRenderer arrowSr = arrowTr.GetComponent<SpriteRenderer>();
        if (arrowSr == null) arrowSr = arrowTr.gameObject.AddComponent<SpriteRenderer>();
        arrowSr.sortingOrder = 15;
        Sprite arrowSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Sprites/Generated/Vehicles/{vType}_arrow/{vType}_arrow_blue_right.png");
        if (arrowSprite != null)
        {
            arrowSr.sprite = arrowSprite;
            arrowSr.color = Color.white;
        }

        BoxCollider2D col = instance.GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.size = new Vector2(length, 1f);
            col.offset = Vector2.zero;
        }

        BusController bc = instance.GetComponent<BusController>();
        if (bc != null)
        {
            SerializedObject so = new SerializedObject(bc);
            SetObjProp(so, "bodyRenderer", rootSr);
            SetObjProp(so, "arrowRenderer", arrowSr);
            SerializedProperty scaleProp = so.FindProperty("defaultScale");
            Vector3 targetScale = new Vector3(0.95f, 0.95f, 1f);
            if (scaleProp != null)
            {
                scaleProp.vector3Value = targetScale;
            }
            instance.transform.localScale = targetScale;
            so.ApplyModifiedProperties();
        }

        PrefabUtility.SaveAsPrefabAsset(instance, path);
        Object.DestroyImmediate(instance);
        Debug.Log($"[Bus Jam] {path} upgraded with original vehicle art, arrow, and seat anchors!");
    }

    private static void SetupSceneVisuals()
    {
        GameObject bgGo = GameObject.Find("SceneBackground");
        if (bgGo == null)
        {
            bgGo = new GameObject("SceneBackground");
        }
        bgGo.transform.position = new Vector3(0f, 0f, 0f);
        bgGo.transform.localScale = new Vector3(0.78f, 0.78f, 1f);

        SpriteRenderer bgSr = bgGo.GetComponent<SpriteRenderer>();
        if (bgSr == null) bgSr = bgGo.AddComponent<SpriteRenderer>();
        bgSr.sortingOrder = -100;
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Generated/Background/background.png");
        if (bgSprite != null)
        {
            bgSr.sprite = bgSprite;
            bgSr.color = Color.white;
        }

        Sprite slotSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Generated/Park/park_slot_unlock.png");
        for (int i = 0; i <= 4; i++)
        {
            GameObject slotGo = GameObject.Find($"Slot_{i}");
            if (slotGo != null)
            {
                SpriteRenderer slotSr = slotGo.GetComponent<SpriteRenderer>();
                if (slotSr == null) slotSr = slotGo.AddComponent<SpriteRenderer>();
                slotSr.sortingOrder = -10;
                if (slotSprite != null)
                {
                    slotSr.sprite = slotSprite;
                    slotSr.color = Color.white;
                }
                slotGo.transform.localScale = new Vector3(1.0f, 0.95f, 1.0f);
            }
        }
        Debug.Log("[Bus Jam] SceneBackground and BusStopSlots configured in scene!");
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
