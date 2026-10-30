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
                       GameObject.Find("HUDCanvas") == null ||
                       GameObject.Find("PopupCanvas") == null;

        if (needsVFX || needsUI)
        {
            Debug.Log("[Bus Jam] Auto-building Task 1 UI Architecture & Prefabs...");
            BuildAllAndSetupScene();
        }
    }

    [MenuItem("Tools/Bus Jam/Build All Prefabs and Setup Scene", false, 1)]
    public static void BuildAllAndSetupScene()
    {
                GenerateVFXPrefabs();
        BuildTask1UIArchitecture();
        SetupAudioSourceAndSlots();

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
        PrefabUtility.SaveAsPrefabAssetAndConnect(vfxCanvasGo, UIDir + "/VFXCanvas.prefab", InteractionMode.AutomatedAction);

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

        return go;
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
