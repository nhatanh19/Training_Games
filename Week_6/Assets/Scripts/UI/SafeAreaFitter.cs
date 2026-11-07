using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    [Header("Design Anchors")]
    [SerializeField] private Vector2 _designAnchorMin = new Vector2(0f, 1f);
    [SerializeField] private Vector2 _designAnchorMax = new Vector2(1f, 1f);

    [Header("Safe Area Edges")]
    [SerializeField] private bool _applyTop = true;
    [SerializeField] private bool _applyBottom = true;
    [SerializeField] private bool _applyLeft = true;
    [SerializeField] private bool _applyRight = true;

    [Header("Editor Simulation")]
    [SerializeField] private bool _simulateInEditor = false;
    [SerializeField] private Rect _editorSimulatedSafeArea = new Rect(0f, 102f, 1080f, 1716f);

    private RectTransform _rectTransform;
    private Rect _lastSafeArea = Rect.zero;
    private Vector2Int _lastScreenSize = Vector2Int.zero;
    private ScreenOrientation _lastOrientation = ScreenOrientation.AutoRotation;

    public Vector2 DesignAnchorMin { get => _designAnchorMin; set { _designAnchorMin = value; Refresh(true); } }
    public Vector2 DesignAnchorMax { get => _designAnchorMax; set { _designAnchorMax = value; Refresh(true); } }
    public bool ApplyTop { get => _applyTop; set { _applyTop = value; Refresh(true); } }
    public bool ApplyBottom { get => _applyBottom; set { _applyBottom = value; Refresh(true); } }
    public bool ApplyLeft { get => _applyLeft; set { _applyLeft = value; Refresh(true); } }
    public bool ApplyRight { get => _applyRight; set { _applyRight = value; Refresh(true); } }
    public bool SimulateInEditor { get => _simulateInEditor; set { _simulateInEditor = value; Refresh(true); } }
    public Rect EditorSimulatedSafeArea { get => _editorSimulatedSafeArea; set { _editorSimulatedSafeArea = value; Refresh(true); } }

    private void Awake()
    {
        EnsureRect();
    }

    private void OnEnable()
    {
        EnsureRect();
        Refresh(true);
    }

    private void Update()
    {
        Refresh(false);
    }

    private void EnsureRect()
    {
        if (_rectTransform == null)
        {
            _rectTransform = GetComponent<RectTransform>();
        }
    }

    public void Refresh(bool force = false)
    {
        if (!Application.isPlaying) return;

        EnsureRect();
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

    public void RestoreDesignAnchors()
    {
        EnsureRect();
        if (_rectTransform == null) return;

        _rectTransform.anchorMin = _designAnchorMin;
        _rectTransform.anchorMax = _designAnchorMax;
    }

    public void ApplySafeArea(Rect safeArea, Vector2Int screenSize)
    {
        EnsureRect();
        if (_rectTransform == null) return;
        if (screenSize.x <= 0 || screenSize.y <= 0) return;

        Vector2 min = safeArea.position;
        Vector2 max = min + safeArea.size;

        float minXRatio = min.x / screenSize.x;
        float maxXRatio = max.x / screenSize.x;
        float minYRatio = min.y / screenSize.y;
        float maxYRatio = max.y / screenSize.y;

        Vector2 newAnchorMin = _designAnchorMin;
        Vector2 newAnchorMax = _designAnchorMax;

        bool isFullStretchY = Mathf.Approximately(_designAnchorMin.y, 0f) && Mathf.Approximately(_designAnchorMax.y, 1f);
        bool isTopAnchored = Mathf.Approximately(_designAnchorMax.y, 1f) && !isFullStretchY;

        if (_applyLeft)
        {
            newAnchorMin.x = minXRatio;
        }
        else
        {
            newAnchorMin.x = _designAnchorMin.x;
        }

        if (_applyRight)
        {
            newAnchorMax.x = maxXRatio;
        }
        else
        {
            newAnchorMax.x = _designAnchorMax.x;
        }

        if (isFullStretchY)
        {
            newAnchorMin.y = _applyBottom ? minYRatio : _designAnchorMin.y;
            newAnchorMax.y = _applyTop ? maxYRatio : _designAnchorMax.y;
        }
        else if (isTopAnchored)
        {

            if (_applyTop)
            {
                float topDelta = 1f - maxYRatio;
                newAnchorMax.y = 1f - topDelta;
                newAnchorMin.y = _designAnchorMin.y - topDelta;
            }
            else
            {
                newAnchorMax.y = _designAnchorMax.y;
                newAnchorMin.y = _designAnchorMin.y;
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
    private void PreviewSimulation(Rect simulatedSafeArea)
    {
        _simulateInEditor = true;
        _editorSimulatedSafeArea = simulatedSafeArea;

        EnsureRect();
        if (_rectTransform == null) return;

        RestoreDesignAnchors();
        ApplySafeArea(simulatedSafeArea, new Vector2Int(Screen.width, Screen.height));
    }

    [ContextMenu("Simulate iPhone 14 Pro")]
    public void SimulateIPhone14Pro()
    {
        int w = Screen.width > 0 ? Screen.width : 1080;
        int h = Screen.height > 0 ? Screen.height : 1920;
        PreviewSimulation(new Rect(0f, 102f, w, h - 250f));
    }

    [ContextMenu("Simulate iPhone X/11 Notch")]
    public void SimulateIPhoneX()
    {
        int w = Screen.width > 0 ? Screen.width : 1080;
        int h = Screen.height > 0 ? Screen.height : 1920;
        PreviewSimulation(new Rect(0f, 102f, w, h - 234f));
    }

    [ContextMenu("Simulate Standard 16:9")]
    public void SimulateStandard()
    {
        int w = Screen.width > 0 ? Screen.width : 1080;
        int h = Screen.height > 0 ? Screen.height : 1920;
        PreviewSimulation(new Rect(0f, 0f, w, h));
    }

    [ContextMenu("Disable Simulation")]
    public void DisableSimulation()
    {
        _simulateInEditor = false;
        RestoreDesignAnchors();
    }
#endif
}
