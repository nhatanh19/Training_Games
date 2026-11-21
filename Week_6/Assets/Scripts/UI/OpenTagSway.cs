using DG.Tweening;
using UnityEngine;

/// <summary>
/// Gentle pendulum swing animation for the hanging Open_tag sign on LoadingCanvas.
/// </summary>
public class OpenTagSway : MonoBehaviour
{
    [SerializeField, Range(0f, 30f)] private float maxAngle = 6f;
    [SerializeField, Min(0.1f)] private float halfPeriod = 1.1f;

    private Quaternion _initialRotation;
    private Tween _swayTween;

    private void Awake()
    {
        _initialRotation = transform.localRotation;
    }

    private void OnEnable()
    {
        _swayTween?.Kill();
        Vector3 baseEuler = _initialRotation.eulerAngles;
        transform.localRotation = Quaternion.Euler(baseEuler.x, baseEuler.y, baseEuler.z - maxAngle);
        _swayTween = transform.DOLocalRotate(new Vector3(baseEuler.x, baseEuler.y, baseEuler.z + maxAngle), halfPeriod)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    private void OnDisable()
    {
        _swayTween?.Kill();
        _swayTween = null;
        transform.localRotation = _initialRotation;
    }
}
