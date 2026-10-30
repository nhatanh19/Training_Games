using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-500)]
public class PassengerQueueManager : MonoBehaviour
{
    public static PassengerQueueManager Instance { get; private set; }

    [Header("Queue Layout")]
    [SerializeField] private Transform queueStartPoint;
    [SerializeField] private Vector2 queueDirection = Vector2.right;
    [SerializeField] private float spacing = 0.65f;

    [Header("Initial Passengers")]
    [SerializeField] private List<PassengerController> initialPassengers = new List<PassengerController>();

    private readonly Queue<PassengerController> _queue = new Queue<PassengerController>();
    private bool _isBoarding = false;
    private bool _initialized = false;

    public int RemainingPassengersCount => _queue.Count;
    public bool IsBoarding => _isBoarding;
    public PassengerController PeekNextPassenger() => _queue.Count > 0 ? _queue.Peek() : null;
    public event System.Action OnPassengerBoarded;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // When a GameBootstrap exists it calls Initialize() before the level starts.
        if (GameBootstrap.IsDriving) return;

        Initialize();
    }

    /// <summary>
    /// Subscribes to bus-stop events and seeds the queue from the authored passengers.
    /// The level loader replaces this queue with the level data afterwards.
    /// Called by GameBootstrap, or by Start() when the scene has no bootstrap.
    /// </summary>
    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        if (BusStopManager.Instance != null)
        {
            BusStopManager.Instance.OnBusArrivedAtStop += OnBusArrived;
        }

        InitializeQueue();
    }

    private void OnDestroy()
    {
        if (BusStopManager.Instance != null)
        {
            BusStopManager.Instance.OnBusArrivedAtStop -= OnBusArrived;
        }
    }

    public void InitializeQueue()
    {
        _queue.Clear();

        if (initialPassengers.Count == 0)
        {
            initialPassengers.AddRange(GetComponentsInChildren<PassengerController>());
        }

        for (int i = 0; i < initialPassengers.Count; i++)
        {
            PassengerController passenger = initialPassengers[i];
            if (passenger != null)
            {
                passenger.transform.position = GetQueuePosition(i);
                _queue.Enqueue(passenger);
            }
        }
    }

    public void LoadPassengers(List<BusColor> colors, PassengerController prefab)
    {
        ClearQueue();

        for (int i = 0; i < colors.Count; i++)
        {
            PassengerController passenger = GamePoolManager.Instance.GetPassenger(transform);
            if (passenger != null)
            {
                passenger.transform.position = GetQueuePosition(i);
                passenger.SetColor(colors[i]);
                _queue.Enqueue(passenger);
            }
        }
    }

    public void ClearQueue()
    {
        while (_queue.Count > 0)
        {
            PassengerController p = _queue.Dequeue();
            if (p != null)
            {
                GamePoolManager.Instance.ReleasePassenger(p);
            }
        }
        _queue.Clear();
    }

    public Vector3 GetQueuePosition(int index)
    {
        Vector3 origin = queueStartPoint != null ? queueStartPoint.position : transform.position;
        Vector3 dir = new Vector3(queueDirection.x, queueDirection.y, 0f).normalized;
        return origin + dir * (index * spacing);
    }

    private void OnBusArrived(BusController bus)
    {
        TryBoardNextPassenger();
    }

    public void TryBoardNextPassenger()
    {
        if (_isBoarding || _queue.Count == 0) return;

        PassengerController head = _queue.Peek();
        if (head == null) return;

        List<BusController> activeBuses = BusStopManager.Instance != null ? BusStopManager.Instance.GetActiveBuses() : new List<BusController>();
        BusController matchingBus = activeBuses.Find(b => b.Color == head.Color && b.HasEmptySeat && b.IsReadyForPassengers);

        if (matchingBus != null)
        {
            StartCoroutine(BoardRoutine(head, matchingBus));
        }
    }

    private IEnumerator BoardRoutine(PassengerController passenger, BusController bus)
    {
        _isBoarding = true;
        _queue.Dequeue();

        Transform seatPoint = bus.GetNextSeatTransform();
        bool jumped = false;

        passenger.MoveToSeat(seatPoint, () =>
        {
            bus.OnPassengerSeated(passenger);
            jumped = true;
            OnPassengerBoarded?.Invoke();
        });

        yield return new WaitUntil(() => jumped);

        UpdateQueuePositions();

        yield return new WaitForSeconds(0.1f);
        _isBoarding = false;

        TryBoardNextPassenger();
    }

    private void UpdateQueuePositions()
    {
        int index = 0;
        foreach (var passenger in _queue)
        {
            if (passenger != null)
            {
                passenger.StepForward(GetQueuePosition(index));
                index++;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = UnityEngine.Color.green;
        Vector3 origin = queueStartPoint != null ? queueStartPoint.position : transform.position;
        Vector3 dir = new Vector3(queueDirection.x, queueDirection.y, 0f).normalized;

        for (int i = 0; i < 6; i++)
        {
            Vector3 pos = origin + dir * (i * spacing);
            Gizmos.DrawWireSphere(pos, 0.3f);
        }
    }
}
