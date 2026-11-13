using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [Header("Display Elements")]
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _coinText;
    [SerializeField] private Transform _coinTargetTransform;

    [Header("Buttons")]
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _settingsButton;

    public const string COIN_PREF_KEY = "TotalCoins";

    private int _currentCoin = 0;
    private int _currentLevel = 1;
    private Vector3 _coinTargetScale;
    private Vector3 _coinTextScale;

    public int CurrentCoin => _currentCoin;
    public Vector3 CoinTargetScale => _coinTargetScale;

    public Transform CoinTargetTransform
    {
        get
        {
            if (_coinTargetTransform != null)
            {
                Transform iconTrans = _coinTargetTransform.Find("CoinIcon");
                return iconTrans != null ? iconTrans : _coinTargetTransform;
            }
            return transform;
        }
    }

    private void Awake()
    {
        BindButtons();
        _coinTargetScale = CoinTargetTransform.localScale;
        if (_coinText != null) _coinTextScale = _coinText.transform.localScale;
        LoadCoinsFromPrefs();
    }

    private void OnEnable()
    {
        LoadCoinsFromPrefs();
        SettingsController.LanguageChanged += OnLanguageChanged;
        RefreshLevelText();
    }

    private void OnDisable()
    {
        SettingsController.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(string language)
    {
        RefreshLevelText();
    }

    private void RefreshLevelText()
    {
        if (_levelText != null)
        {
            _levelText.text = SettingsController.IsVietnameseSaved
                ? $"Cấp độ {_currentLevel}"
                : $"Level {_currentLevel}";
        }
    }

    public void LoadCoinsFromPrefs()
    {
        _currentCoin = PlayerPrefs.GetInt(COIN_PREF_KEY, 0);
        if (_coinText != null)
        {
            _coinText.text = _currentCoin.ToString();
        }
    }

    private void BindButtons()
    {
        if (_restartButton != null)
        {
            _restartButton.onClick.RemoveAllListeners();
            _restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (_settingsButton != null)
        {
            _settingsButton.onClick.RemoveAllListeners();
            _settingsButton.onClick.AddListener(OnSettingsClicked);
        }
    }

    public void SetLevel(int levelNumber)
    {
        _currentLevel = levelNumber;
        RefreshLevelText();
    }

    public void SetCoin(int amount, bool animate = false, bool saveToPrefs = true)
    {
        _currentCoin = amount;
        if (_coinText != null)
        {
            _coinText.text = _currentCoin.ToString();
            if (animate)
            {
                _coinText.transform.DOKill();
                _coinText.transform.localScale = _coinTextScale;
                _coinText.transform.DOPunchScale(_coinTextScale * 0.15f, 0.2f, 2, 0.5f)
                    .SetLink(gameObject);
            }
        }

        if (saveToPrefs)
        {
            PlayerPrefs.SetInt(COIN_PREF_KEY, _currentCoin);
            PlayerPrefs.Save();
        }
    }

    public void AddCoin(int addedAmount, bool animate = true)
    {
        SetCoin(_currentCoin + addedAmount, animate, true);
    }

    private void OnRestartClicked()
    {
        AudioManager.Instance?.PlayClick();
        HapticManager.Instance?.TriggerLight();
        GameManager.Instance?.RestartLevel();
    }

    private void OnSettingsClicked()
    {
        AudioManager.Instance?.PlayClick();
        HapticManager.Instance?.TriggerLight();
        UIManager.Instance?.OpenSettings();
    }
}
