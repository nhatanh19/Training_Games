using DG.Tweening;
using UnityEngine;

public class PassengerController : MonoBehaviour
{
    [SerializeField] private BusColor color;
    [SerializeField] private SpriteRenderer visualRenderer;
    [SerializeField] private Vector3 defaultScale = new Vector3(0.85f, 0.85f, 1f);

    [Header("Movement")]
    [SerializeField, Min(0f)] private float boardingJumpHeight = 0.45f;
    [SerializeField, Min(0.01f)] private float boardingDuration = 0.35f;
    [SerializeField, Min(0.01f)] private float queueMoveDuration = 0.20f;
    [SerializeField, Min(0f)] private float landingDuration = 0.12f;

    private Tween _movementTween;
    private Tween _landingTween;

    public BusColor Color => color;

    private void Awake()
    {
        EnsureDefaultScale();
        ApplyColor();
    }

    private void EnsureDefaultScale()
    {
        if (defaultScale == Vector3.zero || defaultScale.x < 0.6f)
        {
            defaultScale = new Vector3(0.85f, 0.85f, 1f);
        }
    }

    private void OnValidate()
    {
        EnsureDefaultScale();
        ApplyColor();
    }

    public void SetColor(BusColor newColor)
    {
        color = newColor;
        ApplyColor();
    }

    public void ApplyColor()
    {
        if (visualRenderer == null)
        {
            visualRenderer = GetComponent<SpriteRenderer>();
            if (visualRenderer == null)
            {
                visualRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        if (visualRenderer != null)
        {
            Sprite passSprite = Resources.Load<Sprite>("Passengers/passenger_white");
            if (passSprite != null)
            {
                visualRenderer.sprite = passSprite;
            }
            visualRenderer.sortingOrder = 30;
            visualRenderer.color = GetVisualColor(color);
        }
    }

    private UnityEngine.Color GetVisualColor(BusColor busColor)
    {
        return busColor switch
        {
            BusColor.Red => new UnityEngine.Color(0.9f, 0.25f, 0.25f),
            BusColor.Blue => new UnityEngine.Color(0.2f, 0.5f, 0.95f),
            BusColor.Green => new UnityEngine.Color(0.25f, 0.85f, 0.35f),
            BusColor.Yellow => new UnityEngine.Color(0.95f, 0.85f, 0.15f),
            _ => UnityEngine.Color.white
        };
    }

    public void MoveToSeat(Transform seatPoint, System.Action onComplete)
    {
        AudioManager.Instance?.PlayFootstep();

        _movementTween?.Kill();
        _landingTween?.Kill();
        _landingTween = null;
        _movementTween = transform.DOJump(seatPoint.position, Mathf.Max(0f, boardingJumpHeight), 1, Mathf.Max(0.01f, boardingDuration))
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                _movementTween = null;
                transform.SetParent(seatPoint, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                EnsureDefaultScale();
                transform.localScale = defaultScale;

                if (landingDuration > 0f)
                {
                    Vector3 punch = Vector3.Scale(defaultScale, new Vector3(0.06f, -0.08f, 0f));
                    _landingTween = transform.DOPunchScale(punch, landingDuration, 2, 0.5f)
                        .SetLink(gameObject)
                        .OnComplete(() => _landingTween = null);
                }
                AudioManager.Instance?.PlayPop();
                HapticManager.Instance?.TriggerLight();

                onComplete?.Invoke();
            });
    }

    public void StepForward(Vector3 targetWorldPos, float delay = 0f)
    {
        _movementTween?.Kill();
        _movementTween = transform.DOMove(targetWorldPos, Mathf.Max(0.01f, queueMoveDuration))
            .SetDelay(Mathf.Max(0f, delay))
            .SetEase(Ease.OutQuad)
            .OnComplete(() => _movementTween = null);
    }

    public void ResetPassenger()
    {
        _movementTween?.Kill();
        _movementTween = null;
        _landingTween?.Kill();
        _landingTween = null;
        transform.DOKill();
        EnsureDefaultScale();
        transform.localScale = defaultScale;
        transform.localRotation = Quaternion.identity;
    }
}
