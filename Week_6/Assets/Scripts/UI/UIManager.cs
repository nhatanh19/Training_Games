using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
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
    [SerializeField] private Button _retryLoseButton;

    [Header("Popup Motion")]
    [SerializeField, Min(0.01f)] private float _popupOpenDuration = 0.28f;
    [SerializeField, Min(0.01f)] private float _popupCloseDuration = 0.18f;
    [SerializeField, Range(0.1f, 1f)] private float _popupStartScale = 0.85f;
    [SerializeField, Min(0.1f)] private float _rewardTimeoutSeconds = 3f;

    [Header("Button Motion")]
    [SerializeField] private bool _enableButtonMotion = true;
    [SerializeField, Range(0.8f, 1f)] private float _buttonPressedScale = 0.94f;
    [SerializeField, Min(0.01f)] private float _buttonMotionDuration = 0.08f;

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

    private sealed class ButtonMotion
    {
        public Button Button;
        public Vector3 OriginalScale;
        public Tween Tween;
        public EventTrigger Trigger;
        public readonly List<EventTrigger.Entry> Entries = new List<EventTrigger.Entry>();
    }

    private readonly Dictionary<GameObject, PopupMotion> _popupMotions = new Dictionary<GameObject, PopupMotion>();
    private readonly Dictionary<Button, ButtonMotion> _buttonMotions = new Dictionary<Button, ButtonMotion>();
    private bool _initialized;
    private Button _reviveLoseButton;
    private bool _rewardInProgress;
    private Tween _rewardTimeout;
    private int _rewardGeneration;

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
        if (IsFullyWired())
        {
            BindButtons();
            return;
        }

        if (_hudController == null)
        {
            Debug.LogError("[UIManager] _hudController chưa được gán trong Inspector — chạy Tools > Bus Jam > Wire Scene UI References rồi Save scene.");
        }

        GameObject popupCanvas = GameObject.Find("PopupCanvas");
        if (popupCanvas != null)
        {
            Transform root = popupCanvas.transform;
            _winPopup = ResolvePopup(root, "WinPopup", _winPopup);
            _losePopup = ResolvePopup(root, "LosePopup", _losePopup);
            _settingsPopup = ResolvePopup(root, "SettingsPopup", _settingsPopup);
            _continuePopup = ResolvePopup(root, "ContinuePopup", _continuePopup);
        }

        if (_winPopup != null)
        {
            _nextLevelButton = ResolveButton(_winPopup.transform, "Card/MainButton", _nextLevelButton);
        }

        if (_losePopup != null)
        {
            _retryLoseButton = ResolveButton(_losePopup.transform, "Card/MainButton", _retryLoseButton);
        }

        BindButtons();
    }

    private bool IsFullyWired()
    {
        return _hudController != null &&
               _winPopup != null && _losePopup != null && _settingsPopup != null && _continuePopup != null &&
               _nextLevelButton != null && _retryLoseButton != null;
    }

    private static GameObject ResolvePopup(Transform root, string popupName, GameObject current)
    {
        if (current != null) return current;

        Debug.LogError($"[UIManager] Popup '{popupName}' chưa được gán trong Inspector — chạy Tools > Bus Jam > Wire Scene UI References rồi Save scene.");

        Transform found = root.Find(popupName);
        return found != null ? found.gameObject : null;
    }

    private static Button ResolveButton(Transform root, string path, Button current)
    {
        if (current != null) return current;

        Debug.LogError($"[UIManager] Nút '{path}' chưa được gán trong Inspector — chạy Tools > Bus Jam > Wire Scene UI References rồi Save scene.");

        Transform found = root.Find(path);
        return found != null ? found.GetComponent<Button>() : null;
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
        if (_enableButtonMotion)
        {
            if (_hudController != null) InstallButtonMotion(_hudController.gameObject);
            InstallButtonMotion(_winPopup);
            InstallButtonMotion(_losePopup);
            InstallButtonMotion(_settingsPopup);
            InstallButtonMotion(_continuePopup);
        }

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
        foreach (ButtonMotion motion in _buttonMotions.Values)
        {
            ResetButtonMotion(motion);
            if (motion.Trigger == null || motion.Trigger.triggers == null) continue;
            foreach (EventTrigger.Entry entry in motion.Entries) motion.Trigger.triggers.Remove(entry);
        }
        _buttonMotions.Clear();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            GameManager.Instance.OnLevelStarted -= UpdateLevelDisplay;
        }
    }

    private void InstallButtonMotion(GameObject root)
    {
        if (root == null) return;

        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            if (button == null || _buttonMotions.ContainsKey(button)) continue;
            EventTrigger trigger = button.GetComponent<EventTrigger>();
            if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();
            if (trigger.triggers == null) trigger.triggers = new List<EventTrigger.Entry>();

            ButtonMotion motion = new ButtonMotion
            {
                Button = button,
                OriginalScale = button.transform.localScale,
                Trigger = trigger
            };
            _buttonMotions.Add(button, motion);
            AddButtonMotionEvent(motion, EventTriggerType.PointerDown, true);
            AddButtonMotionEvent(motion, EventTriggerType.PointerUp, false);
            AddButtonMotionEvent(motion, EventTriggerType.PointerExit, false);
        }
    }

    private void AddButtonMotionEvent(ButtonMotion motion, EventTriggerType eventType, bool pressed)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
        entry.callback.AddListener(data =>
        {
            if (data is PointerEventData pointer && pointer.button != PointerEventData.InputButton.Left) return;
            AnimateButton(motion, pressed);
        });
        motion.Trigger.triggers.Add(entry);
        motion.Entries.Add(entry);
    }

    private void AnimateButton(ButtonMotion motion, bool pressed)
    {
        if (motion.Button == null) return;
        if (pressed && (!motion.Button.isActiveAndEnabled || !motion.Button.IsInteractable())) return;

        motion.Tween?.Kill();
        Vector3 target = pressed
            ? motion.OriginalScale * Mathf.Clamp(_buttonPressedScale, 0.8f, 1f)
            : motion.OriginalScale;
        motion.Tween = motion.Button.transform.DOScale(target, Mathf.Max(0.01f, _buttonMotionDuration))
            .SetEase(Ease.OutQuad).SetUpdate(true).SetLink(motion.Button.gameObject)
            .OnComplete(() => motion.Tween = null);
    }

    private static void ResetButtonMotion(ButtonMotion motion)
    {
        motion.Tween?.Kill();
        motion.Tween = null;
        if (motion.Button != null) motion.Button.transform.localScale = motion.OriginalScale;
    }

    private void ResetButtonsIn(GameObject root)
    {
        if (root == null) return;
        foreach (ButtonMotion motion in _buttonMotions.Values)
        {
            if (motion.Button != null && motion.Button.transform.IsChildOf(root.transform))
                ResetButtonMotion(motion);
        }
    }

    private void BindButtons()
    {
        if (_nextLevelButton != null)
        {
            _nextLevelButton.onClick.RemoveAllListeners();
            _nextLevelButton.onClick.AddListener(OnNextLevelClicked);
        }

        if (_retryLoseButton != null)
        {
            _retryLoseButton.onClick.RemoveAllListeners();
            _retryLoseButton.onClick.AddListener(OnRestartClicked);
        }

        if (_losePopup != null)
        {
            Transform revive = _losePopup.transform.Find("Card/ReviveButton");
            _reviveLoseButton = revive != null ? revive.GetComponent<Button>() : null;
            if (_reviveLoseButton != null)
            {
                _reviveLoseButton.onClick.RemoveAllListeners();
                _reviveLoseButton.onClick.AddListener(OnReviveClicked);
            }
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
        if (_reviveLoseButton != null)
        {
            _reviveLoseButton.interactable = BusStopManager.Instance != null && BusStopManager.Instance.HasLockedSlot;
        }
        ShowPopupWithAnimation(_losePopup);
    }

    public void OpenSettings()
    {
        if (_settingsPopup == null) ResolveReferences();
        HidePopup(_winPopup);
        HidePopup(_losePopup);
        ShowPopupWithAnimation(_settingsPopup);
    }

    public void OpenContinuePopup(BusStopSlot targetSlot = null)
    {
        if (_continuePopup == null) ResolveReferences();
        if (_continuePopup == null) return;
        ContinuePopupController controller = _continuePopup.GetComponent<ContinuePopupController>();
        if (controller != null) controller.Configure(targetSlot);
        HidePopup(_winPopup);
        HidePopupImmediate(_losePopup);
        HidePopup(_settingsPopup);
        ShowPopupWithAnimation(_continuePopup);
    }

    public void CloseContinuePopup(bool unlockedSlot = false)
    {
        HidePopupWithAnimation(_continuePopup, () =>
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.GameOver)
            {
                if (unlockedSlot) GameManager.Instance.ResumeAfterRevive();
                else ShowLosePopup();
            }
        });
    }

    private void OnReviveClicked()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.GameOver ||
            BusStopManager.Instance == null || !BusStopManager.Instance.HasLockedSlot) return;

        AudioManager.Instance?.PlayClick();
        HapticManager.Instance?.TriggerLight();
        OpenContinuePopup();
    }

    public void CloseSettings(Action onComplete = null)
    {
        HidePopupWithAnimation(_settingsPopup, onComplete);
    }

    public void HideAllPopups()
    {
        _rewardTimeout?.Kill();
        _rewardTimeout = null;
        _rewardInProgress = false;
        _rewardGeneration++;
        if (_nextLevelButton != null) _nextLevelButton.interactable = true;
        HidePopupImmediate(_winPopup);
        HidePopupImmediate(_losePopup);
        HidePopupImmediate(_settingsPopup);
        HidePopupImmediate(_continuePopup);
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
        {
            GameInputManager.SetLock(false);
        }
    }

    private PopupMotion GetPopupMotion(GameObject popup)
    {
        if (_popupMotions.TryGetValue(popup, out PopupMotion motion)) return motion;

        Transform card = popup.transform.Find("Card");
        CanvasGroup group = popup.GetComponent<CanvasGroup>();
        motion = new PopupMotion
        {
            Card = card,
            Group = group,
            OriginalScale = card != null ? card.localScale : Vector3.one,
            OriginalAlpha = group != null ? group.alpha : 1f,
            OriginalInteractable = group == null || group.interactable,
            OriginalBlocksRaycasts = group == null || group.blocksRaycasts
        };
        _popupMotions.Add(popup, motion);
        return motion;
    }

    private void ShowPopupWithAnimation(GameObject popup)
    {
        if (popup == null) return;
        PopupMotion motion = GetPopupMotion(popup);
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

    private void HidePopupWithAnimation(GameObject popup, System.Action onComplete = null)
    {
        if (popup == null || !popup.activeSelf)
        {
            onComplete?.Invoke();
            return;
        }

        PopupMotion motion = GetPopupMotion(popup);
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
            ResetButtonsIn(popup);
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
            PopupMotion motion = GetPopupMotion(popup);
            motion.Sequence?.Kill();
            motion.Sequence = null;
            motion.IsClosing = false;
            popup.SetActive(false);
            ResetButtonsIn(popup);
            if (motion.Card != null) motion.Card.localScale = motion.OriginalScale;
            if (motion.Group != null)
            {
                motion.Group.alpha = motion.OriginalAlpha;
                motion.Group.interactable = motion.OriginalInteractable;
                motion.Group.blocksRaycasts = motion.OriginalBlocksRaycasts;
            }
        }
    }

    private void HidePopup(GameObject popup)
    {
        HidePopupWithAnimation(popup);
    }

    private void OnNextLevelClicked()
    {
        if (_rewardInProgress || GameManager.Instance == null ||
            GameManager.Instance.CurrentState != GameState.GameWin) return;
        _rewardInProgress = true;
        int generation = ++_rewardGeneration;
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
        GameManager.Instance?.AddCoins(rewardAmount);

        _rewardTimeout = DOVirtual.DelayedCall(Mathf.Max(0.1f, _rewardTimeoutSeconds), () => FinishReward(generation), true)
            .SetLink(gameObject);
        if (CoinFlyEffectManager.Instance != null && startPos != Vector3.zero && targetPos != Vector3.zero)
        {
            CoinFlyEffectManager.Instance.PlayFlyCoins(startPos, targetPos, coinCount, () => FinishReward(generation));
        }
        else
        {
            FinishReward(generation);
        }
    }

    private void FinishReward(int generation)
    {
        if (!_rewardInProgress || generation != _rewardGeneration) return;
        _rewardInProgress = false;
        _rewardTimeout?.Kill();
        _rewardTimeout = null;
        HidePopupWithAnimation(_winPopup, () =>
        {
            if (_nextLevelButton != null) _nextLevelButton.interactable = true;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.GameWin)
                GameManager.Instance.NextLevel();
        });
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
