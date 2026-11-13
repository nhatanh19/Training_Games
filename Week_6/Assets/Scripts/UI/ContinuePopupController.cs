using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ContinuePopupController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _coinButton;
    [SerializeField] private Button _adButton;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI _titleText;
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
        _noticeText.text = SettingsController.IsVietnameseSaved ? "KHÔNG ĐỦ TIỀN" : "NOT ENOUGH COINS";
        _noticeText.DOKill();
        _noticeText.gameObject.SetActive(true);
        _noticeText.alpha = 1f;
        _noticeText.transform.localScale = Vector3.one;
        _noticeText.transform.DOKill();
        _noticeText.transform.DOPunchScale(Vector3.one * 0.2f, 0.25f, 2, 0.5f).SetLink(_noticeText.gameObject);
        _noticeText.DOFade(0f, 0.4f).SetDelay(1.4f).SetLink(_noticeText.gameObject)
            .OnComplete(() => _noticeText.gameObject.SetActive(false));
    }

    private void Awake()
    {
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

    private void OnLanguageChanged(string language)
    {
        Refresh();
    }

    private void Refresh()
    {
        bool vi = SettingsController.IsVietnameseSaved;
        if (_titleText != null) _titleText.text = _isRevive
            ? (vi ? "TIẾP TỤC" : "CONTINUE")
            : (vi ? "MỞ KHÓA" : "UNLOCK");
        if (_headlineText != null) _headlineText.text = _isRevive
            ? (vi ? "HỒI SINH" : "REVIVE")
            : (vi ? "MỞ CHỖ ĐẬU" : "UNLOCK SLOT");
        if (_descriptionText != null)
            _descriptionText.text = _isRevive
                ? (vi ? "VÀ MỞ KHÓA MỘT CHỖ ĐẬU XE" : "AND UNLOCK ONE PARKING SLOT")
                : (vi ? "DÙNG XU HOẶC ADS GIẢ LẬP" : "USE COINS OR DEMO AD");
        if (_coinCostText != null) _coinCostText.text = CurrentCost.ToString();
        if (_noticeText != null) _noticeText.gameObject.SetActive(false);
        if (_adText != null) _adText.text = "ADS DEMO";
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
        AudioManager.Instance?.PlayClick();
        HapticManager.Instance?.TriggerLight();
        UIManager.Instance?.CloseContinuePopup();
    }

    private void OnCoinClicked()
    {
        if (_closing) return;
        AudioManager.Instance?.PlayClick();
        HUDController hud = UIManager.Instance != null ? UIManager.Instance.HUD : null;
        if (hud == null) return;

        int cost = CurrentCost;
        if (hud.CurrentCoin < cost)
        {
            ShowNotice();
            HapticManager.Instance?.TriggerLight();

            _coinButton.transform.DOKill(true);
            _coinButton.transform.DOShakePosition(0.3f, new Vector3(12f, 0f, 0f), 20, 90f)
                .SetLink(_coinButton.gameObject);
            return;
        }

        if (!UnlockSelectedSlot()) return;

        _closing = true;
        hud.SetCoin(hud.CurrentCoin - cost, true, true);
        HapticManager.Instance?.TriggerSuccess();
        UIManager.Instance?.CloseContinuePopup(true);
    }

    private void OnAdClicked()
    {
        if (_closing) return;
        AudioManager.Instance?.PlayClick();

        if (UnlockSelectedSlot())
        {
            _closing = true;
            HapticManager.Instance?.TriggerSuccess();
            UIManager.Instance?.CloseContinuePopup(true);
        }
    }
}
