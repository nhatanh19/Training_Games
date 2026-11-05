#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Rebuilds LosePopup inside PopupCanvas.prefab to match the target design.</summary>
public static class LosePopupBuilder
{
    private const string PrefabPath = "Assets/Prefabs/UI/PopupCanvas.prefab";
    private const string ResPrefabPath = "Assets/Resources/Prefabs/UI/PopupCanvas.prefab";
    private const string SpriteDir = "Assets/Art/Sprites/UI/";

    [MenuItem("Tools/Bus Jam/Rebuild Lose Popup UI (Target Design)")]
    public static void BuildLosePopup()
    {
        TMP_FontAsset font = SettingsPopupBuilder.GetOrCreateDynamicFont();
        if (font == null) { Debug.LogError("[LosePopupBuilder] Font missing!"); return; }

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
        Debug.Log("[LosePopupBuilder] Successfully rebuilt LosePopup!");
    }

    private static void Rebuild(GameObject root, TMP_FontAsset font)
    {
        Transform existing = root.transform.Find("LosePopup");
        GameObject popup;
        if (existing != null)
        {
            popup = existing.gameObject;
            for (int i = popup.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(popup.transform.GetChild(i).gameObject);
        }
        else
        {
            popup = new GameObject("LosePopup", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
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
        Sprite faceSp = Load("lose_face_angry.png");
        Sprite btnSp = Load("lose_btn_green.png");

        GameObject card = New("Card", popup.transform, typeof(Image));
        Place(card, Vector2.zero, new Vector2(740f, 800f));
        Image ci = card.GetComponent<Image>();
        ci.sprite = cardSp; ci.preserveAspect = true;

        GameObject header = New("HeaderBanner", card.transform, typeof(Image));
        Place(header, new Vector2(0f, 400f - 20f), new Vector2(620f, 135f));
        Image hi = header.GetComponent<Image>();
        hi.sprite = headerSp; hi.preserveAspect = true;
        TextMeshProUGUI title = Text("TitleText", header.transform, "CẤP ĐỘ THẤT BẠI", font, 46, Color.white);
        Place(title.gameObject, new Vector2(0f, 4f), new Vector2(560f, 90f));
        title.enableAutoSizing = true; title.fontSizeMin = 24; title.fontSizeMax = 46;

        GameObject face = New("FaceIcon", card.transform, typeof(Image));
        Place(face, new Vector2(0f, 40f), new Vector2(340f, 298f));
        Image fi = face.GetComponent<Image>();
        fi.sprite = faceSp; fi.preserveAspect = true; fi.raycastTarget = false;

        GameObject main = New("MainButton", card.transform, typeof(Image), typeof(Button));
        Place(main, new Vector2(0f, -250f), new Vector2(330f, 132f));
        Image mi = main.GetComponent<Image>();
        mi.sprite = btnSp; mi.preserveAspect = true;
        main.GetComponent<Button>().targetGraphic = mi;
        TextMeshProUGUI btnText = Text("Text", main.transform, "THỬ LẠI", font, 42, Color.white);
        Place(btnText.gameObject, new Vector2(0f, 4f), new Vector2(290f, 80f));

        LosePopupLocalizer loc = popup.GetComponent<LosePopupLocalizer>();
        if (loc == null) loc = popup.AddComponent<LosePopupLocalizer>();
        SerializedObject so = new SerializedObject(loc);
        so.FindProperty("_titleText").objectReferenceValue = title;
        so.FindProperty("_buttonText").objectReferenceValue = btnText;
        so.ApplyModifiedProperties();

        UIManager uim = root.GetComponent<UIManager>();
        if (uim == null) uim = root.GetComponentInChildren<UIManager>(true);
        if (uim != null)
        {
            SerializedObject us = new SerializedObject(uim);
            us.FindProperty("_losePopup").objectReferenceValue = popup;
            us.FindProperty("_retryLoseButton").objectReferenceValue = main.GetComponent<Button>();
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
