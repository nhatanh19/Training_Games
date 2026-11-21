using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour
{
    [Serializable]
    public class IconToggleView
    {
        public Button button;
        public Image frameImage;
        public Image iconImage;
        public GameObject offIndicator;
        public Color onFrameColor = Color.white;
        public Color offFrameColor = new Color(0.60f, 0.63f, 0.72f, 1f);
        public Color onIconColor = Color.white;
        public Color offIconColor = new Color(0.78f, 0.78f, 0.82f, 0.65f);
    }

    [Header("Data")]
    [SerializeField] private LocalizationTableSO localization;

    [Header("Icon Tile Toggles (Sound / Music / Haptic)")]
    [SerializeField] private IconToggleView soundToggle;
    [SerializeField] private IconToggleView musicToggle;
    [SerializeField] private IconToggleView hapticToggle;

    [Header("Navigation & Language")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button languageButton;
    [SerializeField] private TextMeshProUGUI languageValueText;

    [Header("Context Action Groups")]
    [SerializeField] private GameObject gameplayActionsGroup;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button quitLevelButton;
    [SerializeField] private GameObject homeActionsGroup;
    [SerializeField] private Button restoreButton;

    public const string LANGUAGE_KEY = "GameLanguage";
    private string _currentLanguage = "vi";
    private bool _isSubscribed;
    private bool _closing;
    private bool _bound;
    private bool _inGameplayContext = true;

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
        BindUI();
        SyncSettingsUI();
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    public void ConfigureContext(bool inGameplay)
    {
        _inGameplayContext = inGameplay;
        if (gameplayActionsGroup != null) gameplayActionsGroup.SetActive(inGameplay);
        if (homeActionsGroup != null) homeActionsGroup.SetActive(!inGameplay);
    }

    private void LoadLanguage()
    {
        _currentLanguage = PlayerPrefs.GetString(LANGUAGE_KEY, "vi");
        RefreshLanguageText();
    }

    private void BindUI()
    {
        if (_bound) return;
        _bound = true;

        if (soundToggle?.button != null) soundToggle.button.onClick.AddListener(OnSoundToggleClicked);
        if (musicToggle?.button != null) musicToggle.button.onClick.AddListener(OnMusicToggleClicked);
        if (hapticToggle?.button != null) hapticToggle.button.onClick.AddListener(OnHapticToggleClicked);
        if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
        if (languageButton != null) languageButton.onClick.AddListener(OnLanguageButtonClicked);
        if (replayButton != null) replayButton.onClick.AddListener(OnReplayClicked);
        if (quitLevelButton != null) quitLevelButton.onClick.AddListener(OnQuitLevelClicked);
        if (restoreButton != null) restoreButton.onClick.AddListener(OnRestoreClicked);
    }

    private void SubscribeEvents()
    {
        if (_isSubscribed || SettingsManager.Instance == null) return;
        SettingsManager.Instance.OnSoundChanged += HandleSoundChanged;
        SettingsManager.Instance.OnMusicChanged += HandleMusicChanged;
        SettingsManager.Instance.OnHapticChanged += HandleHapticChanged;
        _isSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!_isSubscribed || SettingsManager.Instance == null) return;
        SettingsManager.Instance.OnSoundChanged -= HandleSoundChanged;
        SettingsManager.Instance.OnMusicChanged -= HandleMusicChanged;
        SettingsManager.Instance.OnHapticChanged -= HandleHapticChanged;
        _isSubscribed = false;
    }

    public void SyncSettingsUI()
    {
        if (SettingsManager.Instance == null) return;

        ApplyIconToggleVisual(soundToggle, SettingsManager.Instance.IsSoundOn);
        ApplyIconToggleVisual(musicToggle, SettingsManager.Instance.IsMusicOn);
        ApplyIconToggleVisual(hapticToggle, SettingsManager.Instance.IsHapticOn);
        RefreshLanguageText();
    }

    private void HandleSoundChanged(bool isEnabled) => ApplyIconToggleVisual(soundToggle, isEnabled);
    private void HandleMusicChanged(bool isEnabled) => ApplyIconToggleVisual(musicToggle, isEnabled);
    private void HandleHapticChanged(bool isEnabled) => ApplyIconToggleVisual(hapticToggle, isEnabled);

    private void OnSoundToggleClicked() => SettingsManager.Instance?.ToggleSound();
    private void OnMusicToggleClicked() => SettingsManager.Instance?.ToggleMusic();
    private void OnHapticToggleClicked()
    {
        SettingsManager.Instance?.ToggleHaptic();
        if (SettingsManager.Instance != null && SettingsManager.Instance.IsHapticOn)
        {
            HapticManager.Instance?.TriggerLight();
        }
    }

    private static void ApplyIconToggleVisual(IconToggleView view, bool isOn)
    {
        if (view == null) return;
        if (view.frameImage != null) view.frameImage.color = isOn ? view.onFrameColor : view.offFrameColor;
        if (view.iconImage != null) view.iconImage.color = isOn ? view.onIconColor : view.offIconColor;
        if (view.offIndicator != null) view.offIndicator.SetActive(!isOn);
    }

    private void OnLanguageButtonClicked()
    {
        _currentLanguage = (_currentLanguage == "vi") ? "en" : "vi";
        PlayerPrefs.SetString(LANGUAGE_KEY, _currentLanguage);
        PlayerPrefs.Save();

        RefreshLanguageText();
        OnLanguageChanged?.Invoke(_currentLanguage);
        LanguageChanged?.Invoke(_currentLanguage);
    }

    private void RefreshLanguageText()
    {
        if (languageValueText != null && localization != null)
        {
            languageValueText.text = localization.Get("settings_lang_value", IsVietnamese);
        }
    }

    private void OnReplayClicked()
    {
        if (_closing || !_inGameplayContext) return;
        _closing = true;
        if (UIManager.Instance != null)
            UIManager.Instance.CloseSettings(() => GameManager.Instance?.RestartLevel());
        else
            GameManager.Instance?.RestartLevel();
    }

    private void OnQuitLevelClicked()
    {
        if (_closing || !_inGameplayContext) return;
        _closing = true;
        if (UIManager.Instance != null)
            UIManager.Instance.CloseSettings(() => AppFlowController.Instance?.EnterHome(HubTab.Home));
        else
            AppFlowController.Instance?.EnterHome(HubTab.Home);
    }

    private void OnRestoreClicked()
    {
        AudioManager.Instance?.PlayUnlock();
        HapticManager.Instance?.TriggerLight();
    }

    private void OnCloseClicked()
    {
        if (_closing) return;
        _closing = true;
        UIManager.Instance?.CloseSettings();
    }
}
