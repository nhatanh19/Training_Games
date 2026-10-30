using UnityEngine;

/// <summary>
/// Adjusts the RectTransform anchors to conform to Screen.safeArea (Notch, Dynamic Island, Home Bar).
/// Conforms to Week 6 Task 2 specification.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[ExecuteAlways]
public class SafeAreaFitter : MonoBehaviour
{
    [Header("Safe Area Edges")]
    [SerializeField] private bool _applyTop = true;
    [SerializeField] private bool _applyBottom = true;
    [SerializeField] private bool _applyLeft = true;
    [SerializeField] private bool _applyRight = true;

    [Header("Editor Simulation")]
    [SerializeField] private bool _simulateInEditor = false;
    [SerializeField] private Rect _editorSimulatedSafeArea = new Rect(0f, 102f, 1080f, 1716f); // iPhone 14/15 Pro notch & home bar

    private RectTransform _rectTransform;
    private Rect _lastSafeArea = Rect.zero;
    private Vector2Int _lastScreenSize = Vector2Int.zero;
    private ScreenOrientation _lastOrientation = ScreenOrientation.AutoRotation;

    private Vector2 _initialAnchorMin = Vector2.zero;
    private Vector2 _initialAnchorMax = Vector2.one;
    private bool _isInitialized;

    public bool ApplyTop { get => _applyTop; set { _applyTop = value; Refresh(true); } }
    public bool ApplyBottom { get => _applyBottom; set { _applyBottom = value; Refresh(true); } }
    public bool ApplyLeft { get => _applyLeft; set { _applyLeft = value; Refresh(true); } }
    public bool ApplyRight { get => _applyRight; set { _applyRight = value; Refresh(true); } }
    public bool SimulateInEditor { get => _simulateInEditor; set { _simulateInEditor = value; Refresh(true); } }
    public Rect EditorSimulatedSafeArea { get => _editorSimulatedSafeArea; set { _editorSimulatedSafeArea = value; Refresh(true); } }

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnEnable()
    {
        EnsureInitialized();
        Refresh(true);
    }

    private void Update()
    {
        Refresh(false);
    }

    private void OnRectTransformDimensionsChange()
    {
        Refresh(true);
    }

    private void EnsureInitialized()
    {
        if (_rectTransform == null)
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        if (!_isInitialized && _rectTransform != null)
        {
            _initialAnchorMin = _rectTransform.anchorMin;
            _initialAnchorMax = _rectTransform.anchorMax;
            _isInitialized = true;
        }
    }

    public void Refresh(bool force = false)
    {
        EnsureInitialized();
        if (_rectTransform == null) return;

        Rect currentSafeArea = GetCurrentSafeArea();
        Vector2Int currentScreenSize = new Vector2Int(Screen.width, Screen.height);
        ScreenOrientation currentOrientation = Screen.orientation;

        if (force || currentSafeArea != _lastSafeArea || currentScreenSize != _lastScreenSize || currentOrientation != _lastOrientation)
        {
            _lastSafeArea = currentSafeArea;
            _lastScreenSize = currentScreenSize;
            _lastOrientation = currentOrientation;
            ApplySafeArea(currentSafeArea, currentScreenSize);
        }
    }

    public Rect GetCurrentSafeArea()
    {
#if UNITY_EDITOR
        if (_simulateInEditor)
        {
            return _editorSimulatedSafeArea;
        }
#endif
        return Screen.safeArea;
    }

    public void ApplySafeArea(Rect safeArea, Vector2Int screenSize)
    {
        if (screenSize.x <= 0 || screenSize.y <= 0) return;

        Vector2 min = safeArea.position;
        Vector2 max = min + safeArea.size;

        float minXRatio = min.x / screenSize.x;
        float maxXRatio = max.x / screenSize.x;
        float minYRatio = min.y / screenSize.y;
        float maxYRatio = max.y / screenSize.y;

        Vector2 newAnchorMin = _rectTransform.anchorMin;
        Vector2 newAnchorMax = _rectTransform.anchorMax;

        bool isFullStretchY = Mathf.Approximately(_initialAnchorMin.y, 0f) && Mathf.Approximately(_initialAnchorMax.y, 1f);
        bool isTopAnchored = Mathf.Approximately(_initialAnchorMax.y, 1f) && !isFullStretchY;

        if (_applyLeft)
        {
            newAnchorMin.x = minXRatio;
        }
        else
        {
            newAnchorMin.x = _initialAnchorMin.x;
        }

        if (_applyRight)
        {
            newAnchorMax.x = maxXRatio;
        }
        else
        {
            newAnchorMax.x = _initialAnchorMax.x;
        }

        if (isFullStretchY)
        {
            newAnchorMin.y = _applyBottom ? minYRatio : _initialAnchorMin.y;
            newAnchorMax.y = _applyTop ? maxYRatio : _initialAnchorMax.y;
        }
        else if (isTopAnchored)
        {
            // For a top-pinned bar, anchorMax.y follows the safeArea top edge
            if (_applyTop)
            {
                float topDelta = 1f - maxYRatio;
                newAnchorMax.y = 1f - topDelta;
                newAnchorMin.y = _initialAnchorMin.y - topDelta;
            }
            else
            {
                newAnchorMax.y = _initialAnchorMax.y;
                newAnchorMin.y = _initialAnchorMin.y;
            }
        }
        else
        {
            if (_applyBottom) newAnchorMin.y = minYRatio;
            if (_applyTop) newAnchorMax.y = maxYRatio;
        }

        _rectTransform.anchorMin = newAnchorMin;
        _rectTransform.anchorMax = newAnchorMax;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureInitialized();
        Refresh(true);
    }

    [ContextMenu("Simulate iPhone 14 Pro")]
    public void SimulateIPhone14Pro()
    {
        _simulateInEditor = true;
        int w = Screen.width > 0 ? Screen.width : 1080;
        int h = Screen.height > 0 ? Screen.height : 1920;
        _editorSimulatedSafeArea = new Rect(0f, 102f, w, h - 250f);
        Refresh(true);
    }

    [ContextMenu("Simulate iPhone X/11 Notch")]
    public void SimulateIPhoneX()
    {
        _simulateInEditor = true;
        int w = Screen.width > 0 ? Screen.width : 1080;
        int h = Screen.height > 0 ? Screen.height : 1920;
        _editorSimulatedSafeArea = new Rect(0f, 102f, w, h - 234f);
        Refresh(true);
    }

    [ContextMenu("Simulate Standard 16:9")]
    public void SimulateStandard()
    {
        _simulateInEditor = true;
        int w = Screen.width > 0 ? Screen.width : 1080;
        int h = Screen.height > 0 ? Screen.height : 1920;
        _editorSimulatedSafeArea = new Rect(0f, 0f, w, h);
        Refresh(true);
    }

    [ContextMenu("Disable Simulation")]
    public void DisableSimulation()
    {
        _simulateInEditor = false;
        Refresh(true);
    }
#endif
}
