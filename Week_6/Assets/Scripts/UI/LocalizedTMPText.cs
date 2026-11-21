using TMPro;
using UnityEngine;

/// <summary>
/// Binds a TextMeshProUGUI component to a key in LocalizationTableSO.
/// Leaves text untouched if table or localizationKey is unset so designers can author static text freely.
/// </summary>
public class LocalizedTMPText : MonoBehaviour
{
    [SerializeField] private LocalizationTableSO table;
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private string localizationKey;
    [SerializeField] private bool uppercase = false;

    private void Awake()
    {
        if (targetText == null) targetText = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        SettingsController.LanguageChanged += OnLanguageChanged;
        Refresh();
    }

    private void OnDisable()
    {
        SettingsController.LanguageChanged -= OnLanguageChanged;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (targetText == null) targetText = GetComponent<TextMeshProUGUI>();
    }
#endif

    private void OnLanguageChanged(string lang)
    {
        Refresh();
    }

    [ContextMenu("Refresh Localized Text")]
    public void Refresh()
    {
        if (targetText == null || table == null || string.IsNullOrEmpty(localizationKey)) return;
        string value = table.Get(localizationKey);
        targetText.text = uppercase ? value.ToUpperInvariant() : value;
    }
}
