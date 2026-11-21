using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ContinuePopupController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private LocalizationTableSO localization;

    [Header("Buttons")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _coinButton;
    [SerializeField] private Button _adButton;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI _headlineText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _coinCostText;
    [SerializeField] private TextMeshProUGUI _adText;
    [SerializeField] private TextMeshProUGUI _noticeText;

    [Header("Config")]
    [Tooltip("Price of the first extra slot; doubles after every slot unlocked in this level.")]
    [SerializeField] private int _baseCoinCost = 20;

    private bool _bound;
    private bool _closing;
    private BusStopSlot _targetSlot;
    private bool _isRevive;
    private Vector3 _noticeScale = Vector3.one;

    public void Configure(BusStopSlot targetSlot)
    {
        _targetSlot = targetSlot;
        _isRevive = targetSlot == null;
    }

    private int CurrentCost
    {
        get
        {
            int n = BusStopManager.Instance != null ? BusStopManager.Instance.ExtraUnlockCount : 0;
            return _baseCoinCost * (1 << Mathf.Clamp(n, 0, 20));
        }
    }

    private void ShowNotice()
    {
        if (_noticeText == null) return;
        _noticeText.text = localization != null ? localization.Get("not_enough_coins") : string.Empty;
        _noticeText.DOKill();
        _noticeText.gameObject.SetActive(true);
        _noticeText.alpha = 1f;
        _noticeText.transform.DOKill();
        _noticeText.transform.localScale = _noticeScale;
        _noticeText.transform.DOPunchScale(_noticeScale * 0.15f, 0.25f, 2, 0.5f).SetUpdate(true).SetLink(_noticeText.gameObject);
        _noticeText.DOFade(0f, 0.4f).SetDelay(1.3f).SetUpdate(true).SetLink(_noticeText.gameObject)
            .OnComplete(() => _noticeText.gameObject.SetActive(false));
    }

    private void Awake()
    {
        if (_noticeText != null)
        {
            _noticeScale = _noticeText.transform.localScale == Vector3.zero ? Vector3.one : _noticeText.transform.localScale;
        }
        Bind();
    }

    private void OnEnable()
    {
        _closing = false;
        Bind();
        SettingsController.LanguageChanged += OnLanguageChanged;
        Refresh();
    }

    private void OnDisable()
    {
        SettingsController.LanguageChanged -= OnLanguageChanged;
    }

    private void Bind()
    {
        if (_bound) return;
        _bound = true;
        if (_closeButton != null) _closeButton.onClick.AddListener(OnCloseClicked);
        if (_coinButton != null) _coinButton.onClick.AddListener(OnCoinClicked);
        if (_adButton != null) _adButton.onClick.AddListener(OnAdClicked);
    }

    private void OnLanguageChanged(string language) => Refresh();

    private void Refresh()
    {
        if (localization != null)
        {
            if (_headlineText != null)
            {
                _headlineText.text = localization.Get(_isRevive ? "oos_headline_revive" : "oos_headline_unlock");
            }
            if (_descriptionText != null)
            {
                _descriptionText.text = localization.Get(_isRevive ? "oos_desc_revive" : "oos_desc_unlock");
            }
            if (_adText != null)
            {
                _adText.text = localization.Get("ads_demo");
            }
        }

        if (_coinCostText != null) _coinCostText.text = CurrentCost.ToString();
        if (_noticeText != null) _noticeText.gameObject.SetActive(false);
    }

    private bool UnlockSelectedSlot()
    {
        if (BusStopManager.Instance == null) return false;
        return _isRevive
            ? BusStopManager.Instance.UnlockOneSlot()
            : BusStopManager.Instance.UnlockSlot(_targetSlot);
    }

    private void OnCloseClicked()
    {
        if (_closing) return;
        _closing = true;
        UIManager.Instance?.CloseContinuePopup(false);
    }

    private void OnCoinClicked()
    {
        if (_closing || ProgressManager.Instance == null) return;

        int cost = CurrentCost;
        if (ProgressManager.Instance.TotalCoins < cost)
        {
            ShowNotice();
            HapticManager.Instance?.TriggerWarning();

            if (_coinButton != null)
            {
                _coinButton.transform.DOKill(true);
                _coinButton.transform.DOShakePosition(0.3f, new Vector3(12f, 0f, 0f), 20, 90f)
                    .SetUpdate(true)
                    .SetLink(_coinButton.gameObject);
            }
            return;
        }

        if (!UnlockSelectedSlot()) return;

        _closing = true;
        ProgressManager.Instance.TrySpendCoins(cost);
        HapticManager.Instance?.TriggerSuccess();
        UIManager.Instance?.CloseContinuePopup(true);
    }

    private void OnAdClicked()
    {
        if (_closing) return;

        if (UnlockSelectedSlot())
        {
            _closing = true;
            HapticManager.Instance?.TriggerSuccess();
            UIManager.Instance?.CloseContinuePopup(true);
        }
    }
}
