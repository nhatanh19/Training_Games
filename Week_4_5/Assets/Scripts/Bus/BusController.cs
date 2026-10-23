using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BusController : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer bodyRenderer;

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
    public bool HasEmptySeat => _passengersCount < 3;
    public bool IsReadyForPassengers { get; private set; } = false;

    private void OnValidate()
    {
        ApplyColor();
    }

    private void Start()
    {
        if (_gridManager == null)
        {
            _gridManager = GridManager.Instance != null ? GridManager.Instance : FindObjectOfType<GridManager>();
            ApplyColor();
            AlignToGrid();
            RegisterToGrid();
        }
    }

    public void Initialize(BusColor color, Direction dir, int len, Vector2Int rootPos)
    {
        busColor = color;
        direction = dir;
        length = len;
        rootGridPosition = rootPos;
        _gridManager = GridManager.Instance != null ? GridManager.Instance : FindObjectOfType<GridManager>();
        ApplyColor();
        AlignToGrid();
        RegisterToGrid();
    }

    public void ApplyColor()
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
            bodyRenderer.color = GetVisualColor(busColor);
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
        if (_gridManager == null) return;

        Vector2Int tailPos = rootGridPosition;
        Vector2Int headPos = GetHeadCell();

        Vector3 tailWorld = _gridManager.GridToWorldPosition(tailPos);
        Vector3 headWorld = _gridManager.GridToWorldPosition(headPos);

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
        if (_gridManager != null)
        {
            _gridManager.RegisterBus(this, GetOccupiedCells());
        }
    }

    public bool CanExit()
    {
        if (_gridManager == null) return false;

        Vector2Int step = direction.ToVector2Int();
        Vector2Int checkPos = GetHeadCell() + step;

        while (_gridManager.IsInsideGrid(checkPos))
        {
            if (_gridManager.IsCellOccupied(checkPos))
            {
                return false;
            }
            checkPos += step;
        }

        return true;
    }

    private void OnMouseDown()
    {
        OnClicked();
    }

    public void OnClicked()
    {
        if (_isBusy || _assignedSlot != null) return;

        AudioManager.Instance?.PlayClick();

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

    private void DriveToBusStop()
    {
        _isBusy = true;
        IsReadyForPassengers = false;
        GameInputManager.SetLock(true);
        _assignedSlot = BusStopManager.Instance.GetAvailableSlot();
        _assignedSlot.AssignBus(this);

        if (_gridManager != null)
        {
            _gridManager.UnregisterBus(GetOccupiedCells());
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
            BusStopManager.Instance.NotifyBusArrived(this);
        });
    }

    private Sequence BuildDriveSequence(Vector3 targetPos)
    {
        Sequence seq = DOTween.Sequence();
        Vector3 currentPos = transform.position;

        if (_gridManager == null)
        {
            _gridManager = GridManager.Instance != null ? GridManager.Instance : FindObjectOfType<GridManager>();
        }

        float rightX = _gridManager != null ? _gridManager.RightLaneX : currentPos.x + 4f;
        float leftX = _gridManager != null ? _gridManager.LeftLaneX : currentPos.x - 4f;
        float bottomY = _gridManager != null ? _gridManager.BottomLaneY : currentPos.y - 4f;

        float GetDuration(Vector3 from, Vector3 to)
        {
            float dist = Vector3.Distance(from, to);
            return Mathf.Max(dist / moveSpeed, 0.05f);
        }

        switch (direction)
        {
            case Direction.Right:
            {
                Vector3 p1 = new Vector3(rightX, currentPos.y, 0f);
                Vector3 p2 = new Vector3(rightX, targetPos.y, 0f);

                seq.Append(transform.DOMove(p1, GetDuration(currentPos, p1)).SetEase(Ease.Linear));
                seq.Append(transform.DORotate(new Vector3(0, 0, 90f), turnDuration));
                seq.Append(transform.DOMove(p2, GetDuration(p1, p2)).SetEase(Ease.Linear));
                seq.Append(transform.DORotate(new Vector3(0, 0, 180f), turnDuration));
                seq.Append(transform.DOMove(targetPos, GetDuration(p2, targetPos)).SetEase(Ease.Linear));
                seq.Append(transform.DORotate(Vector3.zero, turnDuration));
                break;
            }

            case Direction.Left:
            {
                Vector3 p1 = new Vector3(leftX, currentPos.y, 0f);
                Vector3 p2 = new Vector3(leftX, targetPos.y, 0f);

                seq.Append(transform.DOMove(p1, GetDuration(currentPos, p1)).SetEase(Ease.Linear));
                seq.Append(transform.DORotate(new Vector3(0, 0, 90f), turnDuration));
                seq.Append(transform.DOMove(p2, GetDuration(p1, p2)).SetEase(Ease.Linear));
                seq.Append(transform.DORotate(Vector3.zero, turnDuration));
                seq.Append(transform.DOMove(targetPos, GetDuration(p2, targetPos)).SetEase(Ease.Linear));
                break;
            }

            case Direction.Up:
            {
                Vector3 p1 = new Vector3(currentPos.x, targetPos.y, 0f);

                seq.Append(transform.DOMove(p1, GetDuration(currentPos, p1)).SetEase(Ease.Linear));
                float targetAngle = (targetPos.x >= currentPos.x) ? 0f : 180f;
                seq.Append(transform.DORotate(new Vector3(0, 0, targetAngle), turnDuration));
                seq.Append(transform.DOMove(targetPos, GetDuration(p1, targetPos)).SetEase(Ease.Linear));
                seq.Append(transform.DORotate(Vector3.zero, turnDuration));
                break;
            }

            case Direction.Down:
            {
                Vector3 p1 = new Vector3(currentPos.x, bottomY, 0f);
                bool goRight = _gridManager != null ? currentPos.x >= _gridManager.transform.position.x : currentPos.x >= 0f;
                float sideX = goRight ? rightX : leftX;

                Vector3 p2 = new Vector3(sideX, bottomY, 0f);
                Vector3 p3 = new Vector3(sideX, targetPos.y, 0f);

                seq.Append(transform.DOMove(p1, GetDuration(currentPos, p1)).SetEase(Ease.Linear));
                seq.Append(transform.DORotate(new Vector3(0, 0, goRight ? 0f : 180f), turnDuration));
                seq.Append(transform.DOMove(p2, GetDuration(p1, p2)).SetEase(Ease.Linear));
                seq.Append(transform.DORotate(new Vector3(0, 0, 90f), turnDuration));
                seq.Append(transform.DOMove(p3, GetDuration(p2, p3)).SetEase(Ease.Linear));
                seq.Append(transform.DORotate(new Vector3(0, 0, goRight ? 180f : 0f), turnDuration));
                seq.Append(transform.DOMove(targetPos, GetDuration(p3, targetPos)).SetEase(Ease.Linear));
                seq.Append(transform.DORotate(Vector3.zero, turnDuration));
                break;
            }
        }

        return seq;
    }

    public Transform GetNextSeatTransform()
    {
        if (!HasEmptySeat || _passengersCount >= seatPoints.Count) return null;
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

        if (_passengersCount >= 3)
        {
            DepartFromStop();
        }
    }

    private void DepartFromStop()
    {
        _isBusy = true;
        IsReadyForPassengers = false;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOPunchScale(new Vector3(0.2f, -0.15f, 0f), departDelay, 4, 0.5f));
        seq.AppendCallback(() =>
        {
            CameraShakeManager.Instance?.ShakeHeavy();
            VFXManager.Instance?.PlayDust(transform.position, transform.rotation);
            AudioManager.Instance?.PlayEngine();
        });
        seq.Append(transform.DOMoveX(transform.position.x + 8f, departDuration).SetEase(Ease.InQuad));
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
        transform.localScale = Vector3.one;
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
