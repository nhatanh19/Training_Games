using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour
{
    [System.Serializable]
    public class PillToggleView
    {
        public Toggle toggle;
        public RectTransform thumbRect;
        public Image thumbImage;
        public TextMeshProUGUI thumbText;
        public Sprite onSprite;
        public Sprite offSprite;
        public float onX = -42f;
        public float offX = 42f;
    }

    [Header("Toggles & Views")]
    [SerializeField] private Toggle _soundToggle;
    [SerializeField] private PillToggleView _soundToggleView;
    [SerializeField] private Toggle _musicToggle;
    [SerializeField] private PillToggleView _musicToggleView;
    [SerializeField] private Toggle _hapticToggle;
    [SerializeField] private PillToggleView _hapticToggleView;

    [Header("Buttons & Text Elements")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _languageButton;
    [SerializeField] private TextMeshProUGUI _languageText;
    [SerializeField] private Button _replayButton;
    [SerializeField] private TextMeshProUGUI _replayText;
    [SerializeField] private TextMeshProUGUI _headerTitleText;

    public const string LANGUAGE_KEY = "GameLanguage";
    private string _currentLanguage = "vi";
    private bool _isSubscribed;
    private bool _closing;

    public bool IsVietnamese => _currentLanguage == "vi";
    public string CurrentLanguage => _currentLanguage;
    public event Action<string> OnLanguageChanged;
    public static event Action<string> LanguageChanged;

    public static bool IsVietnameseSaved => PlayerPrefs.GetString(LANGUAGE_KEY, "vi") == "vi";

    private void Awake()
    {
        LoadLanguage();
        BindUI();
    }

    private void OnEnable()
    {
        _closing = false;
        LoadLanguage();
        SyncSettingsUI();
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void LoadLanguage()
    {
        _currentLanguage = PlayerPrefs.GetString(LANGUAGE_KEY, "vi");
        UpdateAllLocalizedTexts();
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

        if (_languageButton != null)
        {
            _languageButton.onClick.RemoveAllListeners();
            _languageButton.onClick.AddListener(OnLanguageButtonClicked);
        }

        if (_replayButton != null)
        {
            _replayButton.onClick.RemoveAllListeners();
            _replayButton.onClick.AddListener(OnReplayClicked);
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

        bool soundOn = SettingsManager.Instance.IsSoundOn;
        if (_soundToggle != null)
            _soundToggle.SetIsOnWithoutNotify(soundOn);
        UpdateToggleVisual(_soundToggleView, soundOn, false);

        bool musicOn = SettingsManager.Instance.IsMusicOn;
        if (_musicToggle != null)
            _musicToggle.SetIsOnWithoutNotify(musicOn);
        UpdateToggleVisual(_musicToggleView, musicOn, false);

        bool hapticOn = SettingsManager.Instance.IsHapticOn;
        if (_hapticToggle != null)
            _hapticToggle.SetIsOnWithoutNotify(hapticOn);
        UpdateToggleVisual(_hapticToggleView, hapticOn, false);

        UpdateAllLocalizedTexts();
    }

    private void HandleSoundChanged(bool isEnabled)
    {
        if (_soundToggle != null && _soundToggle.isOn != isEnabled)
            _soundToggle.SetIsOnWithoutNotify(isEnabled);
        UpdateToggleVisual(_soundToggleView, isEnabled, true);
    }

    private void HandleMusicChanged(bool isEnabled)
    {
        if (_musicToggle != null && _musicToggle.isOn != isEnabled)
            _musicToggle.SetIsOnWithoutNotify(isEnabled);
        UpdateToggleVisual(_musicToggleView, isEnabled, true);
    }

    private void HandleHapticChanged(bool isEnabled)
    {
        if (_hapticToggle != null && _hapticToggle.isOn != isEnabled)
            _hapticToggle.SetIsOnWithoutNotify(isEnabled);
        UpdateToggleVisual(_hapticToggleView, isEnabled, true);
    }

    private void OnSoundToggleChanged(bool isOn)
    {
        AudioManager.Instance?.PlayClick();
        HapticManager.Instance?.TriggerLight();
        SettingsManager.Instance?.SetSound(isOn);
        UpdateToggleVisual(_soundToggleView, isOn, true);
    }

    private void OnMusicToggleChanged(bool isOn)
    {
        AudioManager.Instance?.PlayClick();
        HapticManager.Instance?.TriggerLight();
        SettingsManager.Instance?.SetMusic(isOn);
        UpdateToggleVisual(_musicToggleView, isOn, true);
    }

    private void OnHapticToggleChanged(bool isOn)
    {
        AudioManager.Instance?.PlayClick();
        SettingsManager.Instance?.SetHaptic(isOn);
        if (isOn)
        {
            HapticManager.Instance?.TriggerLight();
        }
        UpdateToggleVisual(_hapticToggleView, isOn, true);
    }

    private void UpdateToggleVisual(PillToggleView view, bool isOn, bool animate)
    {
        if (view == null) return;

        if (view.thumbRect != null)
        {
            view.thumbRect.DOKill();
            float targetX = isOn ? view.onX : view.offX;
            if (animate)
            {
                view.thumbRect.DOAnchorPosX(targetX, 0.2f).SetEase(Ease.OutQuad).SetLink(gameObject);
            }
            else
            {
                Vector2 pos = view.thumbRect.anchoredPosition;
                pos.x = targetX;
                view.thumbRect.anchoredPosition = pos;
            }
        }

        if (view.thumbImage != null)
        {
            if (isOn && view.onSprite != null) view.thumbImage.sprite = view.onSprite;
            else if (!isOn && view.offSprite != null) view.thumbImage.sprite = view.offSprite;
        }

        if (view.thumbText != null)
        {
            view.thumbText.text = isOn ? (IsVietnamese ? "BẬT" : "ON") : (IsVietnamese ? "TẮT" : "OFF");
        }
    }

    private void OnLanguageButtonClicked()
    {
        AudioManager.Instance?.PlayClick();
        HapticManager.Instance?.TriggerLight();

        _currentLanguage = (_currentLanguage == "vi") ? "en" : "vi";
        PlayerPrefs.SetString(LANGUAGE_KEY, _currentLanguage);
        PlayerPrefs.Save();

        UpdateAllLocalizedTexts();
        OnLanguageChanged?.Invoke(_currentLanguage);
        LanguageChanged?.Invoke(_currentLanguage);
    }

    public void UpdateAllLocalizedTexts()
    {
        if (_headerTitleText != null)
        {
            _headerTitleText.text = IsVietnamese ? "CÀI ĐẶT" : "SETTINGS";
        }

        if (_languageText != null)
        {
            _languageText.text = IsVietnamese ? "Tiếng Việt" : "English";
        }

        if (_replayText != null)
        {
            _replayText.text = IsVietnamese ? "CHƠI LẠI" : "REPLAY";
        }

        if (_soundToggleView != null && _soundToggleView.thumbText != null)
        {
            bool isOn = _soundToggle != null ? _soundToggle.isOn : true;
            _soundToggleView.thumbText.text = isOn ? (IsVietnamese ? "BẬT" : "ON") : (IsVietnamese ? "TẮT" : "OFF");
        }
        if (_musicToggleView != null && _musicToggleView.thumbText != null)
        {
            bool isOn = _musicToggle != null ? _musicToggle.isOn : true;
            _musicToggleView.thumbText.text = isOn ? (IsVietnamese ? "BẬT" : "ON") : (IsVietnamese ? "TẮT" : "OFF");
        }
        if (_hapticToggleView != null && _hapticToggleView.thumbText != null)
        {
            bool isOn = _hapticToggle != null ? _hapticToggle.isOn : true;
            _hapticToggleView.thumbText.text = isOn ? (IsVietnamese ? "BẬT" : "ON") : (IsVietnamese ? "TẮT" : "OFF");
        }
    }

    private void OnReplayClicked()
    {
        if (_closing) return;
        _closing = true;
        AudioManager.Instance?.PlayClick();
        HapticManager.Instance?.TriggerLight();
        if (UIManager.Instance != null)
            UIManager.Instance.CloseSettings(() => GameManager.Instance?.RestartLevel());
        else
            GameManager.Instance?.RestartLevel();
    }

    private void OnCloseClicked()
    {
        if (_closing) return;
        _closing = true;
        AudioManager.Instance?.PlayClick();
        HapticManager.Instance?.TriggerLight();
        UIManager.Instance?.CloseSettings();
    }
}
