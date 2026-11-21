using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// View component for an instantiated Shop offer prefab (Bundle, Coin Pack, or Booster Pack).
/// </summary>
public class ShopOfferItemView : MonoBehaviour
{
    [Header("Prefab References (Authored in Editor)")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button buyButton;
    [SerializeField] private GameObject loadingSpinnerRoot;
    [SerializeField] private Transform rewardFlyOrigin;

    private Action<ShopOfferItemView> _onBuyClicked;
    private Tween _spinTween;
    private bool _busy;

    public Transform RewardFlyOrigin => rewardFlyOrigin != null ? rewardFlyOrigin : transform;

    private void Awake()
    {
        if (loadingSpinnerRoot != null) loadingSpinnerRoot.SetActive(false);
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(HandleClick);
        }
    }

    private void OnDisable()
    {
        StopSpinner();
    }

    public void Bind(string title, string subtitle, string reward, string price, Sprite icon, Action<ShopOfferItemView> onBuy)
    {
        if (titleText != null) titleText.text = title;
        if (subtitleText != null) subtitleText.text = subtitle;
        if (rewardText != null) rewardText.text = reward;
        if (priceText != null) priceText.text = price;
        if (iconImage != null && icon != null) iconImage.sprite = icon;
        _onBuyClicked = onBuy;
        StopSpinner();
    }

    public void PlayPurchaseProcess(Action onReadyToGrant)
    {
        if (_busy) return;
        _busy = true;
        if (buyButton != null) buyButton.interactable = false;

        if (loadingSpinnerRoot != null)
        {
            loadingSpinnerRoot.SetActive(true);
            loadingSpinnerRoot.transform.localRotation = Quaternion.identity;
            _spinTween = loadingSpinnerRoot.transform.DORotate(new Vector3(0f, 0f, -360f), 0.32f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    StopSpinner();
                    onReadyToGrant?.Invoke();
                });
        }
        else
        {
            _busy = false;
            if (buyButton != null) buyButton.interactable = true;
            onReadyToGrant?.Invoke();
        }
    }

    private void StopSpinner()
    {
        _spinTween?.Kill();
        _spinTween = null;
        _busy = false;
        if (loadingSpinnerRoot != null) loadingSpinnerRoot.SetActive(false);
        if (buyButton != null) buyButton.interactable = true;
    }

    private void HandleClick()
    {
        if (_busy) return;
        _onBuyClicked?.Invoke(this);
    }
}
