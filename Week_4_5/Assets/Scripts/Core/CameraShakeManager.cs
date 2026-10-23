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
                if (_instance == null)
                {
                    GameObject go = new GameObject("CameraShakeManager");
                    _instance = go.AddComponent<CameraShakeManager>();
                }
            }
            return _instance;
        }
    }

    [SerializeField] private Camera targetCamera;
    private Vector3 _originalPosition;
    private Tween _shakeTween;

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
            });
    }
}
