using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Editor-configurable tactile press scale, click SFX, and haptic feedback for UI buttons.
/// </summary>
public class UIButtonJuice : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private Button targetButton;
    [SerializeField, Range(0.8f, 1f)] private float pressedScale = 0.93f;
    [SerializeField, Min(0.01f)] private float duration = 0.08f;
    [SerializeField] private bool playClickSound = true;
    [SerializeField] private bool playHaptic = true;

    private Vector3 _originalScale;
    private bool _capturedScale;
    private Tween _scaleTween;

    private void Awake()
    {
        if (targetButton == null) targetButton = GetComponent<Button>();
        CaptureScale();
    }

    private void OnEnable()
    {
        CaptureScale();
        transform.localScale = _originalScale;
    }

    private void OnDisable()
    {
        _scaleTween?.Kill();
        _scaleTween = null;
        if (_capturedScale) transform.localScale = _originalScale;
    }

    private void CaptureScale()
    {
        if (_capturedScale) return;
        _originalScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
        _capturedScale = true;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
        if (targetButton != null && (!targetButton.isActiveAndEnabled || !targetButton.IsInteractable())) return;

        CaptureScale();
        _scaleTween?.Kill();
        _scaleTween = transform.DOScale(_originalScale * pressedScale, Mathf.Max(0.01f, duration))
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .SetLink(gameObject)
            .OnComplete(() => _scaleTween = null);

        if (playClickSound) AudioManager.Instance?.PlayClick();
        if (playHaptic) HapticManager.Instance?.TriggerLight();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        RestoreScale();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        RestoreScale();
    }

    private void RestoreScale()
    {
        if (!_capturedScale) return;
        _scaleTween?.Kill();
        _scaleTween = transform.DOScale(_originalScale, Mathf.Max(0.01f, duration))
            .SetEase(Ease.OutBack)
            .SetUpdate(true)
            .SetLink(gameObject)
            .OnComplete(() => _scaleTween = null);
    }
}
