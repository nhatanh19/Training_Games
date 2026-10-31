using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour
{
    [Header("Toggles")]
    [SerializeField] private Toggle _soundToggle;
    [SerializeField] private Toggle _musicToggle;
    [SerializeField] private Toggle _hapticToggle;

    [Header("Buttons")]
    [SerializeField] private Button _closeButton;

    [Header("Animation Components")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private RectTransform _cardRect;

    private bool _isSubscribed;

    private void Awake()
    {
        BindUI();
    }

    private void OnEnable()
    {
        SyncSettingsUI();
        SubscribeEvents();
        AnimateOpen();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void BindUI()
    {
        if (_soundToggle != null)
        {
            _soundToggle.onValueChanged.RemoveAllListeners();
            _soundToggle.onValueChanged.AddListener(OnSoundToggleChanged);
        }

        if (_musicToggle != null)
        {
            _musicToggle.onValueChanged.RemoveAllListeners();
            _musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
        }

        if (_hapticToggle != null)
        {
            _hapticToggle.onValueChanged.RemoveAllListeners();
            _hapticToggle.onValueChanged.AddListener(OnHapticToggleChanged);
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveAllListeners();
            _closeButton.onClick.AddListener(OnCloseClicked);
        }
    }

    private void SubscribeEvents()
    {
        if (_isSubscribed) return;
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSoundChanged += HandleSoundChanged;
            SettingsManager.Instance.OnMusicChanged += HandleMusicChanged;
            SettingsManager.Instance.OnHapticChanged += HandleHapticChanged;
            _isSubscribed = true;
        }
    }

    private void UnsubscribeEvents()
    {
        if (!_isSubscribed) return;
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSoundChanged -= HandleSoundChanged;
            SettingsManager.Instance.OnMusicChanged -= HandleMusicChanged;
            SettingsManager.Instance.OnHapticChanged -= HandleHapticChanged;
            _isSubscribed = false;
        }
    }

    public void SyncSettingsUI()
    {
        if (SettingsManager.Instance == null) return;

        if (_soundToggle != null)
            _soundToggle.SetIsOnWithoutNotify(SettingsManager.Instance.IsSoundOn);

        if (_musicToggle != null)
            _musicToggle.SetIsOnWithoutNotify(SettingsManager.Instance.IsMusicOn);

        if (_hapticToggle != null)
            _hapticToggle.SetIsOnWithoutNotify(SettingsManager.Instance.IsHapticOn);
    }

    private void HandleSoundChanged(bool isEnabled)
    {
        if (_soundToggle != null && _soundToggle.isOn != isEnabled)
            _soundToggle.SetIsOnWithoutNotify(isEnabled);
    }

    private void HandleMusicChanged(bool isEnabled)
    {
        if (_musicToggle != null && _musicToggle.isOn != isEnabled)
            _musicToggle.SetIsOnWithoutNotify(isEnabled);
    }

    private void HandleHapticChanged(bool isEnabled)
    {
        if (_hapticToggle != null && _hapticToggle.isOn != isEnabled)
            _hapticToggle.SetIsOnWithoutNotify(isEnabled);
    }

    private void OnSoundToggleChanged(bool isOn)
    {
        AudioManager.Instance?.PlayClick();
        HapticManager.Instance?.TriggerLight();
        SettingsManager.Instance?.SetSound(isOn);
    }

    private void OnMusicToggleChanged(bool isOn)
    {
        AudioManager.Instance?.PlayClick();
        HapticManager.Instance?.TriggerLight();
        SettingsManager.Instance?.SetMusic(isOn);
    }

    private void OnHapticToggleChanged(bool isOn)
    {
        AudioManager.Instance?.PlayClick();
        SettingsManager.Instance?.SetHaptic(isOn);
        if (isOn)
        {
            HapticManager.Instance?.TriggerLight();
        }
    }

    private void OnCloseClicked()
    {
        AudioManager.Instance?.PlayClick();
        HapticManager.Instance?.TriggerLight();
        AnimateClose(() =>
        {
            UIManager.Instance?.CloseSettings();
        });
    }

    private void AnimateOpen()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.DOKill();
            _canvasGroup.alpha = 0f;
            _canvasGroup.DOFade(1f, 0.25f).SetLink(gameObject);
        }

        if (_cardRect != null)
        {
            _cardRect.DOKill();
            _cardRect.localScale = Vector3.one * 0.6f;
            _cardRect.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack).SetLink(gameObject);
        }
    }

    public void AnimateClose(System.Action onComplete)
    {
        Sequence seq = DOTween.Sequence();
        if (_cardRect != null)
        {
            seq.Join(_cardRect.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack));
        }
        if (_canvasGroup != null)
        {
            seq.Join(_canvasGroup.DOFade(0f, 0.2f));
        }
        seq.SetLink(gameObject);
        seq.OnComplete(() => onComplete?.Invoke());
    }
}
