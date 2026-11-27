using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// View component for an instantiated Collection card prefab.
/// </summary>
public class CollectionCardView : MonoBehaviour
{
    [Header("Prefab References (Authored in Editor)")]
    [SerializeField] private Image vehicleIconImage;
    [SerializeField] private Image lockOverlayImage;
    [SerializeField] private GameObject lockedOverlayRoot;
    [SerializeField] private GameObject unlockedContentRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button claimButton;
    [SerializeField] private TextMeshProUGUI claimRewardText;
    [SerializeField] private GameObject claimedBadgeRoot;
    [SerializeField] private Transform coinOrigin;

    private Action<CollectionCardView> _onClaim;

    public Transform CoinOrigin => coinOrigin != null ? coinOrigin : transform;

    private void Awake()
    {
        if (claimButton != null)
        {
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(() => _onClaim?.Invoke(this));
        }
    }

    public void Bind(
        CollectionCatalogSO.CollectionEntry entry,
        string title,
        string status,
        bool unlocked,
        bool claimed,
        Action<CollectionCardView> onClaim)
    {
        _onClaim = onClaim;

        bool showClaimedBadge = !entry.comingSoon && unlocked && claimed && claimedBadgeRoot != null;
        if (titleText != null) titleText.text = title;
        if (statusText != null)
        {
            statusText.text = status;
            statusText.gameObject.SetActive(!showClaimedBadge);
        }

        if (entry.comingSoon)
        {
            if (unlockedContentRoot != null) unlockedContentRoot.SetActive(false);
            if (lockedOverlayRoot != null) lockedOverlayRoot.SetActive(true);
            if (lockOverlayImage != null && entry.lockedOverlaySprite != null) lockOverlayImage.sprite = entry.lockedOverlaySprite;
            if (claimButton != null) claimButton.gameObject.SetActive(false);
            if (claimedBadgeRoot != null) claimedBadgeRoot.SetActive(false);
            return;
        }

        if (unlockedContentRoot != null) unlockedContentRoot.SetActive(true);
        if (lockedOverlayRoot != null) lockedOverlayRoot.SetActive(!unlocked);
        if (vehicleIconImage != null && entry.vehicleSprite != null)
        {
            vehicleIconImage.sprite = entry.vehicleSprite;
            vehicleIconImage.color = unlocked ? Color.white : new Color(0.25f, 0.25f, 0.30f, 0.75f);
        }

        bool canClaim = unlocked && !claimed && entry.claimCoinReward > 0;
        if (claimButton != null) claimButton.gameObject.SetActive(canClaim);
        if (claimRewardText != null) claimRewardText.text = $"+{entry.claimCoinReward}";
        if (claimedBadgeRoot != null) claimedBadgeRoot.SetActive(showClaimedBadge);
    }
}
