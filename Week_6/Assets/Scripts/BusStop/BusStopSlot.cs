using DG.Tweening;
using UnityEngine;

public class BusStopSlot : MonoBehaviour
{
    [Header("Prefab Visual & Interaction References (Authored in Editor)")]
    [SerializeField] private SpriteRenderer bayHighlightRenderer;
    [SerializeField] private GameObject lockedVisualRoot;
    [SerializeField] private BoxCollider2D clickCollider;
    [SerializeField] private Transform parkedVehicleAnchor;
    [SerializeField, Min(0.1f)] private float parkedVehicleScale = 0.56f;

    [Header("State Feedback Colors")]
    [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private Color reservedColor = new Color(0.45f, 0.85f, 1f, 0.35f);
    [SerializeField] private Color nearFullWarningColor = new Color(1f, 0.65f, 0.15f, 0.45f);
    [SerializeField] private Color fullFlashColor = new Color(1f, 0.28f, 0.22f, 0.65f);

    private Tween _feedbackTween;
    private Tween _warningTween;
    private bool _showingNearFull;

    public Transform SlotTransform => parkedVehicleAnchor != null ? parkedVehicleAnchor : transform;
    public Transform ParkedVehicleAnchor => parkedVehicleAnchor != null ? parkedVehicleAnchor : transform;
    public float ParkedVehicleScale => parkedVehicleScale;
    public BusController CurrentBus { get; private set; }
    public bool IsLocked { get; private set; }
    public bool IsAvailable => !IsLocked && CurrentBus == null;

    private void Awake()
    {
        SetHighlightImmediate(idleColor);
    }

    private void OnDisable()
    {
        StopFeedback();
        StopNearFullWarning();
    }

    public void SetLocked(bool locked)
    {
        StopFeedback();
        StopNearFullWarning();
        IsLocked = locked;

        if (lockedVisualRoot != null)
        {
            lockedVisualRoot.SetActive(locked);
        }

        if (clickCollider != null)
        {
            clickCollider.enabled = locked;
        }

        SetHighlightImmediate(idleColor);
    }

    public void OnLockedSlotClicked()
    {
        if (!IsLocked) return;
        AudioManager.Instance?.PlayClick();
        HapticManager.Instance?.TriggerLight();
        UIManager.Instance?.OpenContinuePopup(this);
    }

    public void AssignBus(BusController bus)
    {
        StopFeedback();
        StopNearFullWarning();
        CurrentBus = bus;
        SetHighlightImmediate(bus != null ? reservedColor : idleColor);
    }

    public void NotifyBusArrived(BusController bus)
    {
        if (CurrentBus != bus) return;
        StopFeedback();
        SetHighlightImmediate(idleColor);
    }

    public void SetNearFullWarning(bool active)
    {
        if (!IsAvailable) active = false;
        if (_showingNearFull == active) return;
        _showingNearFull = active;

        if (!active)
        {
            StopNearFullWarning();
            return;
        }

        if (bayHighlightRenderer == null) return;
        _warningTween?.Kill();
        bayHighlightRenderer.color = new Color(nearFullWarningColor.r, nearFullWarningColor.g, nearFullWarningColor.b, 0.08f);
        _warningTween = bayHighlightRenderer.DOFade(nearFullWarningColor.a, 0.55f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetLink(gameObject);
    }

    public void PlayFullFeedback()
    {
        if (IsLocked || CurrentBus == null || bayHighlightRenderer == null) return;

        StopFeedback();
        Color restore = idleColor;
        Sequence seq = DOTween.Sequence();
        _feedbackTween = seq;
        seq.Append(bayHighlightRenderer.DOColor(fullFlashColor, 0.12f));
        seq.Append(bayHighlightRenderer.DOColor(restore, 0.24f));
        seq.SetLink(gameObject);
        seq.OnComplete(() => _feedbackTween = null);
    }

    public void ClearSlot()
    {
        StopFeedback();
        StopNearFullWarning();
        CurrentBus = null;
        SetHighlightImmediate(idleColor);
    }

    private void StopFeedback()
    {
        if (_feedbackTween == null) return;
        _feedbackTween.Kill();
        _feedbackTween = null;
    }

    private void StopNearFullWarning()
    {
        _showingNearFull = false;
        if (_warningTween == null) return;
        _warningTween.Kill();
        _warningTween = null;
        SetHighlightImmediate(CurrentBus != null ? reservedColor : idleColor);
    }

    private void SetHighlightImmediate(Color c)
    {
        if (bayHighlightRenderer != null)
        {
            bayHighlightRenderer.color = c;
        }
    }
}
