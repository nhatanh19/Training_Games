#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SettingsPopupBuilder
{
    private const string PopupPrefabPath = "Assets/Prefabs/UI/PopupCanvas.prefab";
    private const string SpriteDir = "Assets/Art/Sprites/UI/";

    [MenuItem("Tools/Bus Jam/Rebuild Settings Popup UI (Target Design)")]
    public static void BuildSettingsPopup()
    {
        Debug.Log("[SettingsPopupBuilder] Starting rebuild of SettingsPopup...");

        TMP_FontAsset defaultFont = GetOrCreateDynamicFont();
        if (defaultFont == null)
        {
            Debug.LogError("[SettingsPopupBuilder] Dynamic font could not be created!");
            return;
        }

        RebuildInPrefab(PopupPrefabPath, defaultFont);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SettingsPopupBuilder] Successfully rebuilt and saved SettingsPopup!");
        BusJamSceneWiring.WireSceneReferences();
    }

    public static TMP_FontAsset GetOrCreateDynamicFont()
    {
        const string path = "Assets/TextMesh Pro/Resources/Fonts & Materials/SettingsDynamic SDF.asset";
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (existing != null) return existing;

        Font ttf = AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
        if (ttf == null) return null;
        TMP_FontAsset fa = TMP_FontAsset.CreateFontAsset(ttf, 90, 9,
            UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        fa.name = "SettingsDynamic SDF";
        AssetDatabase.CreateAsset(fa, path);
        fa.material.name = "SettingsDynamic SDF Material";
        AssetDatabase.AddObjectToAsset(fa.material, fa);
        fa.atlasTextures[0].name = "SettingsDynamic Atlas";
        AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssets();
        return fa;
    }
    private static void RebuildInPrefab(string prefabPath, TMP_FontAsset font)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null)
        {
            Debug.LogError($"[SettingsPopupBuilder] Failed to load prefab contents: {prefabPath}");
            return;
        }

        try
        {
            Transform existingPopup = root.transform.Find("SettingsPopup");
            GameObject popupGo;
            if (existingPopup != null)
            {
                popupGo = existingPopup.gameObject;

                for (int i = popupGo.transform.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(popupGo.transform.GetChild(i).gameObject);
                }
            }
            else
            {
                popupGo = new GameObject("SettingsPopup", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                popupGo.transform.SetParent(root.transform, false);
            }

            popupGo.SetActive(false);

            RectTransform pRect = popupGo.GetComponent<RectTransform>();
            pRect.anchorMin = Vector2.zero;
            pRect.anchorMax = Vector2.one;
            pRect.offsetMin = Vector2.zero;
            pRect.offsetMax = Vector2.zero;

            Image bgImg = popupGo.GetComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.72f);
            bgImg.raycastTarget = true;

            CanvasGroup cg = popupGo.GetComponent<CanvasGroup>();

            Sprite cardBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "ui_settings_card_bg.png");
            Sprite headerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "ui_settings_header_banner.png");
            Sprite closeBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "ui_btn_close_red.png");
            Sprite musicIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "ui_icon_music.png");
            Sprite soundIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "ui_icon_sound.png");
            Sprite vibIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "ui_icon_vibration.png");
            Sprite langIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "ui_icon_language.png");
            Sprite trackSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "ui_toggle_track.png");
            Sprite thumbGreenSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "ui_toggle_thumb_green.png");
            Sprite thumbGreySprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "ui_toggle_thumb_grey.png");
            Sprite cyanBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "ui_btn_cyan_pill.png");
            Sprite replayBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "ui_btn_yellow_replay.png");

            GameObject cardGo = new GameObject("Card", typeof(RectTransform), typeof(Image));
            cardGo.transform.SetParent(popupGo.transform, false);
            RectTransform cRect = cardGo.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.5f, 0.5f);
            cRect.anchorMax = new Vector2(0.5f, 0.5f);
            cRect.pivot = new Vector2(0.5f, 0.5f);
            cRect.sizeDelta = new Vector2(740f, 920f);
            cRect.anchoredPosition = new Vector2(0f, -20f);

            Image cImg = cardGo.GetComponent<Image>();
            cImg.sprite = cardBgSprite;
            cImg.type = Image.Type.Simple;
            cImg.color = Color.white;
            cImg.preserveAspect = true;

            GameObject headerGo = new GameObject("HeaderBanner", typeof(RectTransform), typeof(Image));
            headerGo.transform.SetParent(cardGo.transform, false);
            RectTransform hRect = headerGo.GetComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0.5f, 1f);
            hRect.anchorMax = new Vector2(0.5f, 1f);
            hRect.pivot = new Vector2(0.5f, 0.5f);
            hRect.anchoredPosition = new Vector2(0f, -20f);
            hRect.sizeDelta = new Vector2(560f, 125f);

            Image hImg = headerGo.GetComponent<Image>();
            hImg.sprite = headerSprite;
            hImg.color = Color.white;
            hImg.preserveAspect = true;

            TextMeshProUGUI titleTMP = CreateTMPText(headerGo.transform, "TitleText", "CÀI ĐẶT", font,
                Vector2.zero, new Vector2(480f, 80f), Color.white, 46, FontStyles.Bold);

            GameObject closeGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(cardGo.transform, false);
            RectTransform clRect = closeGo.GetComponent<RectTransform>();
            clRect.anchorMin = new Vector2(1f, 1f);
            clRect.anchorMax = new Vector2(1f, 1f);
            clRect.pivot = new Vector2(0.5f, 0.5f);
            clRect.anchoredPosition = new Vector2(15f, 10f);
            clRect.sizeDelta = new Vector2(115f, 115f);

            Image clImg = closeGo.GetComponent<Image>();
            clImg.sprite = closeBtnSprite;
            clImg.color = Color.white;
            clImg.preserveAspect = true;
            Button closeBtn = closeGo.GetComponent<Button>();

            float rowMusicY = 175f;
            float rowSoundY = 55f;
            float rowHapticY = -65f;
            float rowLangY = -185f;
            float rowReplayY = -325f;

            CreateRowIcon(cardGo.transform, "MusicIcon", musicIconSprite, new Vector2(-195f, rowMusicY), new Vector2(85f, 85f));
            var (musicToggle, musicView) = CreatePillToggle(cardGo.transform, "MusicToggle", trackSprite, thumbGreenSprite, thumbGreySprite, font, new Vector2(120f, rowMusicY));

            CreateRowIcon(cardGo.transform, "SoundIcon", soundIconSprite, new Vector2(-195f, rowSoundY), new Vector2(85f, 85f));
            var (soundToggle, soundView) = CreatePillToggle(cardGo.transform, "SoundToggle", trackSprite, thumbGreenSprite, thumbGreySprite, font, new Vector2(120f, rowSoundY));

            CreateRowIcon(cardGo.transform, "HapticIcon", vibIconSprite, new Vector2(-195f, rowHapticY), new Vector2(85f, 85f));
            var (hapticToggle, hapticView) = CreatePillToggle(cardGo.transform, "HapticToggle", trackSprite, thumbGreenSprite, thumbGreySprite, font, new Vector2(120f, rowHapticY));

            CreateRowIcon(cardGo.transform, "LanguageIcon", langIconSprite, new Vector2(-195f, rowLangY), new Vector2(85f, 85f));
            GameObject langBtnGo = new GameObject("LanguageButton", typeof(RectTransform), typeof(Image), typeof(Button));
            langBtnGo.transform.SetParent(cardGo.transform, false);
            RectTransform lRect = langBtnGo.GetComponent<RectTransform>();
            lRect.anchorMin = new Vector2(0.5f, 0.5f);
            lRect.anchorMax = new Vector2(0.5f, 0.5f);
            lRect.pivot = new Vector2(0.5f, 0.5f);
            lRect.anchoredPosition = new Vector2(120f, rowLangY);
            lRect.sizeDelta = new Vector2(250f, 82f);

            Image lImg = langBtnGo.GetComponent<Image>();
            lImg.sprite = cyanBtnSprite;
            lImg.color = Color.white;
            lImg.preserveAspect = true;
            Button langBtn = langBtnGo.GetComponent<Button>();

            TextMeshProUGUI langTMP = CreateTMPText(langBtnGo.transform, "Text", "Tiếng Việt", font,
                Vector2.zero, new Vector2(230f, 60f), Color.white, 30, FontStyles.Bold);

            GameObject replayGo = new GameObject("ReplayButton", typeof(RectTransform), typeof(Image), typeof(Button));
            replayGo.transform.SetParent(cardGo.transform, false);
            RectTransform rpRect = replayGo.GetComponent<RectTransform>();
            rpRect.anchorMin = new Vector2(0.5f, 0.5f);
            rpRect.anchorMax = new Vector2(0.5f, 0.5f);
            rpRect.pivot = new Vector2(0.5f, 0.5f);
            rpRect.anchoredPosition = new Vector2(0f, rowReplayY);
            rpRect.sizeDelta = new Vector2(360f, 110f);

            Image rpImg = replayGo.GetComponent<Image>();
            rpImg.sprite = replayBtnSprite;
            rpImg.color = Color.white;
            rpImg.preserveAspect = true;
            Button replayBtn = replayGo.GetComponent<Button>();

            TextMeshProUGUI replayTMP = CreateTMPText(replayGo.transform, "Text", "CHƠI LẠI", font,
                new Vector2(0f, 3f), new Vector2(320f, 70f), Color.white, 38, FontStyles.Bold);

            SettingsController sc = popupGo.GetComponent<SettingsController>();
            if (sc == null) sc = popupGo.AddComponent<SettingsController>();

            SerializedObject scSo = new SerializedObject(sc);
            scSo.FindProperty("_soundToggle").objectReferenceValue = soundToggle;
            scSo.FindProperty("_musicToggle").objectReferenceValue = musicToggle;
            scSo.FindProperty("_hapticToggle").objectReferenceValue = hapticToggle;
            scSo.FindProperty("_closeButton").objectReferenceValue = closeBtn;
            scSo.FindProperty("_languageButton").objectReferenceValue = langBtn;
            scSo.FindProperty("_languageText").objectReferenceValue = langTMP;
            scSo.FindProperty("_replayButton").objectReferenceValue = replayBtn;
            scSo.FindProperty("_replayText").objectReferenceValue = replayTMP;
            scSo.FindProperty("_headerTitleText").objectReferenceValue = titleTMP;
            scSo.FindProperty("_canvasGroup").objectReferenceValue = cg;
            scSo.FindProperty("_cardRect").objectReferenceValue = cRect;

            WireToggleView(scSo.FindProperty("_soundToggleView"), soundView);
            WireToggleView(scSo.FindProperty("_musicToggleView"), musicView);
            WireToggleView(scSo.FindProperty("_hapticToggleView"), hapticView);

            scSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(sc);

            UIManager uim = root.GetComponent<UIManager>();
            if (uim == null) uim = root.GetComponentInChildren<UIManager>(true);
            if (uim != null)
            {
                SerializedObject uimSo = new SerializedObject(uim);
                uimSo.FindProperty("_settingsPopup").objectReferenceValue = popupGo;
                uimSo.ApplyModifiedProperties();
                EditorUtility.SetDirty(uim);
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void WireToggleView(SerializedProperty prop, SettingsController.PillToggleView view)
    {
        if (prop == null || view == null) return;
        prop.FindPropertyRelative("toggle").objectReferenceValue = view.toggle;
        prop.FindPropertyRelative("thumbRect").objectReferenceValue = view.thumbRect;
        prop.FindPropertyRelative("thumbImage").objectReferenceValue = view.thumbImage;
        prop.FindPropertyRelative("thumbText").objectReferenceValue = view.thumbText;
        prop.FindPropertyRelative("onSprite").objectReferenceValue = view.onSprite;
        prop.FindPropertyRelative("offSprite").objectReferenceValue = view.offSprite;
        prop.FindPropertyRelative("onX").floatValue = view.onX;
        prop.FindPropertyRelative("offX").floatValue = view.offX;
    }

    private static (Toggle, SettingsController.PillToggleView) CreatePillToggle(
        Transform parent, string name, Sprite trackSprite, Sprite thumbOn, Sprite thumbOff, TMP_FontAsset font, Vector2 pos)
    {
        GameObject toggleGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Toggle));
        toggleGo.transform.SetParent(parent, false);
        RectTransform tRect = toggleGo.GetComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0.5f, 0.5f);
        tRect.anchorMax = new Vector2(0.5f, 0.5f);
        tRect.pivot = new Vector2(0.5f, 0.5f);
        tRect.anchoredPosition = pos;
        tRect.sizeDelta = new Vector2(250f, 84f);

        Image trImg = toggleGo.GetComponent<Image>();
        trImg.sprite = trackSprite;
        trImg.color = Color.white;
        trImg.preserveAspect = true;

        Toggle toggle = toggleGo.GetComponent<Toggle>();
        toggle.targetGraphic = trImg;

        GameObject thumbGo = new GameObject("Thumb", typeof(RectTransform), typeof(Image));
        thumbGo.transform.SetParent(toggleGo.transform, false);
        RectTransform thRect = thumbGo.GetComponent<RectTransform>();
        thRect.anchorMin = new Vector2(0.5f, 0.5f);
        thRect.anchorMax = new Vector2(0.5f, 0.5f);
        thRect.pivot = new Vector2(0.5f, 0.5f);
        thRect.anchoredPosition = new Vector2(-42f, 0f);
        thRect.sizeDelta = new Vector2(130f, 74f);

        Image thImg = thumbGo.GetComponent<Image>();
        thImg.sprite = thumbOn;
        thImg.color = Color.white;
        thImg.preserveAspect = true;

        TextMeshProUGUI thText = CreateTMPText(thumbGo.transform, "ThumbText", "BẬT", font,
            Vector2.zero, new Vector2(110f, 50f), Color.white, 30, FontStyles.Bold);

        toggle.graphic = null;

        var view = new SettingsController.PillToggleView
        {
            toggle = toggle,
            thumbRect = thRect,
            thumbImage = thImg,
            thumbText = thText,
            onSprite = thumbOn,
            offSprite = thumbOff,
            onX = -42f,
            offX = 42f
        };

        return (toggle, view);
    }

    private static void CreateRowIcon(Transform parent, string name, Sprite sprite, Vector2 pos, Vector2 size)
    {
        GameObject iconGo = new GameObject(name, typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(parent, false);
        RectTransform rt = iconGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = iconGo.GetComponent<Image>();
        img.sprite = sprite;
        img.color = Color.white;
        img.preserveAspect = true;
    }

    private static TextMeshProUGUI CreateTMPText(
        Transform parent, string name, string text, TMP_FontAsset font, Vector2 pos, Vector2 size, Color color, float fontSize, FontStyles style)
    {
        GameObject textGo = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(parent, false);
        RectTransform rt = textGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }
}
#endif
