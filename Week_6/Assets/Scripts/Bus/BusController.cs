using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BusController : MonoBehaviour
{
    [Header("Visual References (Authored on Prefab)")]
    [SerializeField] private GameVisualThemeSO visualTheme;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer arrowRenderer;
    [SerializeField] private BoxCollider2D busCollider;

    [Header("Arrow Offsets (Side View)")]
    [SerializeField] private Vector3 arrowRightLocalPos = Vector3.zero;
    [SerializeField] private Vector3 arrowLeftLocalPos = Vector3.zero;

    [Header("Bus Properties")]
    [SerializeField] private BusColor busColor;
    [SerializeField] private Direction direction;
    [Range(2, 3)]
    [SerializeField] private int length = 2;
    [SerializeField] private Vector2Int rootGridPosition;

    [Header("Seat Points (Authored on Prefab)")]
    [SerializeField] private List<Transform> seatPoints = new List<Transform>();
    [SerializeField] private List<Transform> parkedSeatPoints = new List<Transform>();

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 13f;
    [SerializeField] private float turnDuration = 0.14f;
    [SerializeField] private float departPulloutDuration = 0.18f;
    [SerializeField] private float departDuration = 0.65f;
    [SerializeField] private float departDelay = 0.22f;
    [SerializeField, Min(0f)] private float parkingApproachOffset = 1.25f;
    [SerializeField, Min(0f)] private float parkingSettleDuration = 0.12f;

    [Header("Blocked Feedback")]
    [SerializeField, Min(0f)] private float impactClearance = 0.04f;
    [SerializeField, Min(0.01f)] private float impactApproachSpeed = 8f;
    [SerializeField, Min(0.01f)] private float impactReturnDuration = 0.20f;

    private Vector3 _prefabBaseScale;
    private Vector3 _gridScale;
    private GridManager _gridManager;
    private BusStopSlot _assignedSlot;
    private int _reservedSeatsCount;
    private int _seatedCount;
    private bool _isBusy;
    private bool _isParked;
    private bool _hasCapturedBaseScale;
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
    public bool HasEmptySeat => _reservedSeatsCount < Capacity;
    public bool IsReadyForPassengers { get; private set; }
    public bool IsParkedOrInTransit => _assignedSlot != null || _isBusy;

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
        CaptureBaseScale();
    }

    private void CaptureBaseScale()
    {
        if (_hasCapturedBaseScale) return;
        _prefabBaseScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
        _gridScale = _prefabBaseScale;
        _hasCapturedBaseScale = true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyGridVisuals();
    }
