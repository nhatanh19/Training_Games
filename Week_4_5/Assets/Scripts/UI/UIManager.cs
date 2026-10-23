using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private static UIManager _instance;

    public static UIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UIManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("UIManager");
                    _instance = go.AddComponent<UIManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Popups")]
    [SerializeField] private GameObject winPopup;
    [SerializeField] private GameObject losePopup;
    [SerializeField] private GameObject hudPanel;

    [Header("HUD Elements")]
    [SerializeField] private Text levelText;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button replayWinButton;
    [SerializeField] private Button retryLoseButton;
    [SerializeField] private Button restartHudButton;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (winPopup == null || losePopup == null || hudPanel == null)
        {
            BuildProceduralUI();
        }

        BindButtons();
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            GameManager.Instance.OnLevelStarted += UpdateLevelDisplay;
            UpdateLevelDisplay(GameManager.Instance.CurrentLevelIndex + 1);
        }

        HideAllPopups();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            GameManager.Instance.OnLevelStarted -= UpdateLevelDisplay;
        }
    }

    private Font GetDefaultFont()
    {
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (f == null)
        {
            try
            {
                f = Font.CreateDynamicFontFromOSFont(new string[] { "Arial", "Segoe UI", "Tahoma" }, 32);
            }
            catch
            {
            }
        }
        return f;
    }

    private void BuildProceduralUI()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        GameObject canvasGo;

        if (canvas == null)
        {
            canvasGo = new GameObject("UICanvas");
            canvas = canvasGo.AddComponent<Canvas>();
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
            canvasGo = canvas.gameObject;
        }

        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        DefaultControls.Resources uiRes = new DefaultControls.Resources();
        Font defaultFont = GetDefaultFont();

        if (hudPanel == null)
        {
            hudPanel = new GameObject("HUDPanel", typeof(RectTransform));
            hudPanel.transform.SetParent(canvasGo.transform, false);
            RectTransform hudRect = hudPanel.GetComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(0f, 1f);
            hudRect.anchorMax = new Vector2(1f, 1f);
            hudRect.pivot = new Vector2(0.5f, 1f);
            hudRect.sizeDelta = new Vector2(0f, 220f);

            GameObject txtGo = DefaultControls.CreateText(uiRes);
            txtGo.transform.SetParent(hudPanel.transform, false);
            levelText = txtGo.GetComponent<Text>();
            levelText.font = defaultFont;
            levelText.text = "LEVEL 1";
            levelText.fontSize = 58;
            levelText.fontStyle = FontStyle.Bold;
            levelText.alignment = TextAnchor.MiddleCenter;
            levelText.color = Color.white;
            RectTransform txtRect = txtGo.GetComponent<RectTransform>();
            txtRect.anchoredPosition = new Vector2(0f, -80f);
            txtRect.sizeDelta = new Vector2(600f, 100f);

            GameObject rstBtnGo = DefaultControls.CreateButton(uiRes);
            rstBtnGo.transform.SetParent(hudPanel.transform, false);
            rstBtnGo.GetComponent<Image>().color = new Color(0.2f, 0.25f, 0.35f, 0.95f);
            restartHudButton = rstBtnGo.GetComponent<Button>();
            Text rstText = rstBtnGo.GetComponentInChildren<Text>();
            rstText.font = defaultFont;
            rstText.text = "RESET";
            rstText.fontSize = 32;
            rstText.fontStyle = FontStyle.Bold;
            rstText.color = Color.white;
            RectTransform rstRect = rstBtnGo.GetComponent<RectTransform>();
            rstRect.anchorMin = new Vector2(1f, 1f);
            rstRect.anchorMax = new Vector2(1f, 1f);
            rstRect.pivot = new Vector2(1f, 1f);
            rstRect.anchoredPosition = new Vector2(-40f, -40f);
            rstRect.sizeDelta = new Vector2(200f, 80f);
        }

        if (winPopup == null)
        {
            winPopup = CreatePopup(canvasGo.transform, "LEVEL COMPLETED!", "All passengers boarded successfully!", new Color(0.12f, 0.65f, 0.35f, 0.98f), out nextLevelButton, out replayWinButton, "NEXT LEVEL", "REPLAY");
        }

        if (losePopup == null)
        {
            Button unused;
            losePopup = CreatePopup(canvasGo.transform, "OUT OF MOVES!", "No matching bus for waiting passengers!", new Color(0.85f, 0.25f, 0.2f, 0.98f), out retryLoseButton, out unused, "RETRY", "");
        }
    }

    private GameObject CreatePopup(Transform parent, string title, string sub, Color bgColor, out Button mainBtn, out Button subBtn, string mainBtnText, string subBtnText)
    {
        DefaultControls.Resources uiRes = new DefaultControls.Resources();
        Font defaultFont = GetDefaultFont();

        GameObject overlay = DefaultControls.CreatePanel(uiRes);
        overlay.name = "Popup_" + title;
        overlay.transform.SetParent(parent, false);
        overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.sizeDelta = Vector2.zero;

        GameObject card = DefaultControls.CreatePanel(uiRes);
        card.name = "Card";
        card.transform.SetParent(overlay.transform, false);
        card.GetComponent<Image>().color = bgColor;
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(850f, 680f);

        GameObject titleGo = DefaultControls.CreateText(uiRes);
        titleGo.transform.SetParent(card.transform, false);
        Text t = titleGo.GetComponent<Text>();
        t.font = defaultFont;
        t.text = title;
        t.fontSize = 62;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        RectTransform tRect = titleGo.GetComponent<RectTransform>();
        tRect.anchoredPosition = new Vector2(0f, 190f);
        tRect.sizeDelta = new Vector2(800f, 120f);

        GameObject subGo = DefaultControls.CreateText(uiRes);
        subGo.transform.SetParent(card.transform, false);
        Text st = subGo.GetComponent<Text>();
        st.font = defaultFont;
        st.text = sub;
        st.fontSize = 34;
        st.alignment = TextAnchor.MiddleCenter;
        st.color = new Color(1f, 1f, 1f, 0.9f);
        RectTransform stRect = subGo.GetComponent<RectTransform>();
        stRect.anchoredPosition = new Vector2(0f, 90f);
        stRect.sizeDelta = new Vector2(800f, 80f);

        GameObject btn1 = DefaultControls.CreateButton(uiRes);
        btn1.transform.SetParent(card.transform, false);
        btn1.GetComponent<Image>().color = new Color(0.95f, 0.75f, 0.1f, 1f);
        mainBtn = btn1.GetComponent<Button>();
        Text btn1Text = btn1.GetComponentInChildren<Text>();
        btn1Text.font = defaultFont;
        btn1Text.text = mainBtnText;
        btn1Text.fontSize = 40;
        btn1Text.fontStyle = FontStyle.Bold;
        btn1Text.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        RectTransform btn1Rect = btn1.GetComponent<RectTransform>();
        btn1Rect.anchoredPosition = string.IsNullOrEmpty(subBtnText) ? new Vector2(0f, -80f) : new Vector2(0f, -40f);
        btn1Rect.sizeDelta = new Vector2(500f, 110f);

        if (!string.IsNullOrEmpty(subBtnText))
        {
            GameObject btn2 = DefaultControls.CreateButton(uiRes);
            btn2.transform.SetParent(card.transform, false);
            btn2.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.85f);
            subBtn = btn2.GetComponent<Button>();
            Text btn2Text = btn2.GetComponentInChildren<Text>();
            btn2Text.font = defaultFont;
            btn2Text.text = subBtnText;
            btn2Text.fontSize = 34;
            btn2Text.fontStyle = FontStyle.Bold;
            btn2Text.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            RectTransform btn2Rect = btn2.GetComponent<RectTransform>();
            btn2Rect.anchoredPosition = new Vector2(0f, -170f);
            btn2Rect.sizeDelta = new Vector2(400f, 85f);
        }
        else
        {
            subBtn = null;
        }

        overlay.SetActive(false);
        return overlay;
    }

    private void BindButtons()
    {
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveAllListeners();
            nextLevelButton.onClick.AddListener(OnNextLevelClicked);
        }

        if (replayWinButton != null)
        {
            replayWinButton.onClick.RemoveAllListeners();
            replayWinButton.onClick.AddListener(OnRestartClicked);
        }

        if (retryLoseButton != null)
        {
            retryLoseButton.onClick.RemoveAllListeners();
            retryLoseButton.onClick.AddListener(OnRestartClicked);
        }

        if (restartHudButton != null)
        {
            restartHudButton.onClick.RemoveAllListeners();
            restartHudButton.onClick.AddListener(OnRestartClicked);
        }
    }

    private void HandleGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Loading:
            case GameState.Playing:
                HideAllPopups();
                break;

            case GameState.GameWin:
                ShowWinPopup();
                break;

            case GameState.GameOver:
                ShowLosePopup();
                break;
        }
    }

    public void UpdateLevelDisplay(int levelNumber)
    {
        if (levelText != null)
        {
            levelText.text = "LEVEL " + levelNumber;
        }
    }

    public void ShowWinPopup()
    {
        if (winPopup != null)
        {
            winPopup.SetActive(true);
            Transform card = winPopup.transform.Find("Card");
            if (card != null)
            {
                card.DOKill();
                card.localScale = Vector3.one * 0.7f;
                card.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
            }
        }
        if (losePopup != null) losePopup.SetActive(false);
    }

    public void ShowLosePopup()
    {
        if (losePopup != null)
        {
            losePopup.SetActive(true);
            Transform card = losePopup.transform.Find("Card");
            if (card != null)
            {
                card.DOKill();
                card.localScale = Vector3.one * 0.7f;
                card.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
            }
        }
        if (winPopup != null) winPopup.SetActive(false);
    }

    public void HideAllPopups()
    {
        if (winPopup != null) winPopup.SetActive(false);
        if (losePopup != null) losePopup.SetActive(false);
    }

    private void OnNextLevelClicked()
    {
        AudioManager.Instance?.PlayClick();
        HideAllPopups();
        GameManager.Instance?.NextLevel();
    }

    private void OnRestartClicked()
    {
        AudioManager.Instance?.PlayClick();
        HideAllPopups();
        GameManager.Instance?.RestartLevel();
    }
}
