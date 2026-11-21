using UnityEngine;

/// <summary>
/// Automatically adjusts an orthographic camera so the playfield and scene background fit cleanly across screen ratios.
/// </summary>
[ExecuteAlways]
public class CameraViewportFitter : MonoBehaviour
{
    public enum FitMode
    {
        FitPlayfieldWidthThenClampHeight = 0,
        FitBackgroundWidth = 1,
        ContainPlayfieldAndSlots = 2
    }

    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private PlayfieldBounds playfieldBounds;

    [Header("Fit Settings")]
    [SerializeField] private FitMode fitMode = FitMode.FitPlayfieldWidthThenClampHeight;
    [SerializeField, Min(0f)] private float horizontalMargin = 0.20f;
    [SerializeField, Min(0f)] private float verticalMargin = 0.35f;
    [SerializeField, Min(3f)] private float minOrthoSize = 7.40f;
    [SerializeField, Min(4f)] private float maxOrthoSize = 11.50f;

    private int _lastWidth = -1;
    private int _lastHeight = -1;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = GetComponent<Camera>();
        ApplyFit();
    }

    private void Start()
    {
        ApplyFit();
    }

    private void Update()
    {
        if (Screen.width != _lastWidth || Screen.height != _lastHeight)
        {
            ApplyFit();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (targetCamera == null) targetCamera = GetComponent<Camera>();
        ApplyFit();
    }
#endif

    public void ApplyFit()
    {
        if (targetCamera == null || !targetCamera.orthographic) return;
        if (Screen.width <= 0 || Screen.height <= 0) return;

        _lastWidth = Screen.width;
        _lastHeight = Screen.height;
        float aspect = Mathf.Max(0.25f, targetCamera.aspect);

        float desiredHalfWidth = 3.90f;
        float desiredHalfHeight = minOrthoSize;

        if (playfieldBounds != null)
        {
            desiredHalfWidth = playfieldBounds.Size.x * 0.5f + horizontalMargin;
            desiredHalfHeight = Mathf.Max(desiredHalfHeight, playfieldBounds.Size.y * 0.5f + verticalMargin);
        }

        if (fitMode == FitMode.FitBackgroundWidth && backgroundRenderer != null && backgroundRenderer.sprite != null)
        {
            desiredHalfWidth = Mathf.Max(desiredHalfWidth, backgroundRenderer.bounds.extents.x);
        }

        float sizeForWidth = desiredHalfWidth / aspect;
        float targetSize = Mathf.Max(desiredHalfHeight, sizeForWidth);

        if (fitMode == FitMode.FitPlayfieldWidthThenClampHeight &&
            backgroundRenderer != null && backgroundRenderer.sprite != null)
        {
            float bgHalfHeight = backgroundRenderer.bounds.extents.y;
            if (bgHalfHeight > 1f)
            {
                float minRequiredForPlayfield = playfieldBounds != null
                    ? (playfieldBounds.Size.x * 0.5f + horizontalMargin * 0.5f) / aspect
                    : minOrthoSize;
                targetSize = Mathf.Clamp(targetSize, minOrthoSize, Mathf.Max(bgHalfHeight, minRequiredForPlayfield));
            }
        }

        targetCamera.orthographicSize = Mathf.Clamp(targetSize, minOrthoSize, maxOrthoSize);

        if (Application.isPlaying && CameraShakeManager.Instance != null)
        {
            CameraShakeManager.Instance.SyncOriginalPosition();
        }
    }
}
