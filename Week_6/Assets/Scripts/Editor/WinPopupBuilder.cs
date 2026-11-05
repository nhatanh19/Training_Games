#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Rebuilds WinPopup inside PopupCanvas.prefab to match the target 3D card design.</summary>
public static class WinPopupBuilder
{
    private const string PrefabPath = "Assets/Prefabs/UI/PopupCanvas.prefab";
    private const string ResPrefabPath = "Assets/Resources/Prefabs/UI/PopupCanvas.prefab";
    private const string SpriteDir = "Assets/Art/Sprites/UI/";

    [MenuItem("Tools/Bus Jam/Rebuild Win Popup UI (Target Design)")]
    public static void BuildWinPopup()
    {
        TMP_FontAsset font = SettingsPopupBuilder.GetOrCreateDynamicFont();
        if (font == null) { Debug.LogError("[WinPopupBuilder] Font missing!"); return; }

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Rebuild(root, font);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            if (System.IO.File.Exists(ResPrefabPath))
                PrefabUtility.SaveAsPrefabAsset(root, ResPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[WinPopupBuilder] Successfully rebuilt WinPopup!");
    }

    private static void Rebuild(GameObject root, TMP_FontAsset font)
    {
        Transform existing = root.transform.Find("WinPopup");
        GameObject popup;
        if (existing != null)
        {
            popup = existing.gameObject;
            for (int i = popup.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(popup.transform.GetChild(i).gameObject);
        }
        else
        {
            popup = new GameObject("WinPopup", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
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
        Sprite faceSp = Load("win_face_happy.png");
        Sprite greenBtnSp = Load("lose_btn_green.png");
        Sprite yellowBtnSp = Load("ui_btn_yellow_replay.png");
        Sprite pillSp = Load("hud_coin_pill.png");
        Sprite coinSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/coin_icon.png");

        Color darkGreen = new Color(0.10f, 0.45f, 0.22f, 1f);

        // 1. Cream Card (720 x 872 matches 760:920 aspect ratio)
        GameObject card = New("Card", popup.transform, typeof(Image));
        Place(card, Vector2.zero, new Vector2(720f, 872f));
        Image ci = card.GetComponent<Image>();
        ci.sprite = cardSp; ci.preserveAspect = true;

        // 2. Header Banner
        GameObject header = New("HeaderBanner", card.transform, typeof(Image));
        Place(header, new Vector2(0f, 405f), new Vector2(600f, 131f));
        Image hi = header.GetComponent<Image>();
        hi.sprite = headerSp; hi.preserveAspect = true;
        TextMeshProUGUI title = Text("TitleText", header.transform, "HOÀN THÀNH!", font, 46, Color.white);
        Place(title.gameObject, new Vector2(0f, 4f), new Vector2(540f, 84f));
        title.enableAutoSizing = true; title.fontSizeMin = 24; title.fontSizeMax = 46;

        // 3. Happy Face Icon
        GameObject face = New("FaceIcon", card.transform, typeof(Image));
        Place(face, new Vector2(0f, 145f), new Vector2(280f, 245f));
        Image fi = face.GetComponent<Image>();
        fi.sprite = faceSp; fi.preserveAspect = true; fi.raycastTarget = false;

        // 4. Subtitle text
        TextMeshProUGUI subtitle = Text("SubtitleText", card.transform, "TẤT CẢ HÀNH KHÁCH ĐÃ LÊN XE!", font, 28, darkGreen);
        Place(subtitle.gameObject, new Vector2(0f, -15f), new Vector2(600f, 55f));
        subtitle.enableAutoSizing = true; subtitle.fontSizeMin = 18; subtitle.fontSizeMax = 28;

        // 5. Coin Reward Pill
        GameObject rewardPill = New("RewardPill", card.transform, typeof(Image));
        Place(rewardPill, new Vector2(0f, -95f), new Vector2(240f, 76f));
        Image rpi = rewardPill.GetComponent<Image>();
        rpi.sprite = pillSp; rpi.preserveAspect = true; rpi.raycastTarget = false;

        GameObject coinIcon = New("CoinIcon", rewardPill.transform, typeof(Image));
        Place(coinIcon, new Vector2(-48f, 0f), new Vector2(54f, 54f));
        Image cii = coinIcon.GetComponent<Image>();
        cii.sprite = coinSp; cii.preserveAspect = true; cii.raycastTarget = false;

        TextMeshProUGUI rewardText = Text("RewardText", rewardPill.transform, "+8", font, 40, new Color(1f, 0.88f, 0.25f, 1f));
        Place(rewardText.gameObject, new Vector2(28f, 0f), new Vector2(110f, 60f));

        // 6. Main Button ("TIẾP THEO" / "NEXT LEVEL") - Green 3D Button on the Right
        GameObject main = New("MainButton", card.transform, typeof(Image), typeof(Button));
        Place(main, new Vector2(160f, -245f), new Vector2(290f, 116f));
        Image mi = main.GetComponent<Image>();
        mi.sprite = greenBtnSp; mi.preserveAspect = true;
        main.GetComponent<Button>().targetGraphic = mi;
        TextMeshProUGUI nextText = Text("Text", main.transform, "TIẾP THEO", font, 36, Color.white);
        Place(nextText.gameObject, new Vector2(0f, 4f), new Vector2(250f, 75f));
        nextText.enableWordWrapping = false;
        nextText.enableAutoSizing = true; nextText.fontSizeMin = 20; nextText.fontSizeMax = 36;

        // 7. Sub Button ("CHƠI LẠI" / "REPLAY") - Yellow 3D Button on the Left
        GameObject sub = New("SubButton", card.transform, typeof(Image), typeof(Button));
        Place(sub, new Vector2(-160f, -245f), new Vector2(290f, 116f));
        Image si = sub.GetComponent<Image>();
        si.sprite = yellowBtnSp; si.preserveAspect = true;
        sub.GetComponent<Button>().targetGraphic = si;
        TextMeshProUGUI replayText = Text("Text", sub.transform, "CHƠI LẠI", font, 36, Color.white);
        Place(replayText.gameObject, new Vector2(0f, 4f), new Vector2(250f, 75f));
        replayText.enableWordWrapping = false;
        replayText.enableAutoSizing = true; replayText.fontSizeMin = 20; replayText.fontSizeMax = 36;

        // 8. Wire WinPopupLocalizer
        WinPopupLocalizer loc = popup.GetComponent<WinPopupLocalizer>();
        if (loc == null) loc = popup.AddComponent<WinPopupLocalizer>();
        SerializedObject so = new SerializedObject(loc);
        so.FindProperty("_titleText").objectReferenceValue = title;
        so.FindProperty("_subtitleText").objectReferenceValue = subtitle;
        so.FindProperty("_rewardText").objectReferenceValue = rewardText;
        so.FindProperty("_nextButtonText").objectReferenceValue = nextText;
        so.FindProperty("_replayButtonText").objectReferenceValue = replayText;
        so.ApplyModifiedProperties();

        // 9. Wire UIManager
        UIManager uim = root.GetComponent<UIManager>();
        if (uim == null) uim = root.GetComponentInChildren<UIManager>(true);
        if (uim != null)
        {
            SerializedObject us = new SerializedObject(uim);
            us.FindProperty("_winPopup").objectReferenceValue = popup;
            us.FindProperty("_nextLevelButton").objectReferenceValue = main.GetComponent<Button>();
            us.FindProperty("_replayWinButton").objectReferenceValue = sub.GetComponent<Button>();
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

    private static TextMeshProUGUI Text(string name, Transform parent, string text, TMP_FontAsset font, float size, Color color)
    {
        GameObject go = New(name, parent, typeof(TextMeshProUGUI));
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.font = font; t.text = text; t.fontSize = size; t.fontStyle = FontStyles.Bold;
        t.color = color; t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false;
        return t;
    }
}
#endif
