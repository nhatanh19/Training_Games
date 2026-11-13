using DG.Tweening;
using UnityEngine;

public class PassengerController : MonoBehaviour
{
    [SerializeField] private BusColor color;
    [SerializeField] private SpriteRenderer visualRenderer;
    [SerializeField] private Vector3 defaultScale = new Vector3(0.85f, 0.85f, 1f);
    private Tween _movementTween;

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
        _movementTween = transform.DOJump(seatPoint.position, 0.8f, 1, 0.35f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                _movementTween = null;
                transform.SetParent(seatPoint, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                EnsureDefaultScale();
                transform.localScale = defaultScale;

                AudioManager.Instance?.PlayPop();
                HapticManager.Instance?.TriggerLight();

                onComplete?.Invoke();
            });
    }

    public void StepForward(Vector3 targetWorldPos, System.Action onComplete = null)
    {
        AudioManager.Instance?.PlayFootstep();
        HapticManager.Instance?.TriggerLight();

        _movementTween?.Kill();
        _movementTween = transform.DOMove(targetWorldPos, 0.2f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                _movementTween = null;
                onComplete?.Invoke();
            });
    }

    public void ResetPassenger()
    {
        _movementTween?.Kill();
        _movementTween = null;
        transform.DOKill();
        EnsureDefaultScale();
        transform.localScale = defaultScale;
        transform.localRotation = Quaternion.identity;
    }
}
