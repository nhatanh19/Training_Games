using UnityEngine;
using UnityEngine.EventSystems;

public class GameInputManager : MonoBehaviour
{
    public static GameInputManager Instance { get; private set; }
    public static bool IsLocked { get; private set; }

    [SerializeField] private LayerMask busLayerMask = ~0;

    private Camera _mainCamera;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        _mainCamera = Camera.main;
    }

    public static void SetLock(bool locked)
    {
        IsLocked = locked;
    }

    private void Update()
    {
        if (IsLocked) return;

        bool hasInput = false;
        Vector2 screenPosition = Vector2.zero;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                hasInput = true;
                screenPosition = touch.position;
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            hasInput = true;
            screenPosition = Input.mousePosition;
        }

        if (!hasInput) return;

        if (IsPointerOverUI()) return;

        ProcessClick(screenPosition);
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        if (Input.touchCount > 0)
        {
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        }

        return EventSystem.current.IsPointerOverGameObject();
    }

    private void ProcessClick(Vector2 screenPosition)
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null) return;
        }

        Vector2 worldPoint = _mainCamera.ScreenToWorldPoint(screenPosition);
        RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero, Mathf.Infinity, busLayerMask);

        if (hit.collider != null)
        {
            BusController bus = hit.collider.GetComponentInParent<BusController>();
            if (bus != null)
            {
                bus.OnClicked();
            }
        }
    }
}
