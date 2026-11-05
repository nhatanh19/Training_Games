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
    [SerializeField] private Button _addCoinButton; // Placeholder: purchase flow will be hooked up later

    public const string COIN_PREF_KEY = "TotalCoins";

    private int _currentCoin = 0;
    private int _currentLevel = 1;

    public int CurrentCoin => _currentCoin;

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
        EnsureCoinDisplay();
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

    private void EnsureCoinDisplay()
    {
        if (_coinTargetTransform != null)
        {
            // 1. Ensure CoinContainer background retains its sleek dark pill styling
            if (_coinTargetTransform.name == "CoinContainer")
            {
                Image bgImg = _coinTargetTransform.GetComponent<Image>();
                if (bgImg != null && bgImg.sprite == null)
                {
                    bgImg.sprite = null;
                    bgImg.color = new Color(0f, 0f, 0f, 0.35f);
                }
            }

            // 2. Find the actual CoinIcon child to replace the placeholder
            Transform iconTrans = _coinTargetTransform.Find("CoinIcon");
            if (iconTrans == null)
            {
                // Fallback search in children
                Image[] images = _coinTargetTransform.GetComponentsInChildren<Image>(true);
                foreach (Image img in images)
                {
                    if (img.gameObject != _coinTargetTransform.gameObject)
                    {
                        iconTrans = img.transform;
                        break;
                    }
                }
            }

            if (iconTrans != null)
            {
                Image coinImg = iconTrans.GetComponent<Image>();
                if (coinImg != null)
                {
                    Sprite coinSprite = Resources.Load<Sprite>("Sprites/coin_icon");
                    if (coinSprite == null)
                    {
                        Texture2D tex = Resources.Load<Texture2D>("Sprites/coin_icon");
                        if (tex != null)
                        {
                            coinSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                        }
                    }

                    if (coinSprite != null)
                    {
                        coinImg.sprite = coinSprite;
                    }
                    coinImg.color = Color.white;
                    coinImg.preserveAspect = true;
                }
            }
        }
    }

    private void BindButtons()
    {
        if (_restartButton != null)
        {
            _restartButton.onClick.RemoveAllListeners();
            _restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (_addCoinButton != null)
        {
            _addCoinButton.onClick.RemoveAllListeners();
            _addCoinButton.onClick.AddListener(OnAddCoinClicked);
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
                _coinText.transform.localScale = Vector3.one;
                _coinText.transform.DOPunchScale(Vector3.one * 0.25f, 0.2f, 2, 0.5f)
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

    private void OnAddCoinClicked()
    {
        AudioManager.Instance?.PlayClick();
        HapticManager.Instance?.TriggerLight();
        UIManager.Instance?.OpenContinuePopup();
    }

    private void OnSettingsClicked()
    {
        AudioManager.Instance?.PlayClick();
        HapticManager.Instance?.TriggerLight();
        UIManager.Instance?.OpenSettings();
    }
}
