#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Builds ContinuePopup (opened by the HUD "+" button) inside PopupCanvas.prefab.</summary>
public static class ContinuePopupBuilder
{
    private const string PrefabPath = "Assets/Prefabs/UI/PopupCanvas.prefab";
    private const string SpriteDir = "Assets/Art/Sprites/UI/";

    [MenuItem("Tools/Bus Jam/Rebuild Continue Popup UI (Target Design)")]
    public static void BuildContinuePopup()
    {
        TMP_FontAsset font = SettingsPopupBuilder.GetOrCreateDynamicFont();
        if (font == null) { Debug.LogError("[ContinuePopupBuilder] Font missing!"); return; }

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Rebuild(root, font);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ContinuePopupBuilder] Successfully rebuilt ContinuePopup!");
        BusJamSceneWiring.WireSceneReferences();
    }

    private static void Rebuild(GameObject root, TMP_FontAsset font)
    {
        Transform existing = root.transform.Find("ContinuePopup");
        GameObject popup;
        if (existing != null)
        {
            popup = existing.gameObject;
            for (int i = popup.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(popup.transform.GetChild(i).gameObject);
        }
        else
        {
            popup = new GameObject("ContinuePopup", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            popup.transform.SetParent(root.transform, false);
        }
        popup.SetActive(false);

        RectTransform pr = popup.GetComponent<RectTransform>();
        pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one;
        pr.offsetMin = Vector2.zero; pr.offsetMax = Vector2.zero;
        Image dim = popup.GetComponent<Image>();
        dim.sprite = null;
        dim.color = new Color(0f, 0f, 0f, 0.72f);
        dim.raycastTarget = true;

        Sprite cardSp = Load("ui_settings_card_bg.png");
        Sprite headerSp = Load("ui_settings_header_banner.png");
        Sprite closeSp = Load("ui_btn_close_red.png");
        Sprite panelSp = Load("continue_reward_panel.png");
        Sprite greenSp = Load("lose_btn_green.png");
        Sprite yellowSp = Load("ui_btn_yellow_replay.png");
        Sprite videoSp = Load("continue_icon_video.png");
        Sprite coinSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/coin_icon.png");

        Color green = new Color(0.10f, 0.45f, 0.22f, 1f);

        GameObject card = New("Card", popup.transform, typeof(Image));
        Place(card, Vector2.zero, new Vector2(720f, 872f));
        Image ci = card.GetComponent<Image>();
        ci.sprite = cardSp; ci.preserveAspect = true;

        GameObject header = New("HeaderBanner", card.transform, typeof(Image));
        Place(header, new Vector2(0f, 405f), new Vector2(600f, 131f));
        Image hi = header.GetComponent<Image>();
        hi.sprite = headerSp; hi.preserveAspect = true;
        TextMeshProUGUI title = Text("TitleText", header.transform, "TIẾP TỤC", font, 46, FontStyles.Bold, Color.white);
        Place(title.gameObject, new Vector2(0f, 4f), new Vector2(500f, 80f));

        GameObject close = New("CloseButton", card.transform, typeof(Image), typeof(Button));
        Place(close, new Vector2(330f, 415f), new Vector2(110f, 110f));
        Image cli = close.GetComponent<Image>();
        cli.sprite = closeSp; cli.preserveAspect = true;
        close.GetComponent<Button>().targetGraphic = cli;

        GameObject panel = New("RewardPanel", card.transform, typeof(Image));
        Place(panel, new Vector2(0f, 120f), new Vector2(560f, 250f));
        Image pi = panel.GetComponent<Image>();
        pi.sprite = panelSp; pi.preserveAspect = true; pi.raycastTarget = false;

        TextMeshProUGUI headline = Text("HeadlineText", card.transform, "HỒI SINH", font, 64, FontStyles.Bold,
            new Color(0.82f, 0.12f, 0.12f, 1f));
        Place(headline.gameObject, new Vector2(0f, -55f), new Vector2(600f, 90f));

        TextMeshProUGUI desc = Text("DescriptionText", card.transform, "VÀ MỞ KHÓA MỘT CHỖ ĐẬU XE", font, 30,
            FontStyles.Bold, green);
        Place(desc.gameObject, new Vector2(0f, -120f), new Vector2(640f, 60f));
        desc.enableAutoSizing = true; desc.fontSizeMin = 18; desc.fontSizeMax = 30;

        // Coin button (left)
        GameObject coinBtn = New("CoinButton", card.transform, typeof(Image), typeof(Button));
        Place(coinBtn, new Vector2(-165f, -225f), new Vector2(290f, 116f));
        Image cbi = coinBtn.GetComponent<Image>();
        cbi.sprite = greenSp; cbi.preserveAspect = true;
        coinBtn.GetComponent<Button>().targetGraphic = cbi;
        GameObject coinIcon = New("CoinIcon", coinBtn.transform, typeof(Image));
        Place(coinIcon, new Vector2(-70f, 4f), new Vector2(64f, 64f));
        Image cii = coinIcon.GetComponent<Image>();
        cii.sprite = coinSp; cii.preserveAspect = true; cii.raycastTarget = false;
        TextMeshProUGUI cost = Text("CostText", coinBtn.transform, "20", font, 46, FontStyles.Bold, Color.white);
        Place(cost.gameObject, new Vector2(35f, 5f), new Vector2(140f, 70f));

        // Ad button (right)
        GameObject adBtn = New("AdButton", card.transform, typeof(Image), typeof(Button));
        Place(adBtn, new Vector2(170f, -225f), new Vector2(330f, 101f));
        Image abi = adBtn.GetComponent<Image>();
        abi.sprite = yellowSp; abi.preserveAspect = true;
        adBtn.GetComponent<Button>().targetGraphic = abi;
        GameObject vid = New("VideoIcon", adBtn.transform, typeof(Image));
        Place(vid, new Vector2(-115f, 4f), new Vector2(66f, 66f));
        Image vi = vid.GetComponent<Image>();
        vi.sprite = videoSp; vi.preserveAspect = true; vi.raycastTarget = false;
        TextMeshProUGUI adText = Text("AdText", adBtn.transform, "SỬ DỤNG", font, 36, FontStyles.Bold, Color.white);
        Place(adText.gameObject, new Vector2(30f, 4f), new Vector2(210f, 70f));
        adText.enableWordWrapping = false;
        adText.enableAutoSizing = true; adText.fontSizeMin = 18; adText.fontSizeMax = 34;

        TextMeshProUGUI notice = Text("NoticeText", card.transform, "KHÔNG ĐỦ TIỀN", font, 36, FontStyles.Bold,
            new Color(0.85f, 0.1f, 0.1f, 1f));
        Place(notice.gameObject, new Vector2(0f, -320f), new Vector2(620f, 60f));
        notice.enableWordWrapping = false;
        notice.gameObject.SetActive(false);

        ContinuePopupController ctrl = popup.GetComponent<ContinuePopupController>();
        if (ctrl == null) ctrl = popup.AddComponent<ContinuePopupController>();
        SerializedObject so = new SerializedObject(ctrl);
        so.FindProperty("_closeButton").objectReferenceValue = close.GetComponent<Button>();
        so.FindProperty("_coinButton").objectReferenceValue = coinBtn.GetComponent<Button>();
        so.FindProperty("_adButton").objectReferenceValue = adBtn.GetComponent<Button>();
        so.FindProperty("_titleText").objectReferenceValue = title;
        so.FindProperty("_headlineText").objectReferenceValue = headline;
        so.FindProperty("_descriptionText").objectReferenceValue = desc;
        so.FindProperty("_coinCostText").objectReferenceValue = cost;
        so.FindProperty("_adText").objectReferenceValue = adText;
        so.FindProperty("_noticeText").objectReferenceValue = notice;
        so.ApplyModifiedProperties();

        UIManager uim = root.GetComponent<UIManager>();
        if (uim == null) uim = root.GetComponentInChildren<UIManager>(true);
        if (uim != null)
        {
            SerializedObject us = new SerializedObject(uim);
            us.FindProperty("_continuePopup").objectReferenceValue = popup;
            us.ApplyModifiedProperties();
        }
    }

    private static Sprite Load(string f) => AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + f);

    private static GameObject New(string name, Transform parent, params System.Type[] comps)
    {
        System.Type[] all = new System.Type[comps.Length + 1];
        all[0] = typeof(RectTransform);
        System.Array.Copy(comps, 0, all, 1, comps.Length);
        GameObject go = new GameObject(name, all);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void Place(GameObject go, Vector2 pos, Vector2 size)
    {
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;
    }

    private static TextMeshProUGUI Text(string name, Transform parent, string text, TMP_FontAsset font,
        float size, FontStyles style, Color color)
    {
        GameObject go = New(name, parent, typeof(TextMeshProUGUI));
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.font = font; t.text = text; t.fontSize = size; t.fontStyle = style;
        t.color = color; t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false;
        return t;
    }
}
#endif
