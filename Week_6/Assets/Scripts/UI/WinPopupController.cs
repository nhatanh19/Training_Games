using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls WinPopup: base coin reward, oscillating Multiplier bar, Claim xN (Ads Demo), and Next Level.
/// </summary>
public class WinPopupController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private LocalizationTableSO localization;

    [Header("Reward & Multiplier")]
    [SerializeField] private RewardMultiplierBar multiplierBar;
    [SerializeField] private TextMeshProUGUI baseRewardText;
    [SerializeField] private TextMeshProUGUI multiplierRewardText;
    [SerializeField] private TextMeshProUGUI multiplierBadgeText;
    [SerializeField] private Button claimMultiplierButton;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Transform coinBurstOrigin;
    [SerializeField, Min(0.2f)] private float rewardTimeoutSeconds = 3f;

    private bool _bound;
    private bool _claiming;
    private int _baseReward;
    private int _claimGeneration;
    private Tween _timeoutTween;

    private void Awake()
    {
        Bind();
    }

    private void OnEnable()
    {
        Bind();
        _claiming = false;
        SetButtonsInteractable(true);
        SettingsController.LanguageChanged += OnLanguageChanged;

        if (multiplierBar != null)
        {
            multiplierBar.OnMultiplierChanged += HandleMultiplierChanged;
            multiplierBar.StartOscillating();
        }

        Refresh();
    }

    private void OnDisable()
    {
        SettingsController.LanguageChanged -= OnLanguageChanged;
        if (multiplierBar != null)
        {
            multiplierBar.OnMultiplierChanged -= HandleMultiplierChanged;
            multiplierBar.StopAndSample();
        }
        _timeoutTween?.Kill();
        _timeoutTween = null;
    }

    private void Bind()
    {
        if (_bound) return;
        _bound = true;
        if (claimMultiplierButton != null) claimMultiplierButton.onClick.AddListener(OnClaimMultiplierClicked);
        if (nextLevelButton != null) nextLevelButton.onClick.AddListener(OnNextClicked);
    }

    private void OnLanguageChanged(string lang) => Refresh();

    private void HandleMultiplierChanged(int mult)
    {
        UpdateMultiplierLabels(mult);
    }

    public void Refresh()
    {
        LevelDataSO data = GameManager.Instance != null ? GameManager.Instance.CurrentLevelData : null;
        _baseReward = data != null ? Mathf.Max(0, data.coinReward) : 25;

        if (baseRewardText != null) baseRewardText.text = $"+{_baseReward}";

        int mult = multiplierBar != null ? multiplierBar.CurrentMultiplier : 2;
        UpdateMultiplierLabels(mult);
    }

    private void UpdateMultiplierLabels(int mult)
    {
        int total = _baseReward * Mathf.Max(1, mult);
        if (multiplierBadgeText != null) multiplierBadgeText.text = $"x{mult}";
        if (multiplierRewardText != null)
        {
            multiplierRewardText.text = localization != null
                ? localization.Format("win_claim_mult", total)
                : $"+{total}";
        }
    }

    private void OnClaimMultiplierClicked()
    {
        if (_claiming || GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.GameWin) return;
        int mult = multiplierBar != null ? multiplierBar.StopAndSample() : 2;
        UpdateMultiplierLabels(mult);
        GrantAndProceed(_baseReward * mult, Mathf.Clamp(mult * 3, 6, 15));
    }

    private void OnNextClicked()
    {
        if (_claiming || GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.GameWin) return;
        multiplierBar?.StopAndSample();
        GrantAndProceed(_baseReward, Mathf.Clamp(_baseReward / 5, 5, 10));
    }

    private void GrantAndProceed(int coinAmount, int visualCoins)
    {
        _claiming = true;
        int gen = ++_claimGeneration;
        SetButtonsInteractable(false);

        GameManager.Instance?.AddCoins(coinAmount);

        Transform hudTarget = UIManager.Instance != null && UIManager.Instance.HUD != null
            ? UIManager.Instance.HUD.CoinTargetTransform
            : null;
        Vector3 origin = coinBurstOrigin != null ? coinBurstOrigin.position : transform.position;

        _timeoutTween?.Kill();
        _timeoutTween = DOVirtual.DelayedCall(rewardTimeoutSeconds, () => FinishClaim(gen), true).SetLink(gameObject);

        if (CoinFlyEffectManager.Instance != null && hudTarget != null)
        {
            CoinFlyEffectManager.Instance.PlayFlyCoins(origin, hudTarget, visualCoins, () => FinishClaim(gen));
        }
        else
        {
            FinishClaim(gen);
        }
    }

    private void FinishClaim(int gen)
    {
        if (!_claiming || gen != _claimGeneration) return;
        _claiming = false;
        _timeoutTween?.Kill();
        _timeoutTween = null;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideWinAndAdvance();
        }
        else
        {
            GameManager.Instance?.NextLevel();
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (claimMultiplierButton != null) claimMultiplierButton.interactable = interactable;
        if (nextLevelButton != null) nextLevelButton.interactable = interactable;
    }
}
