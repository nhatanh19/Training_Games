using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD & Controllers (Wired in Inspector)")]
    [SerializeField] private HUDController _hudController;
    [SerializeField] private SettingsController _settingsController;
    [SerializeField] private ContinuePopupController _continueController;
    [SerializeField] private WinPopupController _winController;
    [SerializeField] private LosePopupController _loseController;

    [Header("Popups & Cards (Wired in Inspector)")]
    [SerializeField] private GameObject _winPopup;
    [SerializeField] private Transform _winCard;
    [SerializeField] private GameObject _losePopup;
    [SerializeField] private Transform _loseCard;
    [SerializeField] private GameObject _settingsPopup;
    [SerializeField] private Transform _settingsCard;
    [SerializeField] private GameObject _continuePopup;
    [SerializeField] private Transform _continueCard;

    [Header("Popup Motion")]
    [SerializeField, Min(0.01f)] private float _popupOpenDuration = 0.28f;
    [SerializeField, Min(0.01f)] private float _popupCloseDuration = 0.18f;
    [SerializeField, Range(0.1f, 1f)] private float _popupStartScale = 0.85f;

    private sealed class PopupMotion
    {
        public Transform Card;
        public CanvasGroup Group;
        public Vector3 OriginalScale;
        public float OriginalAlpha;
        public bool OriginalInteractable;
        public bool OriginalBlocksRaycasts;
        public Sequence Sequence;
        public bool IsClosing;
    }

    private readonly Dictionary<GameObject, PopupMotion> _popupMotions = new Dictionary<GameObject, PopupMotion>();
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

    private void HandleGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Loading:
            case GameState.Menu:
            case GameState.Playing:
                HideAllPopups();
                break;

            case GameState.GameWin:
                ShowWinPopup();
                break;

            case GameState.GameOver:
                if (BusStopManager.Instance != null && BusStopManager.Instance.HasLockedSlot)
                {
                    OpenContinuePopup(null);
                }
                else
                {
                    ShowLosePopup();
                }
                break;
        }
    }

    public void UpdateLevelDisplay(int levelNumber)
    {
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
        HidePopupImmediate(_losePopup);
        HidePopupImmediate(_settingsPopup);
        HidePopupImmediate(_continuePopup);
        ShowPopupWithAnimation(_winPopup, _winCard);
    }

    public void ShowLosePopup()
    {
        HidePopupImmediate(_winPopup);
        HidePopupImmediate(_settingsPopup);
        HidePopupImmediate(_continuePopup);
        ShowPopupWithAnimation(_losePopup, _loseCard);
    }

    public void OpenSettings(bool inGameplay = true)
    {
        HidePopupImmediate(_winPopup);
        HidePopupImmediate(_losePopup);
        HidePopupImmediate(_continuePopup);
        if (_settingsController != null)
        {
            _settingsController.ConfigureContext(inGameplay);
        }
        ShowPopupWithAnimation(_settingsPopup, _settingsCard);
    }

    public void OpenContinuePopup(BusStopSlot targetSlot = null)
    {
        if (_continuePopup == null) return;
        if (_continueController != null) _continueController.Configure(targetSlot);
        HidePopupImmediate(_winPopup);
        HidePopupImmediate(_losePopup);
        HidePopupImmediate(_settingsPopup);
        ShowPopupWithAnimation(_continuePopup, _continueCard);
    }

    public void CloseContinuePopup(bool unlockedSlot = false)
    {
        HidePopupWithAnimation(_continuePopup, _continueCard, () =>
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.GameOver)
            {
                if (unlockedSlot) GameManager.Instance.ResumeAfterRevive();
                else ShowLosePopup();
            }
        });
    }

    public void CloseSettings(Action onComplete = null)
    {
        HidePopupWithAnimation(_settingsPopup, _settingsCard, onComplete);
    }

    public void HideWinAndAdvance()
    {
        HidePopupWithAnimation(_winPopup, _winCard, () =>
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.GameWin)
            {
                GameManager.Instance.NextLevel();
            }
        });
    }

    public void RestartFromLosePopup()
    {
        HidePopupWithAnimation(_losePopup, _loseCard, () =>
        {
            GameManager.Instance?.RestartLevel();
        });
    }

    public void ReturnHomeFromLosePopup()
    {
        HidePopupWithAnimation(_losePopup, _loseCard, () =>
        {
            AppFlowController.Instance?.EnterHome(HubTab.Home);
        });
    }

    public void HideAllPopups()
    {
        HidePopupImmediate(_winPopup, _winCard);
        HidePopupImmediate(_losePopup, _loseCard);
        HidePopupImmediate(_settingsPopup, _settingsCard);
        HidePopupImmediate(_continuePopup, _continueCard);

        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
        {
            GameInputManager.SetLock(false);
        }
    }

    private PopupMotion GetPopupMotion(GameObject popup, Transform explicitCard = null)
    {
        if (_popupMotions.TryGetValue(popup, out PopupMotion motion)) return motion;

        CanvasGroup group = popup.GetComponent<CanvasGroup>();
        motion = new PopupMotion
        {
            Card = explicitCard,
            Group = group,
            OriginalScale = explicitCard != null && explicitCard.localScale != Vector3.zero ? explicitCard.localScale : Vector3.one,
            OriginalAlpha = group != null ? group.alpha : 1f,
            OriginalInteractable = group == null || group.interactable,
            OriginalBlocksRaycasts = group == null || group.blocksRaycasts
        };
        _popupMotions.Add(popup, motion);
        return motion;
    }

    private void ShowPopupWithAnimation(GameObject popup, Transform card)
    {
        if (popup == null) return;
        PopupMotion motion = GetPopupMotion(popup, card);
        motion.Sequence?.Kill();
        motion.IsClosing = false;
        popup.SetActive(true);
        GameInputManager.SetLock(true);

        Sequence seq = DOTween.Sequence().SetUpdate(true).SetLink(popup);
        motion.Sequence = seq;
        if (motion.Card != null)
        {
            motion.Card.localScale = motion.OriginalScale * Mathf.Clamp(_popupStartScale, 0.1f, 1f);
            seq.Join(motion.Card.DOScale(motion.OriginalScale, Mathf.Max(0.01f, _popupOpenDuration)).SetEase(Ease.OutBack));
        }
        if (motion.Group != null)
        {
            motion.Group.alpha = 0f;
            motion.Group.interactable = false;
            motion.Group.blocksRaycasts = true;
            seq.Join(motion.Group.DOFade(motion.OriginalAlpha, Mathf.Max(0.01f, _popupOpenDuration)));
        }
        seq.AppendInterval(0.001f);
        seq.OnComplete(() =>
        {
            motion.Sequence = null;
            if (motion.Group != null) motion.Group.interactable = motion.OriginalInteractable;
        });
    }

    private void HidePopupWithAnimation(GameObject popup, Transform card, Action onComplete = null)
    {
        if (popup == null || !popup.activeSelf)
        {
            onComplete?.Invoke();
            return;
        }

        PopupMotion motion = GetPopupMotion(popup, card);
        if (motion.IsClosing) return;
        motion.Sequence?.Kill();
        motion.IsClosing = true;

        Sequence seq = DOTween.Sequence().SetUpdate(true).SetLink(popup);
        motion.Sequence = seq;
        if (motion.Card != null)
        {
            seq.Join(motion.Card.DOScale(motion.OriginalScale * Mathf.Clamp(_popupStartScale, 0.1f, 1f),
                Mathf.Max(0.01f, _popupCloseDuration)).SetEase(Ease.InQuad));
        }
        if (motion.Group != null)
        {
            motion.Group.interactable = false;
            seq.Join(motion.Group.DOFade(0f, Mathf.Max(0.01f, _popupCloseDuration)));
        }
        seq.AppendInterval(0.001f);
        seq.OnComplete(() =>
        {
            motion.Sequence = null;
            motion.IsClosing = false;
            popup.SetActive(false);
            if (!IsAnyPopupOpen() && GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
            {
                GameInputManager.SetLock(false);
            }
            onComplete?.Invoke();
        });
    }

    private void HidePopupImmediate(GameObject popup, Transform card = null)
    {
        if (popup == null) return;
        PopupMotion motion = GetPopupMotion(popup, card);
        motion.Sequence?.Kill();
        motion.Sequence = null;
        motion.IsClosing = false;
        popup.SetActive(false);
        if (motion.Card != null) motion.Card.localScale = motion.OriginalScale;
        if (motion.Group != null)
        {
            motion.Group.alpha = motion.OriginalAlpha;
            motion.Group.interactable = motion.OriginalInteractable;
            motion.Group.blocksRaycasts = motion.OriginalBlocksRaycasts;
        }
    }
}
