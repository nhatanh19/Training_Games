using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the 3 bottom Gameplay booster buttons, charge/coin badges, lock overlays, and VIP targeting bar.
/// </summary>
public class BoosterBarController : MonoBehaviour
{
    [Serializable]
    public class BoosterSlotView
    {
        public BoosterType type;
        public Button button;
        public Image iconImage;
        public GameObject countBadgeRoot;
        public TextMeshProUGUI countText;
        public GameObject lockOverlayRoot;
        public TextMeshProUGUI unlockText;
        public GameObject activeOutline;
    }

    [Header("Data")]
    [SerializeField] private BoosterCatalogSO catalog;
    [SerializeField] private LocalizationTableSO localization;

    [Header("Booster Slots (Wired in Prefab)")]
    [SerializeField] private List<BoosterSlotView> slots = new List<BoosterSlotView>();

    [Header("VIP Targeting & Feedback")]
    [SerializeField] private GameObject vipTargetingBanner;
    [SerializeField] private Button cancelVipButton;
    [SerializeField] private TextMeshProUGUI vipPromptText;
    [SerializeField] private TextMeshProUGUI feedbackText;

    private bool _bound;
    private Tween _feedbackTween;

    private void Awake()
    {
        if (vipTargetingBanner != null) vipTargetingBanner.SetActive(false);
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);
        Bind();
    }

    private void OnEnable()
    {
        Bind();
        SettingsController.LanguageChanged += OnLanguageChanged;
        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.OnBoostersChanged += Refresh;
            ProgressManager.Instance.OnCoinsChanged += OnCoinsChanged;
        }
        if (BoosterManager.Instance != null)
        {
            BoosterManager.Instance.OnVipTargetingChanged += HandleVipTargetingChanged;
            BoosterManager.Instance.OnBoosterFeedback += HandleBoosterFeedback;
        }
        Refresh();
    }

    private void OnDisable()
    {
        SettingsController.LanguageChanged -= OnLanguageChanged;
        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.OnBoostersChanged -= Refresh;
            ProgressManager.Instance.OnCoinsChanged -= OnCoinsChanged;
        }
        if (BoosterManager.Instance != null)
        {
            BoosterManager.Instance.OnVipTargetingChanged -= HandleVipTargetingChanged;
            BoosterManager.Instance.OnBoosterFeedback -= HandleBoosterFeedback;
        }
    }

    private void Bind()
    {
        if (_bound) return;
        _bound = true;

        foreach (var slot in slots)
        {
            if (slot == null || slot.button == null) continue;
            BoosterType captured = slot.type;
            slot.button.onClick.RemoveAllListeners();
            slot.button.onClick.AddListener(() => BoosterManager.Instance?.OnBoosterButtonClicked(captured));
        }

        if (cancelVipButton != null)
        {
            cancelVipButton.onClick.RemoveAllListeners();
            cancelVipButton.onClick.AddListener(() => BoosterManager.Instance?.CancelVipTargeting());
        }
    }

    private void OnLanguageChanged(string lang) => Refresh();
    private void OnCoinsChanged(int coins) => Refresh();

    public void Refresh()
    {
        int currentLevel = GameManager.Instance != null ? GameManager.Instance.CurrentLevelIndex + 1 : 1;
        bool vipActive = BoosterManager.Instance != null && BoosterManager.Instance.IsVipTargeting;

        foreach (var slot in slots)
        {
            if (slot == null) continue;
            BoosterCatalogSO.BoosterDefinition def = catalog != null ? catalog.GetDefinition(slot.type) : null;
            int unlockLv = def != null ? def.unlockLevel : 1;
            bool unlocked = currentLevel >= unlockLv;
            int count = ProgressManager.Instance != null ? ProgressManager.Instance.GetBoosterCount(slot.type) : 0;

            if (slot.iconImage != null && def != null && def.iconSprite != null)
            {
                slot.iconImage.sprite = def.iconSprite;
                slot.iconImage.color = unlocked ? Color.white : new Color(0.75f, 0.75f, 0.8f, 0.55f);
            }

            if (slot.lockOverlayRoot != null) slot.lockOverlayRoot.SetActive(!unlocked);
            if (slot.unlockText != null)
            {
                slot.unlockText.text = localization != null
                    ? localization.Format("booster_unlock_lv", unlockLv)
                    : $"Lv.{unlockLv}";
            }

            if (slot.countBadgeRoot != null) slot.countBadgeRoot.SetActive(unlocked);
            if (slot.countText != null && unlocked)
            {
                if (count > 0)
                {
                    slot.countText.text = count.ToString();
                }
                else
                {
                    int cost = def != null ? def.refillCoinCost : 50;
                    slot.countText.text = $"{cost}";
                }
            }

            if (slot.activeOutline != null)
            {
                slot.activeOutline.SetActive(vipActive && slot.type == BoosterType.VipSend);
            }
        }

        HandleVipTargetingChanged(vipActive);
    }

    private void HandleVipTargetingChanged(bool active)
    {
        if (vipTargetingBanner != null) vipTargetingBanner.SetActive(active);
        if (active && vipPromptText != null && localization != null)
        {
            vipPromptText.text = localization.Get("booster_vip_prompt");
        }

        foreach (var slot in slots)
        {
            if (slot != null && slot.activeOutline != null)
            {
                slot.activeOutline.SetActive(active && slot.type == BoosterType.VipSend);
            }
        }
    }

    private void HandleBoosterFeedback(BoosterType type, string code)
    {
        if (feedbackText == null) return;
        string key = code switch
        {
            "LOCKED" => "booster_locked_msg",
            "NO_COINS" => "not_enough_coins",
            "BAY_FULL" => "booster_bay_full",
            "ALREADY_SORTED" => "booster_already_sorted",
            _ => "booster_no_target"
        };

        feedbackText.text = localization != null ? localization.Get(key) : key;
        _feedbackTween?.Kill();
        feedbackText.gameObject.SetActive(true);
        feedbackText.alpha = 1f;
        _feedbackTween = feedbackText.DOFade(0f, 0.35f).SetDelay(1.2f).SetUpdate(true).SetLink(gameObject)
            .OnComplete(() => feedbackText.gameObject.SetActive(false));
    }
}
