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

        // Block input when game is not playing (e.g. Loading, GameWin, GameOver)
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
        {
            return;
        }

        // Block input if any modal popup is open
        if (UIManager.Instance != null && UIManager.Instance.IsAnyPopupOpen())
        {
            return;
        }

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

        if (IsPointerOverUI(screenPosition)) return;

        ProcessClick(screenPosition);
    }

    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null) return false;

        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                    return true;
            }
        }

        if (EventSystem.current.IsPointerOverGameObject())
            return true;

        // Fallback UI raycast
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
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
