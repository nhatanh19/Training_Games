using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the LoadingCanvas progress bar, percentage label, and smooth fade transition into MainHub.
/// </summary>
public class LoadingScreenController : MonoBehaviour
{
    [Header("UI References (Authored in Prefab)")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image progressFillImage;
    [SerializeField] private TextMeshProUGUI percentText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private LocalizationTableSO localization;

    [Header("Timing")]
    [SerializeField, Min(0.2f)] private float loadingDuration = 1.35f;
    [SerializeField, Min(0.05f)] private float fadeOutDuration = 0.28f;

    private Sequence _sequence;

    public void PlayLoading(Action onCompleted)
    {
        _sequence?.Kill();
        gameObject.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = false;
        }

        if (statusText != null && localization != null)
        {
            statusText.text = localization.Get("loading_status");
        }

        SetProgress(0f);

        _sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        _sequence.Append(DOVirtual.Float(0f, 1f, loadingDuration, SetProgress).SetEase(Ease.InOutCubic));
        if (canvasGroup != null)
        {
            _sequence.Append(canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.OutQuad));
        }
        _sequence.OnComplete(() =>
        {
            _sequence = null;
            gameObject.SetActive(false);
            onCompleted?.Invoke();
        });
    }

    private void SetProgress(float value)
    {
        float clamped = Mathf.Clamp01(value);
        if (progressFillImage != null)
        {
            progressFillImage.fillAmount = clamped;
        }
        if (percentText != null)
        {
            percentText.text = $"{Mathf.RoundToInt(clamped * 100f)}%";
        }
    }

    private void OnDestroy()
    {
        _sequence?.Kill();
        _sequence = null;
    }
}
