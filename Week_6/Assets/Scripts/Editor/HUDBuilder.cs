#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Rebuilds the HUD (coin pill, level title, pause button, remaining-passenger counter)
/// inside HUDCanvas.prefab to match the target design. The Settings popup is not touched.
/// </summary>
public static class HUDBuilder
{
    private const string HudPrefabPath = "Assets/Prefabs/UI/HUDCanvas.prefab";
    private const string SpriteDir = "Assets/Art/Sprites/UI/";

    [MenuItem("Tools/Bus Jam/Rebuild HUD UI (Target Design)")]
    public static void BuildHUD()
    {
        TMP_FontAsset font = SettingsPopupBuilder.GetOrCreateDynamicFont();
        if (font == null)
        {
            Debug.LogError("[HUDBuilder] Dynamic font could not be created!");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(HudPrefabPath);
        try
        {
            Rebuild(root, font);
            PrefabUtility.SaveAsPrefabAsset(root, HudPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[HUDBuilder] Successfully rebuilt HUD!");
        BusJamSceneWiring.WireSceneReferences();
    }

    private static void Rebuild(GameObject root, TMP_FontAsset font)
    {
        HUDController hud = root.GetComponent<HUDController>();
        if (hud == null) hud = root.AddComponent<HUDController>();

        // Remove previous HUD content except the safe-area panel itself.
        Transform panelTrans = root.transform.Find("HUDPanel");
        GameObject panel;
        if (panelTrans == null)
        {
            panel = new GameObject("HUDPanel", typeof(RectTransform));
            panel.transform.SetParent(root.transform, false);
            RectTransform r = panel.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(1f, 1f);
            r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = new Vector2(0f, 240f);
        }
        else
        {
            panel = panelTrans.gameObject;
        }
        if (panel.GetComponent<SafeAreaFitter>() == null) panel.AddComponent<SafeAreaFitter>();

        for (int i = panel.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(panel.transform.GetChild(i).gameObject);

        Transform oldSign = root.transform.Find("PassengerCounterSign");
        if (oldSign != null) Object.DestroyImmediate(oldSign.gameObject);

        Sprite pillSprite = Load("hud_coin_pill.png");
        Sprite plusSprite = Load("hud_btn_plus.png");
        Sprite pauseSprite = Load("hud_btn_pause.png");
        Sprite coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/coin_icon.png");

        // ---- Coin pill (top-left) ----
        GameObject coin = NewUI("CoinContainer", panel.transform, typeof(Image));
        SetRect(coin, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -24f), new Vector2(380f, 100f));
        Image coinBg = coin.GetComponent<Image>();
        coinBg.sprite = pillSprite;
        coinBg.raycastTarget = false;

        GameObject coinIcon = NewUI("CoinIcon", coin.transform, typeof(Image));
        SetRect(coinIcon, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(56f, 0f), new Vector2(68f, 68f));
        Image coinIconImg = coinIcon.GetComponent<Image>();
        coinIconImg.sprite = coinSprite;
        coinIconImg.preserveAspect = true;
        coinIconImg.raycastTarget = false;

        TextMeshProUGUI coinText = NewText("CoinText", coin.transform, "0", font, 44, FontStyles.Bold, Color.white);
        SetRect(coinText.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-6f, 0f), new Vector2(170f, 70f));
        coinText.alignment = TextAlignmentOptions.Center;

        GameObject plus = NewUI("PlusButton", coin.transform, typeof(Image), typeof(Button));
        SetRect(plus, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-52f, 0f), new Vector2(72f, 72f));
        Image plusImg = plus.GetComponent<Image>();
        plusImg.sprite = plusSprite;
        plusImg.preserveAspect = true;
        Button plusBtn = plus.GetComponent<Button>();
        plusBtn.targetGraphic = plusImg;

        // ---- Level title (top-centre) ----
        TextMeshProUGUI level = NewText("LevelText", panel.transform, "Cấp độ 1", font, 52, FontStyles.Bold,
            new Color(0.10f, 0.14f, 0.30f, 1f));
        SetRect(level.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -74f), new Vector2(420f, 80f));
        level.alignment = TextAlignmentOptions.Center;

        // ---- Pause button (top-right) ----
        GameObject pause = NewUI("PauseButton", panel.transform, typeof(Image), typeof(Button));
        SetRect(pause, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -20f), new Vector2(110f, 110f));
        Image pauseImg = pause.GetComponent<Image>();
        pauseImg.sprite = pauseSprite;
        pauseImg.preserveAspect = true;
        pause.GetComponent<Button>().targetGraphic = pauseImg;

        // ---- Remaining passenger counter, overlaid on the baked blue sign ----
        GameObject sign = NewUI("PassengerCounterSign", root.transform, typeof(PassengerCounterSign));
        SetRect(sign, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(170f, 110f));

        TextMeshProUGUI count = NewText("CountText", sign.transform, "0", font, 60, FontStyles.Bold, Color.white);
        StretchRect(count.gameObject, new Vector2(0.04f, 0.42f), new Vector2(0.96f, 0.98f));
        count.enableAutoSizing = true;
        count.fontSizeMin = 10;
        count.fontSizeMax = 80;

        TextMeshProUGUI label = NewText("LabelText", sign.transform, "CÒN LẠI", font, 26, FontStyles.Bold,
            new Color(0.17f, 0.40f, 0.85f, 1f));
        StretchRect(label.gameObject, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.42f));
        label.enableAutoSizing = true;
        label.fontSizeMin = 8;
        label.fontSizeMax = 34;

        SerializedObject signSo = new SerializedObject(sign.GetComponent<PassengerCounterSign>());
        signSo.FindProperty("_signRoot").objectReferenceValue = sign.GetComponent<RectTransform>();
        signSo.FindProperty("_countText").objectReferenceValue = count;
        signSo.FindProperty("_labelText").objectReferenceValue = label;
        signSo.ApplyModifiedProperties();

        // ---- Wire HUDController ----
        SerializedObject hudSo = new SerializedObject(hud);
        hudSo.FindProperty("_levelText").objectReferenceValue = level;
        hudSo.FindProperty("_coinText").objectReferenceValue = coinText;
        hudSo.FindProperty("_coinTargetTransform").objectReferenceValue = coin.transform;
        hudSo.FindProperty("_restartButton").objectReferenceValue = null;
        hudSo.FindProperty("_settingsButton").objectReferenceValue = pause.GetComponent<Button>();
        hudSo.FindProperty("_addCoinButton").objectReferenceValue = plusBtn;
        hudSo.ApplyModifiedProperties();
        EditorUtility.SetDirty(hud);
    }

    private static Sprite Load(string file)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + file);
    }

    private static GameObject NewUI(string name, Transform parent, params System.Type[] comps)
    {
        System.Type[] all = new System.Type[comps.Length + 1];
        all[0] = typeof(RectTransform);
        System.Array.Copy(comps, 0, all, 1, comps.Length);
        GameObject go = new GameObject(name, all);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void SetRect(GameObject go, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = anchor;
        r.anchorMax = anchor;
        r.pivot = pivot;
        r.anchoredPosition = pos;
        r.sizeDelta = size;
    }

    private static void StretchRect(GameObject go, Vector2 min, Vector2 max)
    {
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = min;
        r.anchorMax = max;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    private static TextMeshProUGUI NewText(string name, Transform parent, string text, TMP_FontAsset font,
        float size, FontStyles style, Color color)
    {
        GameObject go = NewUI(name, parent, typeof(TextMeshProUGUI));
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }
}
#endif
