using TMPro;
using UnityEngine;

public class LosePopupLocalizer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _buttonText;
    [SerializeField] private TextMeshProUGUI _reviveButtonText;

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

    private void Refresh()
    {
        bool vi = SettingsController.IsVietnameseSaved;
        if (_titleText != null) _titleText.text = vi ? "CẤP ĐỘ THẤT BẠI" : "LEVEL FAILED";
        if (_buttonText != null) _buttonText.text = vi ? "THỬ LẠI" : "TRY AGAIN";
        if (_reviveButtonText != null)
        {
            bool noSlots = BusStopManager.Instance != null && !BusStopManager.Instance.HasLockedSlot;
            _reviveButtonText.text = noSlots
                ? (vi ? "HẾT CHỖ" : "NO SLOTS")
                : (vi ? "HỒI SINH" : "REVIVE");
        }
    }
}
