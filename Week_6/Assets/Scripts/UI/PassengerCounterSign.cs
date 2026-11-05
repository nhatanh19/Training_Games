using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// Overlays the remaining-passenger counter on the blue bus-stop sign that is baked into the
/// scene background. The sign position is derived from the background sprite bounds so it
/// follows any screen aspect ratio.
/// </summary>
public class PassengerCounterSign : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform _signRoot;
    [SerializeField] private TextMeshProUGUI _countText;
    [SerializeField] private TextMeshProUGUI _labelText;

    [Header("Sign placement inside background sprite (UV, origin bottom-left)")]
    [SerializeField] private Vector2 _signUv = new Vector2(0.1807f, 0.8345f);
    [SerializeField] private Vector2 _signUvSize = new Vector2(0.1625f, 0.0695f);

    private Canvas _canvas;
    private RectTransform _canvasRect;
    private SpriteRenderer _background;
    private Camera _camera;
    private int _lastCount = -1;

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas != null) _canvasRect = _canvas.transform as RectTransform;
    }

    private void OnEnable()
    {
        SettingsController.LanguageChanged += OnLanguageChanged;
        RefreshLabel();
    }

    private void OnDisable()
    {
        SettingsController.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(string language)
    {
        RefreshLabel();
    }

    private void RefreshLabel()
    {
        if (_labelText != null)
        {
            _labelText.text = SettingsController.IsVietnameseSaved ? "CÒN LẠI" : "LEFT";
        }
    }

    private void LateUpdate()
    {
        UpdateCount();
        UpdatePlacement();
    }

    private void UpdateCount()
    {
        PassengerQueueManager queue = PassengerQueueManager.Instance;
        int count = queue != null ? queue.RemainingPassengersCount : 0;
        if (count == _lastCount) return;

        bool animate = _lastCount >= 0;
        _lastCount = count;
        if (_countText == null) return;

        _countText.text = count.ToString();
        if (animate)
        {
            _countText.transform.DOKill();
            _countText.transform.localScale = Vector3.one;
            _countText.transform.DOPunchScale(Vector3.one * 0.25f, 0.2f, 2, 0.5f).SetLink(gameObject);
        }
    }

    private void UpdatePlacement()
    {
        if (_signRoot == null || _canvasRect == null) return;

        if (_camera == null) _camera = Camera.main;
        if (_background == null)
        {
            GameObject bg = GameObject.Find("SceneBackground");
            if (bg != null) _background = bg.GetComponent<SpriteRenderer>();
        }
        if (_camera == null || _background == null) return;

        Bounds b = _background.bounds;
        Vector3 center = new Vector3(
            b.min.x + b.size.x * _signUv.x,
            b.min.y + b.size.y * _signUv.y,
            b.center.z);
        Vector3 halfX = center + new Vector3(b.size.x * _signUvSize.x * 0.5f, 0f, 0f);
        Vector3 halfY = center + new Vector3(0f, b.size.y * _signUvSize.y * 0.5f, 0f);

        Vector2 sc = _camera.WorldToScreenPoint(center);
        Vector2 sx = _camera.WorldToScreenPoint(halfX);
        Vector2 sy = _camera.WorldToScreenPoint(halfY);

        Camera uiCam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        RectTransform parent = _signRoot.parent as RectTransform;
        if (parent == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, sc, uiCam, out Vector2 local))
        {
            _signRoot.anchoredPosition = local;
        }

        float scale = _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f;
        float width = Mathf.Abs(sx.x - sc.x) * 2f / scale;
        float height = Mathf.Abs(sy.y - sc.y) * 2f / scale;
        _signRoot.sizeDelta = new Vector2(width, height);
    }
}
