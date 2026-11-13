using TMPro;
using UnityEngine;

public class WinPopupLocalizer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _subtitleText;
    [SerializeField] private TextMeshProUGUI _rewardText;
    [SerializeField] private TextMeshProUGUI _nextButtonText;

    private void OnEnable()
    {
        SettingsController.LanguageChanged += OnLanguageChanged;
        Refresh();
    }

    private void OnDisable()
    {
        SettingsController.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(string language)
    {
        Refresh();
    }

    public void Refresh()
    {
        bool vi = SettingsController.IsVietnameseSaved;
        if (_titleText != null) _titleText.text = vi ? "HOÀN THÀNH!" : "LEVEL COMPLETED!";
        if (_subtitleText != null)
            _subtitleText.text = vi ? "TẤT CẢ HÀNH KHÁCH ĐÃ LÊN XE!" : "ALL PASSENGERS ARE ON THEIR WAY!";
        if (_nextButtonText != null) _nextButtonText.text = vi ? "TIẾP THEO" : "NEXT LEVEL";

        if (_rewardText != null)
        {
            int reward = GameManager.Instance != null && GameManager.Instance.CurrentLevelData != null
                ? GameManager.Instance.CurrentLevelData.coinReward
                : 8;
            reward = Mathf.Clamp(reward, 5, 10);
            _rewardText.text = $"+{reward}";
        }
    }
}
