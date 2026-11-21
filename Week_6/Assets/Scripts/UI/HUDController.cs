using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [Header("Data Assets")]
    [SerializeField] private GameVisualThemeSO visualTheme;
    [SerializeField] private LocalizationTableSO localization;

    [Header("Top Display Elements")]
    [SerializeField] private Image levelBadgeImage;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private GameObject difficultyTabRoot;
    [SerializeField] private TextMeshProUGUI difficultyTabText;
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private Transform coinTargetTransform;

    [Header("Difficulty Intro Toast")]
    [SerializeField] private GameObject difficultyIntroBanner;
    [SerializeField] private CanvasGroup difficultyIntroGroup;
    [SerializeField] private TextMeshProUGUI difficultyIntroText;
    [SerializeField, Min(0.3f)] private float difficultyIntroDuration = 1.35f;

    [Header("Buttons & Modules")]
    [SerializeField] private Button coinShopButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private BoosterBarController boosterBar;
    [SerializeField] private PassengerCounterDisplay passengerCounter;

    private int _displayedCoins = -1;
    private int _currentLevel = 1;
    private LevelDifficulty _currentDifficulty = LevelDifficulty.Normal;
    private Vector3 _coinTargetScale = Vector3.one;
    private Vector3 _coinTextScale = Vector3.one;
    private Tween _coinTween;
    private Sequence _introSequence;
    private bool _bound;

    public int CurrentCoin => ProgressManager.Instance != null ? ProgressManager.Instance.TotalCoins : 0;
    public Vector3 CoinTargetScale => _coinTargetScale;
    public Transform CoinTargetTransform => coinTargetTransform != null ? coinTargetTransform : transform;

    private void Awake()
    {
        BindButtons();
        _coinTargetScale = CoinTargetTransform.localScale == Vector3.zero ? Vector3.one : CoinTargetTransform.localScale;
        if (coinText != null)
        {
            _coinTextScale = coinText.transform.localScale == Vector3.zero ? Vector3.one : coinText.transform.localScale;
        }
        if (difficultyIntroBanner != null) difficultyIntroBanner.SetActive(false);
    }

    private void OnEnable()
    {
        BindButtons();
        SettingsController.LanguageChanged += OnLanguageChanged;
        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.OnCoinsChanged += HandleCoinsChanged;
            RefreshCoinImmediate();
        }
        RefreshLevelDisplay();
    }

    private void OnDisable()
    {
        SettingsController.LanguageChanged -= OnLanguageChanged;
        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.OnCoinsChanged -= HandleCoinsChanged;
        }
    }

    private void BindButtons()
    {
        if (_bound) return;
        _bound = true;

        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
        if (coinShopButton != null) coinShopButton.onClick.AddListener(OnCoinShopClicked);
    }

    public void RefreshAll()
    {
        RefreshCoinImmediate();
        int lv = GameManager.Instance != null ? GameManager.Instance.CurrentLevelIndex + 1 : 1;
        SetLevel(lv);
        if (boosterBar != null) boosterBar.Refresh();
        if (passengerCounter != null) passengerCounter.ResetCounter();
    }

    public void RefreshCoinImmediate()
    {
        _coinTween?.Kill();
        int total = ProgressManager.Instance != null ? ProgressManager.Instance.TotalCoins : 0;
        _displayedCoins = total;
        if (coinText != null) coinText.text = total.ToString();
    }

    public void SetLevel(int levelNumber)
    {
        _currentLevel = Mathf.Max(1, levelNumber);
        LevelDataSO data = GameManager.Instance != null ? GameManager.Instance.CurrentLevelData : null;
        _currentDifficulty = data != null ? data.difficulty : LevelDifficulty.Normal;

        RefreshLevelDisplay();
        if (boosterBar != null) boosterBar.Refresh();
        PlayDifficultyIntroIfNeeded();
    }

    private void OnLanguageChanged(string lang)
    {
        RefreshLevelDisplay();
    }

    private void RefreshLevelDisplay()
    {
        if (levelText != null)
        {
            levelText.text = localization != null
                ? localization.Format("hud_level", _currentLevel)
                : $"{_currentLevel}";
        }

        GameVisualThemeSO.DifficultyVisualEntry entry = visualTheme != null
            ? visualTheme.GetDifficultyEntry(_currentDifficulty)
            : null;

        if (levelBadgeImage != null && entry != null && entry.hudLevelBadgeSprite != null)
        {
            levelBadgeImage.sprite = entry.hudLevelBadgeSprite;
        }

        bool isSpecial = _currentDifficulty != LevelDifficulty.Normal;
        if (difficultyTabRoot != null) difficultyTabRoot.SetActive(isSpecial);
        if (difficultyTabText != null && isSpecial && localization != null)
        {
            string key = _currentDifficulty == LevelDifficulty.SuperHard ? "difficulty_superhard" : "difficulty_hard";
            difficultyTabText.text = localization.Get(key);
        }
    }

    private void PlayDifficultyIntroIfNeeded()
    {
        if (difficultyIntroBanner == null || _currentDifficulty == LevelDifficulty.Normal)
        {
            if (difficultyIntroBanner != null) difficultyIntroBanner.SetActive(false);
            return;
        }

        if (difficultyIntroText != null && localization != null)
        {
            string key = _currentDifficulty == LevelDifficulty.SuperHard
                ? "intro_superhard"
                : "intro_hard";
            difficultyIntroText.text = localization.Get(key);
        }

        _introSequence?.Kill();
        difficultyIntroBanner.SetActive(true);
        if (difficultyIntroGroup != null) difficultyIntroGroup.alpha = 0f;
        difficultyIntroBanner.transform.localScale = Vector3.one * 0.85f;

        _introSequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        _introSequence.Join(difficultyIntroBanner.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack));
        if (difficultyIntroGroup != null) _introSequence.Join(difficultyIntroGroup.DOFade(1f, 0.22f));
        _introSequence.AppendInterval(difficultyIntroDuration);
        if (difficultyIntroGroup != null) _introSequence.Append(difficultyIntroGroup.DOFade(0f, 0.28f));
        _introSequence.OnComplete(() =>
        {
            _introSequence = null;
            if (difficultyIntroBanner != null) difficultyIntroBanner.SetActive(false);
        });
    }

    private void HandleCoinsChanged(int newTotal)
    {
        if (coinText == null) return;
        if (_displayedCoins < 0)
        {
            RefreshCoinImmediate();
            return;
        }

        int from = _displayedCoins;
        _coinTween?.Kill();
        _coinTween = DOVirtual.Int(from, newTotal, 0.35f, val =>
        {
            _displayedCoins = val;
            if (coinText != null) coinText.text = val.ToString();
        }).SetUpdate(true).SetLink(gameObject);

        coinText.transform.DOKill();
        coinText.transform.localScale = _coinTextScale;
        coinText.transform.DOPunchScale(_coinTextScale * 0.15f, 0.2f, 2, 0.5f).SetUpdate(true).SetLink(gameObject);
    }

    private void OnSettingsClicked()
    {
        UIManager.Instance?.OpenSettings(inGameplay: true);
    }

    private void OnCoinShopClicked()
    {
        AppFlowController.Instance?.OpenShopFromGameplay();
    }
}
