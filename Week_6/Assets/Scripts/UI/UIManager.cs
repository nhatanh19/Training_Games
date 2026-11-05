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
    [SerializeField] private GameObject _continuePopup;

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
            if (_continuePopup == null)
            {
                Transform t = popupCanvas.transform.Find("ContinuePopup");
                if (t != null) _continuePopup = t.gameObject;
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

        if (CoinFlyEffectManager.Instance == null)
        {
            CoinFlyEffectManager effectMgr = FindObjectOfType<CoinFlyEffectManager>();
            if (effectMgr == null)
            {
                GameObject vfxCanvas = GameObject.Find("VFXCanvas");
                if (vfxCanvas != null)
                {
                    vfxCanvas.AddComponent<CoinFlyEffectManager>();
                }
            }
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

    public bool IsAnyPopupOpen()
    {
        return (_winPopup != null && _winPopup.activeSelf) ||
               (_losePopup != null && _losePopup.activeSelf) ||
               (_settingsPopup != null && _settingsPopup.activeSelf) ||
               (_continuePopup != null && _continuePopup.activeSelf);
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

    public void OpenContinuePopup()
    {
        if (_continuePopup == null) ResolveReferences();
        if (_continuePopup == null) return;
        HidePopup(_winPopup);
        HidePopup(_losePopup);
        HidePopup(_settingsPopup);
        ShowPopupWithAnimation(_continuePopup);
    }

    public void CloseContinuePopup()
    {
        HidePopupWithAnimation(_continuePopup, () =>
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing && !IsAnyPopupOpen())
            {
                GameInputManager.SetLock(false);
            }
        });
    }

    public void CloseSettings()
    {
        HidePopupWithAnimation(_settingsPopup);
    }

    public void HideAllPopups()
    {
        HidePopupImmediate(_winPopup);
        HidePopupImmediate(_losePopup);
        HidePopupImmediate(_settingsPopup);
        HidePopupImmediate(_continuePopup);
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
        {
            GameInputManager.SetLock(false);
        }
    }

    private void ShowPopupWithAnimation(GameObject popup)
    {
        if (popup == null) return;
        popup.SetActive(true);
        GameInputManager.SetLock(true);

        Transform card = popup.transform.Find("Card");
        if (card != null)
        {
            card.DOKill();
            card.localScale = Vector3.one * 0.5f;
            card.DOScale(Vector3.one, 0.35f).From(0.5f).SetEase(Ease.OutBack).SetLink(popup);
        }

        CanvasGroup cg = popup.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.DOKill();
            cg.alpha = 0f;
            cg.DOFade(1f, 0.25f).SetLink(popup);
        }
    }

    private void HidePopupWithAnimation(GameObject popup, System.Action onComplete = null)
    {
        if (popup == null || !popup.activeSelf)
        {
            onComplete?.Invoke();
            return;
        }

        Transform card = popup.transform.Find("Card");
        CanvasGroup cg = popup.GetComponent<CanvasGroup>();

        Sequence seq = DOTween.Sequence();
        seq.SetLink(popup);

        if (card != null)
        {
            card.DOKill();
            seq.Join(card.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack));
        }

        if (cg != null)
        {
            cg.DOKill();
            seq.Join(cg.DOFade(0f, 0.2f));
        }

        seq.OnComplete(() =>
        {
            popup.SetActive(false);
            if (!IsAnyPopupOpen() && GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
            {
                GameInputManager.SetLock(false);
            }
            onComplete?.Invoke();
        });
    }

    private void HidePopupImmediate(GameObject popup)
    {
        if (popup != null)
        {
            popup.transform.DOKill();
            Transform card = popup.transform.Find("Card");
            if (card != null) card.DOKill();
            popup.SetActive(false);
        }
    }

    private void HidePopup(GameObject popup)
    {
        HidePopupWithAnimation(popup);
    }

    private void OnNextLevelClicked()
    {
        AudioManager.Instance?.PlayClick();
        HapticManager.Instance?.TriggerLight();

        if (_nextLevelButton != null)
        {
            _nextLevelButton.interactable = false;
        }

        Vector3 startPos = Vector3.zero;
        if (_nextLevelButton != null)
        {
            startPos = _nextLevelButton.transform.position;
        }
        else if (_winPopup != null)
        {
            Transform card = _winPopup.transform.Find("Card");
            if (card != null) startPos = card.position;
        }

        Vector3 targetPos = _hudController != null ? _hudController.CoinTargetTransform.position : Vector3.zero;

        int rewardAmount = GameManager.Instance != null && GameManager.Instance.CurrentLevelData != null
            ? GameManager.Instance.CurrentLevelData.coinReward
            : 8;
        rewardAmount = Mathf.Clamp(rewardAmount, 5, 10);
        int coinCount = rewardAmount;

        if (CoinFlyEffectManager.Instance != null && startPos != Vector3.zero && targetPos != Vector3.zero)
        {
            CoinFlyEffectManager.Instance.PlayFlyCoins(startPos, targetPos, coinCount, rewardAmount, () =>
            {
                HidePopupWithAnimation(_winPopup, () =>
                {
                    if (_nextLevelButton != null) _nextLevelButton.interactable = true;
                    GameManager.Instance?.NextLevel();
                });
            });
        }
        else
        {
            HidePopupWithAnimation(_winPopup, () =>
            {
                if (_nextLevelButton != null) _nextLevelButton.interactable = true;
                GameManager.Instance?.NextLevel();
            });
        }
    }

    private void OnRestartClicked()
    {
        AudioManager.Instance?.PlayClick();
        HapticManager.Instance?.TriggerLight();

        GameObject activePopup = null;
        if (_winPopup != null && _winPopup.activeSelf) activePopup = _winPopup;
        else if (_losePopup != null && _losePopup.activeSelf) activePopup = _losePopup;

        if (activePopup != null)
        {
            HidePopupWithAnimation(activePopup, () =>
            {
                GameManager.Instance?.RestartLevel();
            });
        }
        else
        {
            HideAllPopups();
            GameManager.Instance?.RestartLevel();
        }
    }
}
