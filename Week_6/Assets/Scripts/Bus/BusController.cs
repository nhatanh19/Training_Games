using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BusController : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer arrowRenderer;
    private Vector3 _defaultScale;

    [Header("Bus Properties")]
    [SerializeField] private BusColor busColor;
    [SerializeField] private Direction direction;
    [Range(2, 3)]
    [SerializeField] private int length = 2;
    [SerializeField] private Vector2Int rootGridPosition;

    [Header("Seat Points (in boarding order)")]
    [SerializeField] private List<Transform> seatPoints = new List<Transform>();

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private float turnDuration = 0.15f;
    [SerializeField] private float departDuration = 1.0f;
    [SerializeField] private float departDelay = 0.4f;
    [SerializeField, Min(0f)] private float parkingApproachOffset = 1.4f;
    [SerializeField, Min(0f)] private float parkingSettleDuration = 0.12f;

    [Header("Blocked Feedback")]
    [SerializeField, Min(0f)] private float impactClearance = 0.04f;
    [SerializeField, Min(0.01f)] private float impactApproachSpeed = 8f;
    [SerializeField, Min(0.01f)] private float impactReturnDuration = 0.20f;

    private GridManager _gridManager;
    private BusStopSlot _assignedSlot;
    private int _passengersCount = 0;
    private bool _isBusy = false;
    private Sequence _movementSequence;
    private BusController _impactBlocker;
    private bool _reservedForImpact;
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
        _defaultScale = transform.localScale;
    }

    private void OnValidate()
    {
        ApplyColor(allowCreateArrow: false);
    }

    private void Start()
    {

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
                    arrowTr.localPosition = new Vector3(arrowX, 0f, 0f);
                    arrowTr.localScale = new Vector3(0.42f, 0.42f, 1f);
                    arrowRenderer = arrowGo.AddComponent<SpriteRenderer>();
                    arrowRenderer.sortingOrder = 15;
                }
            }
            else
            {
                arrowRenderer = arrowTr.GetComponent<SpriteRenderer>();
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
        return FindPathBlocker() == null;
    }

    private BusController FindPathBlocker()
    {
        GridManager grid = Grid;
        if (grid == null) return null;
        Vector2Int step = direction.ToVector2Int();
        Vector2Int checkPos = GetHeadCell() + step;

        while (grid.IsInsideGrid(checkPos))
        {
            BusController blocker = grid.GetPathBlocker(checkPos);
            if (blocker != null)
            {
                return blocker;
            }
            checkPos += step;
        }

        return null;
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
        if (_isBusy || _assignedSlot != null || _reservedForImpact) return;
        if (GameInputManager.IsLocked) return;
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
        if (UIManager.Instance != null && UIManager.Instance.IsAnyPopupOpen()) return;

        AudioManager.Instance?.PlayClick();
        HapticManager.Instance?.TriggerLight();

        BusController blocker = FindPathBlocker();
        if (blocker != null)
        {
            PlayBlockedFeedback(blocker);
            return;
        }

        if (Grid == null) return;
        if (BusStopManager.Instance == null || !BusStopManager.Instance.HasAvailableSlot)
        {
            HapticManager.Instance?.TriggerWarning();
            BusStopManager.Instance?.ShowFullFeedback();
            return;
        }

        DriveToBusStop();
    }

    private const float ParkedScaleFactor = 0.85f;

    private void DriveToBusStop()
    {
        _isBusy = true;
        IsReadyForPassengers = false;
        _assignedSlot = BusStopManager.Instance.GetAvailableSlot();
        _assignedSlot.AssignBus(this);

        GridManager grid = Grid;
        if (grid != null)
        {
            grid.UnregisterBus(GetOccupiedCells());
        }

        Vector3 targetPos = _assignedSlot.SlotTransform.position;

        Sequence seq = DOTween.Sequence();
        _movementSequence = seq;
        seq.Append(transform.DOPunchScale(new Vector3(0.12f, -0.08f, 0f), 0.12f, 3, 0.5f));
        seq.AppendCallback(() =>
        {
            VFXManager.Instance?.PlayDust(transform.position, transform.rotation);
            AudioManager.Instance?.PlayHonk();
        });
        seq.Append(BuildDriveSequence(targetPos));
        if (parkingSettleDuration > 0f)
        {
            Vector3 settlePunch = Vector3.Scale(_defaultScale, new Vector3(0.025f, -0.035f, 0f));
            seq.Append(transform.DOPunchScale(settlePunch, parkingSettleDuration, 2, 0.5f));
        }
        seq.OnComplete(() =>
        {
            _movementSequence = null;
            _isBusy = false;
            IsReadyForPassengers = true;
            _assignedSlot?.NotifyBusArrived(this);
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
        float approachY = targetPos.y - Mathf.Max(0f, parkingApproachOffset);

        float GetDuration(Vector3 from, Vector3 to)
        {
            float dist = Vector3.Distance(from, to);
            return Mathf.Max(dist / Mathf.Max(0.01f, moveSpeed), 0.05f);
        }

        void AppendMove(Vector3 from, Vector3 to, Ease ease = Ease.Linear)
        {
            seq.Append(transform.DOMove(to, GetDuration(from, to)).SetEase(ease));
        }

        void AppendTurnMove(Vector3 from, Vector3 to, float angle, Ease ease = Ease.Linear)
        {
            float duration = GetDuration(from, to);
            seq.Append(transform.DOMove(to, duration).SetEase(ease));
            seq.Join(transform.DORotate(new Vector3(0f, 0f, angle),
                Mathf.Min(duration, Mathf.Max(0.01f, turnDuration))).SetEase(Ease.InOutSine));
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

                AppendMove(currentPos, p1, Ease.InSine);
                AppendTurnMove(p1, p2, 90f);
                AppendMove(p2, p3);
                AppendTurnMove(p3, p4, 180f);
                AppendTurnMove(p4, targetPos, 90f, Ease.OutSine);
                break;
            }

            case Direction.Left:
            {

                float turnX = leftX + (length == 3 ? 0.65f : 0.35f);
                Vector3 p1 = new Vector3(turnX, currentPos.y, 0f);
                Vector3 p2 = new Vector3(leftX, currentPos.y + 0.35f, 0f);
                Vector3 p3 = new Vector3(leftX, approachY, 0f);
                Vector3 p4 = new Vector3(targetPos.x, approachY, 0f);

                AppendMove(currentPos, p1, Ease.InSine);
                AppendTurnMove(p1, p2, 90f);
                AppendMove(p2, p3);
                AppendTurnMove(p3, p4, 0f);
                AppendTurnMove(p4, targetPos, 90f, Ease.OutSine);
                break;
            }

            case Direction.Up:
            {
                if (Mathf.Abs(currentPos.x - targetPos.x) < 0.05f)
                {
                    AppendMove(currentPos, targetPos, Ease.InOutSine);
                }
                else
                {
                    Vector3 p1 = new Vector3(currentPos.x, approachY, 0f);
                    Vector3 p2 = new Vector3(targetPos.x, approachY, 0f);
                    float angleToBay = targetPos.x >= currentPos.x ? 0f : 180f;

                    AppendMove(currentPos, p1, Ease.InSine);
                    AppendTurnMove(p1, p2, angleToBay);
                    AppendTurnMove(p2, targetPos, 90f, Ease.OutSine);
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

                AppendMove(currentPos, p1, Ease.InSine);
                AppendTurnMove(p1, p2, goRight ? 0f : 180f);
                AppendTurnMove(p2, p3, 90f);
                AppendTurnMove(p3, p4, goRight ? 180f : 0f);
                AppendTurnMove(p4, targetPos, 90f, Ease.OutSine);
                break;
            }
        }

        seq.SetLink(gameObject);
        return seq;
    }

    public Transform GetNextSeatTransform()
    {
        if (!HasEmptySeat) return null;
        if (seatPoints == null || _passengersCount >= seatPoints.Count)
        {
            Debug.LogError($"[BusController] Not enough seat points on {name}; expected {Capacity}.", this);
            return null;
        }
        Transform seat = seatPoints[_passengersCount];
        if (seat == null || !seat.gameObject.activeInHierarchy)
        {
            Debug.LogError($"[BusController] Seat {_passengersCount} is missing or inactive on {name}.", this);
            return null;
        }
        _passengersCount++;
        return seat;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (seatPoints == null) return;

        for (int i = 0; i < seatPoints.Count && i < Capacity; i++)
        {
            Transform seat = seatPoints[i];
            if (seat == null) continue;
            Gizmos.color = seat.gameObject.activeInHierarchy ? UnityEngine.Color.cyan : UnityEngine.Color.red;
            Gizmos.DrawWireSphere(seat.position, 0.12f);
            UnityEditor.Handles.Label(seat.position + Vector3.up * 0.15f, $"Seat {i + 1}/{Capacity}");
        }
    }
#endif

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
        _movementSequence = seq;
        Vector3 departPunch = Vector3.Scale(_defaultScale, new Vector3(-0.04f, 0.06f, 0f));
        seq.Append(transform.DOPunchScale(departPunch, Mathf.Max(0.01f, departDelay), 2, 0.5f));
        seq.AppendCallback(() =>
        {
            CameraShakeManager.Instance?.ShakeLight();
            VFXManager.Instance?.PlayDust(transform.position, transform.rotation);
            AudioManager.Instance?.PlayEngine();
            HapticManager.Instance?.TriggerSuccess();
        });
        seq.Append(transform.DOMoveY(transform.position.y + 10f, departDuration).SetEase(Ease.InQuad));
        seq.OnComplete(() =>
        {
            _movementSequence = null;
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

    private void PlayBlockedFeedback(BusController blocker)
    {
        if (blocker == null || blocker._isBusy || blocker._reservedForImpact ||
            blocker._assignedSlot != null || Grid == null) return;

        Collider2D ownCollider = GetComponent<Collider2D>();
        Collider2D blockerCollider = blocker.GetComponent<Collider2D>();
        if (ownCollider == null || blockerCollider == null ||
            !ownCollider.enabled || !blockerCollider.enabled) return;
        if (!Grid.ReserveImpactPath(this, blocker)) return;

        blocker._reservedForImpact = true;
        _impactBlocker = blocker;
        _isBusy = true;
        Vector2Int step = direction.ToVector2Int();
        Vector3 moveDirection = new Vector3(step.x, step.y, 0f);
        Bounds ownBounds = ownCollider.bounds;
        Bounds blockerBounds = blockerCollider.bounds;
        float ownExtent = Mathf.Abs(step.x) * ownBounds.extents.x + Mathf.Abs(step.y) * ownBounds.extents.y;
        float blockerExtent = Mathf.Abs(step.x) * blockerBounds.extents.x + Mathf.Abs(step.y) * blockerBounds.extents.y;
        float gap = Vector3.Dot(blockerBounds.center - ownBounds.center, moveDirection) - ownExtent - blockerExtent;
        float distance = Mathf.Max(0f, gap - Mathf.Max(0f, impactClearance));
        Vector3 startPosition = transform.position;
        Vector3 impactPosition = startPosition + moveDirection * distance;
        Vector3 hitPoint = ownBounds.center + moveDirection * (ownExtent + distance);

        Sequence seq = DOTween.Sequence();
        _movementSequence = seq;
        if (distance > 0.001f)
        {
            seq.Append(transform.DOMove(impactPosition, distance / Mathf.Max(0.01f, impactApproachSpeed))
                .SetEase(Ease.OutQuad));
        }
        seq.AppendCallback(() =>
        {
            VFXManager.Instance?.PlayHitSpark(hitPoint);
            AudioManager.Instance?.PlayCrash();
            HapticManager.Instance?.TriggerWarning();
        });
        if (distance > 0.001f)
        {
            seq.Append(transform.DOMove(startPosition, Mathf.Max(0.01f, impactReturnDuration)).SetEase(Ease.OutQuad));
        }
        else
        {
            seq.Append(transform.DOPunchScale(new Vector3(-0.04f, 0.04f, 0f), 0.15f, 2, 0.5f));
        }
        seq.OnComplete(() =>
        {
            _movementSequence = null;
            _isBusy = false;
            ReleaseImpactReservation();
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
                GameManager.Instance.TriggerStatusCheck();
        });
    }

    private void ReleaseImpactReservation()
    {
        if (_impactBlocker != null) _impactBlocker._reservedForImpact = false;
        _impactBlocker = null;
        if (_gridManager != null) _gridManager.ReleaseImpactPath(this);
    }

    public void ResetBus()
    {
        _movementSequence?.Kill();
        _movementSequence = null;
        transform.DOKill();
        ReleaseImpactReservation();
        _reservedForImpact = false;
        _isBusy = false;
        IsReadyForPassengers = false;
        _passengersCount = 0;
        if (_assignedSlot != null)
        {
            _assignedSlot.ClearSlot();
            _assignedSlot = null;
        }
        transform.localScale = _defaultScale;
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