#endif

    public void Initialize(BusColor color, Direction dir, int len, Vector2Int rootPos, float gridScaleFactor = 1f, GameVisualThemeSO themeOverride = null)
    {
        CaptureBaseScale();
        if (themeOverride != null) visualTheme = themeOverride;

        busColor = color;
        direction = dir;
        length = Mathf.Clamp(len, 2, 3);
        rootGridPosition = rootPos;
        _gridScale = _prefabBaseScale * Mathf.Max(0.1f, gridScaleFactor);
        transform.localScale = _gridScale;
        _isParked = false;
        IsReadyForPassengers = false;
        _reservedSeatsCount = 0;
        _seatedCount = 0;

        AlignToGrid();
        ApplyGridVisuals();
        RegisterToGrid();
    }

    public void SetColorVisual(BusColor newColor)
    {
        busColor = newColor;
        ApplyGridVisuals();
        transform.DOKill(true);
        transform.localScale = _gridScale;
        transform.DOPunchScale(_gridScale * 0.14f, 0.24f, 2, 0.5f).SetLink(gameObject);
    }

    public void ApplyGridVisuals()
    {
        if (bodyRenderer == null || visualTheme == null) return;

        Sprite gridSprite = visualTheme.GetGridSprite(busColor, length, direction);
        if (gridSprite != null)
        {
            bodyRenderer.sprite = gridSprite;
            bodyRenderer.color = UnityEngine.Color.white;
        }

        bool facingLeft = direction == Direction.Left;
        bodyRenderer.flipX = facingLeft;
        bodyRenderer.flipY = false;

        int depthOrder = 20 - Mathf.RoundToInt(transform.position.y * 2f);
        bodyRenderer.sortingOrder = Mathf.Clamp(depthOrder, 5, 35);

        if (arrowRenderer != null)
        {
            if (visualTheme.DirectionArrowSprite != null)
            {
                arrowRenderer.sprite = visualTheme.DirectionArrowSprite;
            }
            arrowRenderer.gameObject.SetActive(!_isParked);
            arrowRenderer.flipX = facingLeft;
            arrowRenderer.transform.localPosition = facingLeft ? arrowLeftLocalPos : arrowRightLocalPos;
            arrowRenderer.transform.localRotation = Quaternion.identity;
            arrowRenderer.sortingOrder = bodyRenderer.sortingOrder + 1;
        }
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
            Direction.Up => 90f,
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

    public void OnClicked()
    {
        if (_isBusy || _assignedSlot != null || _reservedForImpact || _isParked) return;
        if (GameInputManager.IsLocked) return;
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
        if (UIManager.Instance != null && UIManager.Instance.IsAnyPopupOpen()) return;

        if (BoosterManager.Instance != null && BoosterManager.Instance.IsVipTargeting)
        {
            BoosterManager.Instance.ConfirmVipTarget(this);
            return;
        }

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

        DriveToBusStop(false);
    }

    public bool SendViaVipBooster()
    {
        if (_isBusy || _assignedSlot != null || _reservedForImpact || _isParked) return false;
        if (BusStopManager.Instance == null || !BusStopManager.Instance.HasAvailableSlot)
        {
            BusStopManager.Instance?.ShowFullFeedback();
            return false;
        }

        DriveToBusStop(true);
        return true;
    }

    private void DriveToBusStop(bool ignoreBlockers)
    {
        _isBusy = true;
        IsReadyForPassengers = false;
        _assignedSlot = BusStopManager.Instance.GetAvailableSlot();
        _assignedSlot.AssignBus(this);
        BusStopManager.Instance.UpdateNearFullWarning();

        if (busCollider != null) busCollider.enabled = false;
        if (arrowRenderer != null) arrowRenderer.gameObject.SetActive(false);

        GridManager grid = Grid;
        if (grid != null)
        {
            grid.UnregisterBus(GetOccupiedCells());
        }

        Vector3 targetPos = _assignedSlot.ParkedVehicleAnchor.position;
        Vector3 parkedScale = _prefabBaseScale * _assignedSlot.ParkedVehicleScale;
        if (bodyRenderer != null) bodyRenderer.sortingOrder = 40;

        Sequence seq = DOTween.Sequence();
        _movementSequence = seq;
        seq.Append(transform.DOPunchScale(_gridScale * 0.10f, 0.10f, 2, 0.5f));
        seq.AppendCallback(() =>
        {
            VFXManager.Instance?.PlayDust(transform.position, Quaternion.identity);
            AudioManager.Instance?.PlayHonk();
        });

        Sequence driveSeq = ignoreBlockers
            ? BuildVipArcSequence(targetPos)
            : BuildDriveSequence(targetPos);

        driveSeq.Join(transform.DOScale(parkedScale, Mathf.Max(0.2f, driveSeq.Duration() * 0.75f)).SetEase(Ease.InOutSine));
        seq.Append(driveSeq);

        seq.AppendCallback(() =>
        {
            ApplyParkedVisuals();
        });

        if (parkingSettleDuration > 0f)
        {
            Vector3 settlePunch = new Vector3(parkedScale.x * 0.04f, -parkedScale.y * 0.06f, 0f);
            seq.Append(transform.DOPunchScale(settlePunch, parkingSettleDuration, 2, 0.5f));
        }

        seq.OnComplete(() =>
        {
            _movementSequence = null;
            _isBusy = false;
            _isParked = true;
            IsReadyForPassengers = true;
            _assignedSlot?.NotifyBusArrived(this);
            HapticManager.Instance?.TriggerLight();
            BusStopManager.Instance?.NotifyBusArrived(this);
        });
    }

    private void ApplyParkedVisuals()
    {
        if (_assignedSlot == null) return;

        transform.rotation = _assignedSlot.ParkedVehicleAnchor.rotation;
        transform.localScale = _prefabBaseScale * _assignedSlot.ParkedVehicleScale;

        if (bodyRenderer != null && visualTheme != null)
        {
            Sprite diagSprite = visualTheme.GetDiagonalSprite(busColor, length);
            if (diagSprite != null)
            {
                bodyRenderer.sprite = diagSprite;
            }
            bodyRenderer.flipX = false;
            bodyRenderer.flipY = false;
            bodyRenderer.sortingOrder = 42;
        }
    }

    private Sequence BuildVipArcSequence(Vector3 targetPos)
    {
        Sequence seq = DOTween.Sequence();
        Vector3 start = transform.position;
        Vector3 mid = (start + targetPos) * 0.5f + new Vector3(0f, 0.9f, 0f);
        float dist = Vector3.Distance(start, targetPos);
        float duration = Mathf.Clamp(dist / Mathf.Max(0.01f, moveSpeed * 1.15f), 0.25f, 0.55f);

        Vector3[] path = { start, mid, targetPos };
        seq.Append(transform.DOPath(path, duration, PathType.CatmullRom).SetEase(Ease.InOutCubic));
        seq.Join(transform.DORotate(Vector3.zero, Mathf.Min(duration, 0.18f)));
        seq.SetLink(gameObject);
        return seq;
    }

    private Sequence BuildDriveSequence(Vector3 targetPos)
    {
        Sequence seq = DOTween.Sequence();
        Vector3 currentPos = transform.position;

        GridManager grid = Grid;
        float rightX = grid != null ? grid.RightLaneX : 3.10f;
        float leftX = grid != null ? grid.LeftLaneX : -3.10f;
        float bottomY = grid != null ? grid.BottomLaneY : -4.30f;
        float approachY = Mathf.Min(targetPos.y - Mathf.Max(0.3f, parkingApproachOffset),
            BusStopManager.Instance != null ? BusStopManager.Instance.ApronExitY : targetPos.y - 1.1f);

        float GetDuration(Vector3 from, Vector3 to)
        {
            float dist = Vector3.Distance(from, to);
            return Mathf.Max(dist / Mathf.Max(0.01f, moveSpeed), 0.04f);
        }

        void FaceSegment(Vector3 delta)
        {
            if (bodyRenderer == null) return;
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            {
                bool goLeft = delta.x < -0.01f;
                bodyRenderer.flipX = goLeft;
                transform.DORotate(Vector3.zero, Mathf.Max(0.02f, turnDuration * 0.6f)).SetEase(Ease.OutSine);
            }
            else
            {
                bodyRenderer.flipX = false;
                float angle = delta.y >= 0f ? 90f : 270f;
                transform.DORotate(new Vector3(0f, 0f, angle), Mathf.Max(0.02f, turnDuration * 0.6f)).SetEase(Ease.OutSine);
            }
        }

        void AppendLeg(Vector3 from, Vector3 to, Ease ease = Ease.Linear)
        {
            Vector3 delta = to - from;
            if (delta.sqrMagnitude < 0.0001f) return;
            float dur = GetDuration(from, to);
            seq.AppendCallback(() => FaceSegment(delta));
            seq.Append(transform.DOMove(to, dur).SetEase(ease));
        }

        switch (direction)
        {
            case Direction.Right:
            {
                Vector3 p1 = new Vector3(rightX, currentPos.y, 0f);
                Vector3 p2 = new Vector3(rightX, approachY, 0f);
                Vector3 p3 = new Vector3(targetPos.x, approachY, 0f);
                AppendLeg(currentPos, p1, Ease.InSine);
                AppendLeg(p1, p2);
                AppendLeg(p2, p3);
                AppendLeg(p3, targetPos, Ease.OutSine);
                break;
            }

            case Direction.Left:
            {
                Vector3 p1 = new Vector3(leftX, currentPos.y, 0f);
                Vector3 p2 = new Vector3(leftX, approachY, 0f);
                Vector3 p3 = new Vector3(targetPos.x, approachY, 0f);
                AppendLeg(currentPos, p1, Ease.InSine);
                AppendLeg(p1, p2);
                AppendLeg(p2, p3);
                AppendLeg(p3, targetPos, Ease.OutSine);
                break;
            }

            case Direction.Up:
            {
                if (Mathf.Abs(currentPos.x - targetPos.x) < 0.08f)
                {
                    AppendLeg(currentPos, targetPos, Ease.InOutSine);
                }
                else
                {
                    Vector3 p1 = new Vector3(currentPos.x, approachY, 0f);
                    Vector3 p2 = new Vector3(targetPos.x, approachY, 0f);
                    AppendLeg(currentPos, p1, Ease.InSine);
                    AppendLeg(p1, p2);
                    AppendLeg(p2, targetPos, Ease.OutSine);
                }
                break;
            }

            case Direction.Down:
            {
                Vector3 p1 = new Vector3(currentPos.x, bottomY, 0f);
                bool goRight = grid != null ? currentPos.x >= grid.transform.position.x : currentPos.x >= 0f;
                float sideX = goRight ? rightX : leftX;
                Vector3 p2 = new Vector3(sideX, bottomY, 0f);
                Vector3 p3 = new Vector3(sideX, approachY, 0f);
                Vector3 p4 = new Vector3(targetPos.x, approachY, 0f);

                AppendLeg(currentPos, p1, Ease.InSine);
                AppendLeg(p1, p2);
                AppendLeg(p2, p3);
                AppendLeg(p3, p4);
                AppendLeg(p4, targetPos, Ease.OutSine);
                break;
            }
        }

        seq.SetLink(gameObject);
        return seq;
    }

    public bool TryReserveNextSeat(out Transform seatPoint)
    {
        seatPoint = null;
        if (!HasEmptySeat) return false;

        List<Transform> activeList = (_isParked && parkedSeatPoints != null && parkedSeatPoints.Count >= Capacity)
            ? parkedSeatPoints
            : seatPoints;

        if (activeList == null || _reservedSeatsCount >= activeList.Count)
        {
            Debug.LogError($"[BusController] Not enough seat points on {name}; expected {Capacity}.", this);
            return false;
        }

        Transform seat = activeList[_reservedSeatsCount];
        if (seat == null || !seat.gameObject.activeInHierarchy)
        {
            Debug.LogError($"[BusController] Seat {_reservedSeatsCount} is missing or inactive on {name}.", this);
            return false;
        }

        _reservedSeatsCount++;
        seatPoint = seat;
        return true;
    }

    public void OnPassengerSeated(PassengerController passenger)
    {
        if (passenger != null)
        {
            _seatedPassengers.Add(passenger);
        }

        _seatedCount++;
        if (_seatedCount >= Capacity)
        {
            DepartFromStop();
        }
    }

    private void DepartFromStop()
    {
        _isBusy = true;
        IsReadyForPassengers = false;

        Vector3 parkedScale = transform.localScale;
        Vector3 exitPoint = BusStopManager.Instance != null
            ? BusStopManager.Instance.GetExitPoint(_assignedSlot)
            : transform.position + new Vector3(7f, -1f, 0f);

        float apronY = BusStopManager.Instance != null
            ? BusStopManager.Instance.ApronExitY
            : transform.position.y - 1.15f;

        bool exitLeft = exitPoint.x < transform.position.x;
        Vector3 apronPoint = new Vector3(transform.position.x + (exitLeft ? -0.45f : 0.45f), apronY, 0f);

        Sequence seq = DOTween.Sequence();
        _movementSequence = seq;

        Vector3 departPunch = new Vector3(-parkedScale.x * 0.05f, parkedScale.y * 0.08f, 0f);
        seq.Append(transform.DOPunchScale(departPunch, Mathf.Max(0.05f, departDelay), 2, 0.5f));
        seq.AppendCallback(() =>
        {
            CameraShakeManager.Instance?.ShakeLight();
            VFXManager.Instance?.PlayDust(transform.position, Quaternion.identity);
            AudioManager.Instance?.PlayEngine();
            HapticManager.Instance?.TriggerSuccess();

            if (bodyRenderer != null && visualTheme != null)
            {
                Sprite sideSp = visualTheme.GetGridSprite(busColor, length, exitLeft ? Direction.Left : Direction.Right);
                if (sideSp != null) bodyRenderer.sprite = sideSp;
                bodyRenderer.flipX = exitLeft;
            }
            transform.rotation = Quaternion.identity;
        });

        seq.Append(transform.DOMove(apronPoint, Mathf.Max(0.05f, departPulloutDuration)).SetEase(Ease.InSine));
        seq.Append(transform.DOMove(new Vector3(exitPoint.x, apronY, 0f), Mathf.Max(0.15f, departDuration)).SetEase(Ease.InQuad));
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

            BusStopManager.Instance?.UpdateNearFullWarning();
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

        Collider2D ownCollider = busCollider != null ? busCollider : GetComponent<Collider2D>();
        Collider2D blockerCollider = blocker.busCollider != null ? blocker.busCollider : blocker.GetComponent<Collider2D>();
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
            seq.Append(transform.DOPunchScale(_gridScale * 0.06f, 0.15f, 2, 0.5f));
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
        CaptureBaseScale();
        _movementSequence?.Kill();
        _movementSequence = null;
        transform.DOKill();
        ReleaseImpactReservation();
        _reservedForImpact = false;
        _isBusy = false;
        _isParked = false;
        IsReadyForPassengers = false;
        _reservedSeatsCount = 0;
        _seatedCount = 0;

        if (busCollider != null) busCollider.enabled = true;
        if (bodyRenderer != null)
        {
            bodyRenderer.flipX = false;
            bodyRenderer.flipY = false;
        }
        if (arrowRenderer != null)
        {
            arrowRenderer.gameObject.SetActive(true);
            arrowRenderer.flipX = false;
        }

        if (_assignedSlot != null)
        {
            _assignedSlot.ClearSlot();
            _assignedSlot = null;
        }

        transform.localScale = _prefabBaseScale;
        transform.rotation = Quaternion.identity;

        foreach (var p in _seatedPassengers)
        {
            if (p != null)
            {
                GamePoolManager.Instance.ReleasePassenger(p);
            }
        }
        _seatedPassengers.Clear();

        ReleaseRemainingSeatedChildren(seatPoints);
        ReleaseRemainingSeatedChildren(parkedSeatPoints);
    }

    private static void ReleaseRemainingSeatedChildren(List<Transform> points)
    {
        if (points == null) return;
        foreach (var seat in points)
        {
            if (seat == null) continue;
            for (int i = seat.childCount - 1; i >= 0; i--)
            {
                PassengerController p = seat.GetChild(i).GetComponent<PassengerController>();
                if (p != null)
                {
                    GamePoolManager.Instance.ReleasePassenger(p);
                }
            }
        }
    }
}
