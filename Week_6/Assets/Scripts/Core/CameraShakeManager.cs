using DG.Tweening;
using UnityEngine;

public class CameraShakeManager : MonoBehaviour
{
    private static CameraShakeManager _instance;

    public static CameraShakeManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<CameraShakeManager>();

            }
            return _instance;
        }
    }

    [SerializeField] private Camera targetCamera;
    private Vector3 _originalPosition;
    private Tween _shakeTween;
    private bool _initialized;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Initialize();
    }

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera != null)
        {
            _originalPosition = targetCamera.transform.position;
        }
    }

    public void ShakeLight(float duration = 0.2f, float strength = 0.08f)
    {
        Shake(duration, strength, 12);
    }

    public void ShakeHeavy(float duration = 0.35f, float strength = 0.22f)
    {
        Shake(duration, strength, 16);
    }

    public void ResetShake()
    {
        if (_shakeTween == null) return;
        _shakeTween.Kill();
        _shakeTween = null;
        if (targetCamera != null) targetCamera.transform.position = _originalPosition;
    }

    public void Shake(float duration, float strength, int vibrato)
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null) return;
            _originalPosition = targetCamera.transform.position;
        }

        _shakeTween?.Kill();
        targetCamera.transform.position = _originalPosition;

        _shakeTween = targetCamera.transform.DOShakePosition(duration, new Vector3(strength, strength, 0f), vibrato, 90f, false, true)
            .OnComplete(() =>
            {
                targetCamera.transform.position = _originalPosition;
                _shakeTween = null;
            });
    }
}
