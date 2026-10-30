using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Controllers")]
    [SerializeField] private HUDController _hudController;

    [Header("Popups")]
    [SerializeField] private GameObject _winPopup;
    [SerializeField] private GameObject _losePopup;
    [SerializeField] private GameObject _settingsPopup;

    [Header("Win / Lose Buttons")]
    [SerializeField] private Button _nextLevelButton;
    [SerializeField] private Button _replayWinButton;
    [SerializeField] private Button _retryLoseButton;

    private bool _initialized;

    public HUDController HUD => _hudController;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }

        ResolveReferences();
    }

    public void ResolveReferences()
    {
        if (_hudController == null)
        {
            _hudController = FindObjectOfType<HUDController>();
        }

        GameObject popupCanvas = GameObject.Find("PopupCanvas");
        if (popupCanvas != null)
        {
            if (_winPopup == null)
            {
                Transform t = popupCanvas.transform.Find("WinPopup");
                if (t != null) _winPopup = t.gameObject;
            }
            if (_losePopup == null)
            {
                Transform t = popupCanvas.transform.Find("LosePopup");
                if (t != null) _losePopup = t.gameObject;
            }
            if (_settingsPopup == null)
            {
                Transform t = popupCanvas.transform.Find("SettingsPopup");
                if (t != null) _settingsPopup = t.gameObject;
            }
        }

        if (_winPopup != null)
        {
            if (_nextLevelButton == null)
            {
                Transform b = _winPopup.transform.Find("Card/MainButton");
                if (b != null) _nextLevelButton = b.GetComponent<Button>();
            }
            if (_replayWinButton == null)
            {
                Transform b = _winPopup.transform.Find("Card/SubButton");
                if (b != null) _replayWinButton = b.GetComponent<Button>();
            }
        }

        if (_losePopup != null && _retryLoseButton == null)
        {
            Transform b = _losePopup.transform.Find("Card/MainButton");
            if (b != null) _retryLoseButton = b.GetComponent<Button>();
        }

        BindButtons();
    }

    private void Start()
    {
        if (GameBootstrap.IsDriving) return;
        Initialize();
    }

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        ResolveReferences();

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

    private void BindButtons()
    {
        if (_nextLevelButton != null)
        {
            _nextLevelButton.onClick.RemoveAllListeners();
            _nextLevelButton.onClick.AddListener(OnNextLevelClicked);
        }

        if (_replayWinButton != null)
        {
            _replayWinButton.onClick.RemoveAllListeners();
            _replayWinButton.onClick.AddListener(OnRestartClicked);
        }

        if (_retryLoseButton != null)
        {
            _retryLoseButton.onClick.RemoveAllListeners();
            _retryLoseButton.onClick.AddListener(OnRestartClicked);
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
        if (_hudController == null)
        {
            _hudController = FindObjectOfType<HUDController>();
        }

        if (_hudController != null)
        {
            _hudController.SetLevel(levelNumber);
        }
    }

    public void ShowWinPopup()
    {
        if (_winPopup == null) ResolveReferences();
        HidePopup(_losePopup);
        HidePopup(_settingsPopup);
        ShowPopupWithAnimation(_winPopup);
    }

    public void ShowLosePopup()
    {
        if (_losePopup == null) ResolveReferences();
        HidePopup(_winPopup);
        HidePopup(_settingsPopup);
        ShowPopupWithAnimation(_losePopup);
    }

    public void OpenSettings()
    {
        if (_settingsPopup == null) ResolveReferences();
        HidePopup(_winPopup);
        HidePopup(_losePopup);
        ShowPopupWithAnimation(_settingsPopup);
    }

    public void CloseSettings()
    {
        HidePopup(_settingsPopup);
    }

    public void HideAllPopups()
    {
        HidePopup(_winPopup);
        HidePopup(_losePopup);
        HidePopup(_settingsPopup);
    }

    private void ShowPopupWithAnimation(GameObject popup)
    {
        if (popup == null) return;
        popup.SetActive(true);

        Transform card = popup.transform.Find("Card");
        if (card != null)
        {
            card.DOKill();
            card.localScale = Vector3.one * 0.6f;
            card.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack).SetLink(popup);
        }

        CanvasGroup cg = popup.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.DOKill();
            cg.alpha = 0f;
            cg.DOFade(1f, 0.25f).SetLink(popup);
        }
    }

    private void HidePopup(GameObject popup)
    {
        if (popup != null)
        {
            popup.SetActive(false);
        }
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
