using UnityEngine;

/// <summary>
/// Projects a world-space Transform anchor into UI canvas local coordinates without overriding sizeDelta.
/// </summary>
public class WorldAnchorUI : MonoBehaviour
{
    [SerializeField] private Transform worldTarget;
    [SerializeField] private RectTransform uiTarget;
    [SerializeField] private Vector2 screenOffset = Vector2.zero;
    [SerializeField] private Camera worldCamera;

    private Canvas _canvas;
    private RectTransform _parentRect;

    private void Awake()
    {
        if (uiTarget == null) uiTarget = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        if (uiTarget != null) _parentRect = uiTarget.parent as RectTransform;
    }

    private void LateUpdate()
    {
        UpdateAnchorPosition();
    }

    public void UpdateAnchorPosition()
    {
        if (worldTarget == null || uiTarget == null) return;
        if (worldCamera == null) worldCamera = Camera.main;
        if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
        if (_parentRect == null) _parentRect = uiTarget.parent as RectTransform;
        if (worldCamera == null || _canvas == null || _parentRect == null) return;

        Vector2 screenPoint = worldCamera.WorldToScreenPoint(worldTarget.position);
        screenPoint += screenOffset;
        Camera uiCam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRect, screenPoint, uiCam, out Vector2 localPoint))
        {
            uiTarget.anchoredPosition = localPoint;
        }
    }
}
