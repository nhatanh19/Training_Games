using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BusController : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer arrowRenderer;
    [SerializeField] private Vector3 defaultScale;

    [Header("Bus Properties")]
    [SerializeField] private BusColor busColor;
    [SerializeField] private Direction direction;
    [Range(2, 3)]
    [SerializeField] private int length = 2;
    [SerializeField] private Vector2Int rootGridPosition;

    [Header("Seats (Must be 3 points)")]
    [SerializeField] private List<Transform> seatPoints = new List<Transform>();

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private float turnDuration = 0.15f;
    [SerializeField] private float departDuration = 1.0f;
    [SerializeField] private float departDelay = 0.4f;

    private GridManager _gridManager;
    private BusStopSlot _assignedSlot;
    private int _passengersCount = 0;
    private bool _isBusy = false;
    private readonly List<PassengerController> _seatedPassengers = new List<PassengerController>();

    public BusColor Color => busColor;
    public Direction MoveDirection => direction;
    public int Length => length;
    public Vector2Int RootGridPosition => rootGridPosition;
    public List<Transform> SeatPoints => seatPoints;
    public int Capacity => length == 3 ? 3 : 2;
    public bool HasEmptySeat => _passengersCount < Capacity;
    public bool IsReadyForPassengers { get; private set; } = false;

    private GridManager Grid
    {
        get
        {
            if (_gridManager == null)
            {
                _gridManager = GridManager.Instance != null
                    ? GridManager.Instance
                    : FindObjectOfType<GridManager>();
            }
            return _gridManager;
        }
    }

    private void Awake()
    {
        UpdateScaleToGrid();
    }

    public void UpdateScaleToGrid()
    {
        GridManager grid = Grid;
        float cs = grid != null ? grid.CellSize : 0.95f;

        float scaleFactor = cs * 0.90f;
        defaultScale = new Vector3(scaleFactor, scaleFactor, 1f);
        transform.localScale = defaultScale;

        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.size = new Vector2(length, 1f);
            col.offset = Vector2.zero;
        }
    }

    private void EnsureDefaultScale()
    {
        UpdateScaleToGrid();
    }

    private void OnValidate()
    {
        EnsureDefaultScale();
        ApplyColor(allowCreateArrow: false);
    }

    private void Start()
    {

        UpdateScaleToGrid();
        ApplyColor(allowCreateArrow: true);
        AlignToGrid();
        RegisterToGrid();
    }

    public void Initialize(BusColor color, Direction dir, int len, Vector2Int rootPos)
    {
        busColor = color;
        direction = dir;
        length = len;
        rootGridPosition = rootPos;
        UpdateScaleToGrid();
        ApplyColor(allowCreateArrow: true);
        AlignToGrid();
        RegisterToGrid();
    }

    public void ApplyColor(bool allowCreateArrow = true)
    {
        if (bodyRenderer == null)
        {
            bodyRenderer = GetComponent<SpriteRenderer>();
            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        if (bodyRenderer != null)
        {
            bodyRenderer.sortingOrder = 10;
        }

        Transform fg = transform.Find("FrontGlass");
        if (fg != null) fg.gameObject.SetActive(false);

        for (int i = 0; i < seatPoints.Count; i++)
        {
            var seat = seatPoints[i];
            if (seat != null)
            {
                SpriteRenderer sr = seat.GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = false;
            }
        }

        float arrowX = length == 3 ? 0.95f : 0.55f;
        if (arrowRenderer == null)
        {
            Transform arrowTr = transform.Find("DirectionArrow");
            if (arrowTr == null)
            {

                if (allowCreateArrow && Application.isPlaying)
                {
                    GameObject arrowGo = new GameObject("DirectionArrow");
                    arrowTr = arrowGo.transform;
                    arrowTr.SetParent(transform, false);
                    arrowRenderer = arrowGo.AddComponent<SpriteRenderer>();
                    arrowRenderer.sortingOrder = 15;
                }
            }
            else
            {
                arrowRenderer = arrowTr.GetComponent<SpriteRenderer>();
            }

            if (arrowTr != null)
            {
                arrowTr.localPosition = new Vector3(arrowX, 0f, 0f);
                arrowTr.localRotation = Quaternion.identity;
                arrowTr.localScale = new Vector3(0.42f, 0.42f, 1f);
            }
        }
        else
        {
            arrowRenderer.transform.localPosition = new Vector3(arrowX, 0f, 0f);
            arrowRenderer.transform.localScale = new Vector3(0.42f, 0.42f, 1f);
        }

        if (seatPoints != null && seatPoints.Count > 0)
        {
            if (length == 3)
            {
                if (seatPoints.Count > 0 && seatPoints[0] != null) seatPoints[0].localPosition = new Vector3(-0.75f, 0f, 0f);
                if (seatPoints.Count > 1 && seatPoints[1] != null) seatPoints[1].localPosition = new Vector3(-0.15f, 0f, 0f);
                if (seatPoints.Count > 2 && seatPoints[2] != null)
                {
                    seatPoints[2].gameObject.SetActive(true);
                    seatPoints[2].localPosition = new Vector3(0.45f, 0f, 0f);
                }
            }
            else
            {
                if (seatPoints.Count > 0 && seatPoints[0] != null) seatPoints[0].localPosition = new Vector3(-0.35f, 0f, 0f);
                if (seatPoints.Count > 1 && seatPoints[1] != null) seatPoints[1].localPosition = new Vector3(0.20f, 0f, 0f);
                if (seatPoints.Count > 2 && seatPoints[2] != null)
                {
                    seatPoints[2].gameObject.SetActive(false);
                }
            }
        }

        string vehicleType = length == 3 ? "bus" : "car";
        string colorName = busColor switch
        {
            BusColor.Red => "red",
            BusColor.Blue => "blue",
            BusColor.Green => "green",
            BusColor.Yellow => "yellow",
            _ => "blue"
        };

        Sprite vSprite = Resources.Load<Sprite>($"Vehicles/{vehicleType}_seat/{vehicleType}_seat_{colorName}_right");
        if (vSprite != null && bodyRenderer != null)
        {
            bodyRenderer.sprite = vSprite;
            bodyRenderer.color = UnityEngine.Color.white;
        }
        else if (bodyRenderer != null)
        {
            bodyRenderer.color = GetVisualColor(busColor);
        }

        Sprite aSprite = Resources.Load<Sprite>($"Vehicles/{vehicleType}_arrow/{vehicleType}_arrow_{colorName}_right");
        if (aSprite != null && arrowRenderer != null)
        {
            arrowRenderer.sprite = aSprite;
            arrowRenderer.color = UnityEngine.Color.white;
        }
    }

    private UnityEngine.Color GetVisualColor(BusColor color)
    {
        return color switch
        {
            BusColor.Red => new UnityEngine.Color(0.9f, 0.25f, 0.25f),
            BusColor.Blue => new UnityEngine.Color(0.2f, 0.5f, 0.95f),
            BusColor.Green => new UnityEngine.Color(0.25f, 0.85f, 0.35f),
            BusColor.Yellow => new UnityEngine.Color(0.95f, 0.85f, 0.15f),
            _ => UnityEngine.Color.white
        };
    }

    public List<Vector2Int> GetOccupiedCells()
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        Vector2Int step = direction.ToVector2Int();

        for (int i = 0; i < length; i++)
        {
            cells.Add(rootGridPosition + step * i);
        }

        return cells;
    }

    public Vector2Int GetHeadCell()
    {
        return rootGridPosition + direction.ToVector2Int() * (length - 1);
    }

    public void AlignToGrid()
    {
        GridManager grid = Grid;
        if (grid == null) return;

        UpdateScaleToGrid();

        Vector2Int tailPos = rootGridPosition;
        Vector2Int headPos = GetHeadCell();

        Vector3 tailWorld = grid.GridToWorldPosition(tailPos);
        Vector3 headWorld = grid.GridToWorldPosition(headPos);

        transform.position = (tailWorld + headWorld) * 0.5f;

        float angle = direction switch
        {
            Direction.Right => 0f,
            Direction.Up => 90f,
            Direction.Left => 180f,
            Direction.Down => 270f,
            _ => 0f
        };

        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void RegisterToGrid()
    {
        GridManager grid = Grid;
        if (grid != null)
        {
            grid.RegisterBus(this, GetOccupiedCells());
        }
    }

    public bool CanExit()
    {
        GridManager grid = Grid;
        if (grid == null) return false;

        Vector2Int step = direction.ToVector2Int();
        Vector2Int checkPos = GetHeadCell() + step;

        while (grid.IsInsideGrid(checkPos))
        {
            if (grid.IsCellOccupied(checkPos))
            {
                return false;
            }
            checkPos += step;
        }

        return true;
    }

    private void OnMouseDown()
    {
        if (GameInputManager.IsLocked) return;
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
        if (UIManager.Instance != null && UIManager.Instance.IsAnyPopupOpen()) return;
        if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        OnClicked();
    }

    public void OnClicked()
    {
        if (_isBusy || _assignedSlot != null) return;
        if (GameInputManager.IsLocked) return;
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
        if (UIManager.Instance != null && UIManager.Instance.IsAnyPopupOpen()) return;

        AudioManager.Instance?.PlayClick();
        HapticManager.Instance?.TriggerLight();

        if (!CanExit())
        {
            PlayBlockedFeedback();
            return;
        }

        if (BusStopManager.Instance == null || !BusStopManager.Instance.HasAvailableSlot)
        {
            PlayBlockedFeedback();
            Debug.LogWarning("Bến đón đã đầy hoặc chưa có BusStopManager!");
            return;
        }

        DriveToBusStop();
    }

    private const float ParkedScaleFactor = 0.85f;

    private void DriveToBusStop()
    {
        _isBusy = true;
        IsReadyForPassengers = false;
        GameInputManager.SetLock(true);
        _assignedSlot = BusStopManager.Instance.GetAvailableSlot();
        _assignedSlot.AssignBus(this);

        GridManager grid = Grid;
        if (grid != null)
        {
            grid.UnregisterBus(GetOccupiedCells());
        }

        Vector3 targetPos = _assignedSlot.SlotTransform.position;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOPunchScale(new Vector3(0.12f, -0.08f, 0f), 0.12f, 3, 0.5f));
        seq.AppendCallback(() =>
        {
            VFXManager.Instance?.PlayDust(transform.position, transform.rotation);
            AudioManager.Instance?.PlayHonk();
        });
        seq.Append(BuildDriveSequence(targetPos));
        seq.OnComplete(() =>
        {
            _isBusy = false;
            IsReadyForPassengers = true;
            GameInputManager.SetLock(false);
            HapticManager.Instance?.TriggerSuccess();
            BusStopManager.Instance.NotifyBusArrived(this);
        });
    }

    private Sequence BuildDriveSequence(Vector3 targetPos)
    {
        Sequence seq = DOTween.Sequence();
        Vector3 currentPos = transform.position;

        GridManager grid = Grid;
        float rightX = grid != null ? grid.RightLaneX : 3.10f;
        float leftX = grid != null ? grid.LeftLaneX : -3.10f;
        float bottomY = grid != null ? grid.BottomLaneY : -4.30f;
        float approachY = targetPos.y - 1.4f;

        float GetDuration(Vector3 from, Vector3 to)
        {
            float dist = Vector3.Distance(from, to);
            return Mathf.Max(dist / moveSpeed, 0.05f);
        }

        switch (direction)
        {
            case Direction.Right:
            {

                float turnX = rightX - (length == 3 ? 0.65f : 0.35f);
                Vector3 p1 = new Vector3(turnX, currentPos.y, 0f);
                Vector3 p2 = new Vector3(rightX, currentPos.y + 0.35f, 0f);
                Vector3 p3 = new Vector3(rightX, approachY, 0f);
                Vector3 p4 = new Vector3(targetPos.x, approachY, 0f);

                seq.Append(transform.DOMove(p1, GetDuration(currentPos, p1)).SetEase(Ease.Linear));
                seq.Append(transform.DORotate(new Vector3(0, 0, 90f), turnDuration));
                seq.Append(transform.DOMove(p2, GetDuration(p1, p2)).SetEase(Ease.Linear));
                seq.Append(transform.DOMove(p3, GetDuration(p2, p3)).SetEase(Ease.Linear));
                seq.Append(transform.DORotate(new Vector3(0, 0, 180f), turnDuration));
                seq.Append(transform.DOMove(p4, GetDuration(p3, p4)).SetEase(Ease.Linear));
                seq.Append(transform.DORotate(new Vector3(0, 0, 90f), turnDuration));
                seq.Append(transform.DOMove(targetPos, GetDuration(p4, targetPos)).SetEase(Ease.Linear));
                break;
            }

            case Direction.Left:
            {

                float turnX = leftX + (length == 3 ? 0.65f : 0.35f);
                Vector3 p1 = new Vector3(turnX, currentPos.y, 0f);
                Vector3 p2 = new Vector3(leftX, currentPos.y + 0.35f, 0f);
                Vector3 p3 = new Vector3(leftX, approachY, 0f);
                Vector3 p4 = new Vector3(targetPos.x, approachY, 0f);

                seq.Append(transform.DOMove(p1, GetDuration(currentPos, p1)).SetEase(Ease.Linear));
                seq.Append(transform.DORotate(new Vector3(0, 0, 90f), turnDuration));
                seq.Append(transform.DOMove(p2, GetDuration(p1, p2)).SetEase(Ease.Linear));
                seq.Append(transform.DOMove(p3, GetDuration(p2, p3)).SetEase(Ease.Linear));
                seq.Append(transform.DORotate(Vector3.zero, turnDuration));
                seq.Append(transform.DOMove(p4, GetDuration(p3, p4)).SetEase(Ease.Linear));
                seq.Append(transform.DORotate(new Vector3(0, 0, 90f), turnDuration));
                seq.Append(transform.DOMove(targetPos, GetDuration(p4, targetPos)).SetEase(Ease.Linear));
                break;
            }

            case Direction.Up:
            {
                if (Mathf.Abs(currentPos.x - targetPos.x) < 0.05f)
                {
                    seq.Append(transform.DOMove(targetPos, GetDuration(currentPos, targetPos)).SetEase(Ease.Linear));
                }
                else
                {
                    Vector3 p1 = new Vector3(currentPos.x, approachY, 0f);
                    Vector3 p2 = new Vector3(targetPos.x, approachY, 0f);
                    float angleToBay = targetPos.x >= currentPos.x ? 0f : 180f;

                    seq.Append(transform.DOMove(p1, GetDuration(currentPos, p1)).SetEase(Ease.Linear));
                    seq.Append(transform.DORotate(new Vector3(0, 0, angleToBay), turnDuration));
                    seq.Append(transform.DOMove(p2, GetDuration(p1, p2)).SetEase(Ease.Linear));
                    seq.Append(transform.DORotate(new Vector3(0, 0, 90f), turnDuration));
                    seq.Append(transform.DOMove(targetPos, GetDuration(p2, targetPos)).SetEase(Ease.Linear));
                }
                break;
            }

            case Direction.Down:
            {
                float turnY = bottomY + (length == 3 ? 0.65f : 0.35f);
                Vector3 p1 = new Vector3(currentPos.x, turnY, 0f);
                bool goRight = grid != null ? currentPos.x >= grid.transform.position.x : currentPos.x >= 0f;
                float sideX = goRight ? rightX : leftX;

                Vector3 p2 = new Vector3(sideX, bottomY, 0f);
                Vector3 p3 = new Vector3(sideX, approachY, 0f);
                Vector3 p4 = new Vector3(targetPos.x, approachY, 0f);

                seq.Append(transform.DOMove(p1, GetDuration(currentPos, p1)).SetEase(Ease.Linear));
                seq.Append(transform.DORotate(new Vector3(0, 0, goRight ? 0f : 180f), turnDuration));
                seq.Append(transform.DOMove(p2, GetDuration(p1, p2)).SetEase(Ease.Linear));
                seq.Append(transform.DORotate(new Vector3(0, 0, 90f), turnDuration));
                seq.Append(transform.DOMove(p3, GetDuration(p2, p3)).SetEase(Ease.Linear));
                seq.Append(transform.DORotate(new Vector3(0, 0, goRight ? 180f : 0f), turnDuration));
                seq.Append(transform.DOMove(p4, GetDuration(p3, p4)).SetEase(Ease.Linear));
                seq.Append(transform.DORotate(new Vector3(0, 0, 90f), turnDuration));
                seq.Append(transform.DOMove(targetPos, GetDuration(p4, targetPos)).SetEase(Ease.Linear));
                break;
            }
        }

        seq.SetLink(gameObject);
        return seq;
    }

    public Transform GetNextSeatTransform()
    {
        if (!HasEmptySeat || _passengersCount >= Capacity || _passengersCount >= seatPoints.Count) return null;
        Transform seat = seatPoints[_passengersCount];
        _passengersCount++;
        return seat;
    }

    public void OnPassengerSeated(PassengerController passenger)
    {
        if (passenger != null)
        {
            _seatedPassengers.Add(passenger);
        }

        if (_passengersCount >= Capacity)
        {
            DepartFromStop();
        }
    }

    private void DepartFromStop()
    {
        _isBusy = true;
        IsReadyForPassengers = false;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOPunchScale(new Vector3(-0.12f, 0.18f, 0f), departDelay, 4, 0.5f));
        seq.AppendCallback(() =>
        {
            CameraShakeManager.Instance?.ShakeHeavy();
            VFXManager.Instance?.PlayDust(transform.position, transform.rotation);
            AudioManager.Instance?.PlayEngine();
            HapticManager.Instance?.TriggerSuccess();
        });
        seq.Append(transform.DOMoveY(transform.position.y + 10f, departDuration).SetEase(Ease.InQuad));
        seq.OnComplete(() =>
        {
            if (_assignedSlot != null)
            {
                _assignedSlot.ClearSlot();
                _assignedSlot = null;
            }

            foreach (var p in _seatedPassengers)
            {
                if (p != null)
                {
                    GamePoolManager.Instance.ReleasePassenger(p);
                }
            }
            _seatedPassengers.Clear();

            GamePoolManager.Instance.ReleaseBus(this);

            BusStopManager.Instance?.NotifyBusDeparted();
            LevelLoader.Instance?.RemoveSpawnedBus(this);

            if (PassengerQueueManager.Instance != null)
            {
                PassengerQueueManager.Instance.TryBoardNextPassenger();
            }
        });
    }

    private void PlayBlockedFeedback()
    {
        _isBusy = true;
        CameraShakeManager.Instance?.ShakeLight();
        AudioManager.Instance?.PlayCrash();
        HapticManager.Instance?.TriggerWarning();

        Vector2Int step = direction.ToVector2Int();
        Vector3 hitPoint = transform.position + new Vector3(step.x, step.y, 0f) * (length * 0.5f);
        VFXManager.Instance?.PlayHitSpark(hitPoint);

        Vector3 punchVector = new Vector3(step.x, step.y, 0f) * 0.2f;

        transform.DOPunchPosition(punchVector, 0.25f, 8, 0.5f).OnComplete(() =>
        {
            _isBusy = false;
        });
    }

    public void ResetBus()
    {
        _isBusy = false;
        IsReadyForPassengers = false;
        _passengersCount = 0;
        if (_assignedSlot != null)
        {
            _assignedSlot.ClearSlot();
            _assignedSlot = null;
        }
        transform.DOKill();
        EnsureDefaultScale();
        transform.localScale = defaultScale;
        transform.rotation = Quaternion.identity;

        foreach (var p in _seatedPassengers)
        {
            if (p != null)
            {
                GamePoolManager.Instance.ReleasePassenger(p);
            }
        }
        _seatedPassengers.Clear();

        foreach (var seat in seatPoints)
        {
            if (seat != null)
            {
                for (int i = seat.childCount - 1; i >= 0; i--)
                {
                    Transform child = seat.GetChild(i);
                    PassengerController p = child.GetComponent<PassengerController>();
                    if (p != null)
                    {
                        GamePoolManager.Instance.ReleasePassenger(p);
                    }
                }
            }
        }
    }
}
