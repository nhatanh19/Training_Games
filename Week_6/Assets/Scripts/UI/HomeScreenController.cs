using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays current level progression, difficulty-styled Play button, reward preview, and milestone hint on Home tab.
/// </summary>
public class HomeScreenController : MonoBehaviour
{
    [Header("Data Assets")]
    [SerializeField] private GameVisualThemeSO visualTheme;
    [SerializeField] private LocalizationTableSO localization;
    [SerializeField] private CollectionCatalogSO collectionCatalog;

    [Header("UI Elements (Authored in Prefab)")]
    [SerializeField] private Button playButton;
    [SerializeField] private Image playButtonImage;
    [SerializeField] private TextMeshProUGUI playLevelText;
    [SerializeField] private TextMeshProUGUI playActionLabelText;
    [SerializeField] private GameObject difficultyBadgeRoot;
    [SerializeField] private TextMeshProUGUI difficultyTagText;
    [SerializeField] private TextMeshProUGUI nextRewardText;
    [SerializeField] private TextMeshProUGUI milestoneHintText;

    private bool _bound;

    private void Awake()
    {
        Bind();
    }

    private void OnEnable()
    {
        Bind();
        SettingsController.LanguageChanged += OnLanguageChanged;
        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.OnLevelChanged += OnLevelChanged;
        }
        Refresh();
    }

    private void OnDisable()
    {
        SettingsController.LanguageChanged -= OnLanguageChanged;
        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.OnLevelChanged -= OnLevelChanged;
        }
    }

    private void Bind()
    {
        if (_bound) return;
        _bound = true;
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayClicked);
        }
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
        if (visible) Refresh();
    }

    private void OnLanguageChanged(string lang) => Refresh();
    private void OnLevelChanged(int idx) => Refresh();

    public void Refresh()
    {
        int levelIdx = ProgressManager.Instance != null ? ProgressManager.Instance.CurrentLevelIndex : 0;
        int displayLevel = levelIdx + 1;
        LevelDataSO levelData = LevelLoader.Instance != null ? LevelLoader.Instance.GetLevelDataByIndex(levelIdx) : null;
        LevelDifficulty difficulty = levelData != null ? levelData.difficulty : LevelDifficulty.Normal;

        if (playLevelText != null)
        {
            playLevelText.text = localization != null
                ? localization.Format("home_level_number", displayLevel)
                : displayLevel.ToString();
        }

        if (playActionLabelText != null && localization != null)
        {
            playActionLabelText.text = localization.Get("home_play_btn");
        }

        GameVisualThemeSO.DifficultyVisualEntry diffStyle = visualTheme != null
            ? visualTheme.GetDifficultyEntry(difficulty)
            : null;

        if (playButtonImage != null && diffStyle != null && diffStyle.homePlayButtonSprite != null)
        {
            playButtonImage.sprite = diffStyle.homePlayButtonSprite;
        }

        bool showDiffBadge = difficulty != LevelDifficulty.Normal;
        if (difficultyBadgeRoot != null) difficultyBadgeRoot.SetActive(showDiffBadge);

        if (difficultyTagText != null && localization != null)
        {
            string key = difficulty switch
            {
                LevelDifficulty.Hard => "difficulty_hard",
                LevelDifficulty.SuperHard => "difficulty_superhard",
                _ => "difficulty_normal"
            };
            difficultyTagText.text = localization.Get(key);
        }

        if (nextRewardText != null && levelData != null)
        {
            nextRewardText.text = $"+{levelData.coinReward}";
        }

        RefreshMilestoneHint(displayLevel);
    }

    private void RefreshMilestoneHint(int currentLevel)
    {
        if (milestoneHintText == null || collectionCatalog == null || localization == null) return;

        CollectionCatalogSO.CollectionEntry nextEntry = null;
        int bestDelta = int.MaxValue;
        foreach (var entry in collectionCatalog.Entries)
        {
            if (entry == null || entry.comingSoon) continue;
            if (entry.unlockAtLevel >= currentLevel && entry.unlockAtLevel - currentLevel < bestDelta)
            {
                bestDelta = entry.unlockAtLevel - currentLevel;
                nextEntry = entry;
            }
        }

        if (nextEntry != null)
        {
            string title = localization.Get(nextEntry.titleKey);
            milestoneHintText.text = localization.Format("home_next_unlock", title, nextEntry.unlockAtLevel);
        }
        else
        {
            milestoneHintText.text = localization.Get("home_all_unlocked");
        }
    }

    private void OnPlayClicked()
    {
        AppFlowController.Instance?.EnterGameplay();
    }
}
