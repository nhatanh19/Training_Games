using DG.Tweening;
using UnityEngine;

public class PassengerController : MonoBehaviour
{
    [Header("Visuals (Authored on Prefab)")]
    [SerializeField] private BusColor color;
    [SerializeField] private SpriteRenderer visualRenderer;
    [SerializeField] private GameVisualThemeSO visualTheme;
    [SerializeField, Min(0.1f)] private float seatedWorldScaleMultiplier = 0.72f;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float boardingJumpHeight = 0.50f;
    [SerializeField, Min(0.01f)] private float boardingDuration = 0.30f;
    [SerializeField, Min(0.01f)] private float queueMoveDuration = 0.18f;
    [SerializeField, Min(0f)] private float landingDuration = 0.10f;

    private Vector3 _prefabScale;
    private bool _capturedPrefabScale;
    private Tween _movementTween;
    private Tween _landingTween;

    public BusColor Color => color;

    private void Awake()
    {
        CapturePrefabScale();
        ApplyVisualColor();
    }

    private void CapturePrefabScale()
    {
        if (_capturedPrefabScale) return;
        _prefabScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
        _capturedPrefabScale = true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyVisualColor();
    }
#endif

    public void SetColor(BusColor newColor, GameVisualThemeSO themeOverride = null)
    {
        CapturePrefabScale();
        if (themeOverride != null) visualTheme = themeOverride;
        color = newColor;
        ApplyVisualColor();
    }

    public void ApplyVisualColor()
    {
        if (visualRenderer == null || visualTheme == null) return;
        visualRenderer.color = visualTheme.GetPassengerTint(color);
    }

    public void SetQueueAppearance(Vector3 worldPosition, bool visibleInLine)
    {
        CapturePrefabScale();
        transform.position = worldPosition;
        transform.localRotation = Quaternion.identity;
        transform.localScale = visibleInLine ? _prefabScale : Vector3.zero;
        gameObject.SetActive(true);
    }

    public void MoveToSeat(Transform seatPoint, System.Action onComplete)
    {
        CapturePrefabScale();
        AudioManager.Instance?.PlayFootstep();

        _movementTween?.Kill();
        _landingTween?.Kill();
        _landingTween = null;

        if (transform.localScale == Vector3.zero)
        {
            transform.localScale = _prefabScale;
        }

        if (visualRenderer != null) visualRenderer.sortingOrder = 55;

        _movementTween = transform.DOJump(seatPoint.position, Mathf.Max(0f, boardingJumpHeight), 1, Mathf.Max(0.01f, boardingDuration))
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                _movementTween = null;
                transform.SetParent(seatPoint, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;

                Vector3 parentLossy = seatPoint.lossyScale;
                Vector3 compensated = new Vector3(
                    parentLossy.x > 0.01f ? (_prefabScale.x * seatedWorldScaleMultiplier) / parentLossy.x : _prefabScale.x,
                    parentLossy.y > 0.01f ? (_prefabScale.y * seatedWorldScaleMultiplier) / parentLossy.y : _prefabScale.y,
                    1f);
                transform.localScale = compensated;
                if (visualRenderer != null) visualRenderer.sortingOrder = 46;

                if (landingDuration > 0f)
                {
                    Vector3 punch = new Vector3(compensated.x * 0.08f, -compensated.y * 0.10f, 0f);
                    _landingTween = transform.DOPunchScale(punch, landingDuration, 2, 0.5f)
                        .SetLink(gameObject)
                        .OnComplete(() => _landingTween = null);
                }
                AudioManager.Instance?.PlayPop();
                HapticManager.Instance?.TriggerLight();

                onComplete?.Invoke();
            });
    }

    public void StepForward(Vector3 targetWorldPos, bool visibleInLine, float delay = 0f)
    {
        CapturePrefabScale();
        _movementTween?.Kill();

        if (!visibleInLine)
        {
            transform.position = targetWorldPos;
            transform.localScale = Vector3.zero;
            return;
        }

        if (transform.localScale.x < 0.01f)
        {
            transform.DOScale(_prefabScale, Mathf.Max(0.05f, queueMoveDuration))
                .SetDelay(Mathf.Max(0f, delay))
                .SetEase(Ease.OutBack)
                .SetLink(gameObject);
        }

        _movementTween = transform.DOMove(targetWorldPos, Mathf.Max(0.01f, queueMoveDuration))
            .SetDelay(Mathf.Max(0f, delay))
            .SetEase(Ease.OutQuad)
            .SetLink(gameObject)
            .OnComplete(() => _movementTween = null);
    }

    public void ResetPassenger()
    {
        CapturePrefabScale();
        _movementTween?.Kill();
        _movementTween = null;
        _landingTween?.Kill();
        _landingTween = null;
        transform.DOKill();
        transform.localScale = _prefabScale;
        transform.localRotation = Quaternion.identity;
        if (visualRenderer != null) visualRenderer.sortingOrder = 30;
    }
}
