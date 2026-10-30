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
            Destroy(gameObject);
            return;
        }

        if (_hudController == null)
        {
            _hudController = FindObjectOfType<HUDController>();
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
        HidePopup(_losePopup);
        HidePopup(_settingsPopup);
        ShowPopupWithAnimation(_winPopup);
    }

    public void ShowLosePopup()
    {
        HidePopup(_winPopup);
        HidePopup(_settingsPopup);
        ShowPopupWithAnimation(_losePopup);
    }

    public void OpenSettings()
    {
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
