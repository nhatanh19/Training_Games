using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-500)]
public class PassengerQueueManager : MonoBehaviour
{
    public static PassengerQueueManager Instance { get; private set; }

    [Header("Waypoint Corridor (Authored in Scene along BG glass barrier)")]
    [SerializeField] private List<Transform> queueWaypoints = new List<Transform>();
    [SerializeField, Min(0.1f)] private float spacing = 0.44f;
    [SerializeField, Min(1)] private int maxVisibleQueueLength = 12;

    [Header("Queue Motion & Rapid Boarding")]
    [SerializeField, Min(0.02f)] private float boardingStagger = 0.09f;
    [SerializeField, Min(0f)] private float stepStagger = 0.02f;
    [SerializeField, Min(0f)] private float maxStepDelay = 0.10f;

    private readonly List<PassengerController> _queue = new List<PassengerController>();
    private readonly List<PassengerController> _boardingPassengers = new List<PassengerController>();
    private bool _boardingLoopActive;
    private bool _initialized;

    public int RemainingPassengersCount => _queue.Count + _boardingPassengers.Count;
    public bool IsBoarding => _boardingPassengers.Count > 0 || _boardingLoopActive;
    public PassengerController PeekNextPassenger() => _queue.Count > 0 ? _queue[0] : null;
    public IReadOnlyList<Transform> Waypoints => queueWaypoints;
    public event System.Action OnPassengerBoarded;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (GameBootstrap.IsDriving) return;
        Initialize();
    }

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        if (BusStopManager.Instance != null)
        {
            BusStopManager.Instance.OnBusArrivedAtStop += OnBusArrived;
        }
    }

    private void OnDestroy()
    {
        if (BusStopManager.Instance != null)
        {
            BusStopManager.Instance.OnBusArrivedAtStop -= OnBusArrived;
        }
    }

    public void LoadPassengers(List<BusColor> colors, GameVisualThemeSO theme = null)
    {
        ClearQueue();
        if (colors == null) return;

        for (int i = 0; i < colors.Count; i++)
        {
            PassengerController passenger = GamePoolManager.Instance.GetPassenger(transform);
            if (passenger != null)
            {
                passenger.SetColor(colors[i], theme);
                bool visible = i < maxVisibleQueueLength;
                passenger.SetQueueAppearance(GetQueuePosition(i), visible);
                _queue.Add(passenger);
            }
        }
    }

    public void ClearQueue()
    {
        StopAllCoroutines();
        _boardingLoopActive = false;

        for (int i = 0; i < _boardingPassengers.Count; i++)
        {
            if (_boardingPassengers[i] != null)
            {
                GamePoolManager.Instance.ReleasePassenger(_boardingPassengers[i]);
            }
        }
        _boardingPassengers.Clear();

        for (int i = 0; i < _queue.Count; i++)
        {
            if (_queue[i] != null)
            {
                GamePoolManager.Instance.ReleasePassenger(_queue[i]);
            }
        }
        _queue.Clear();
    }

    public Vector3 GetQueuePosition(int index)
    {
        if (queueWaypoints == null || queueWaypoints.Count == 0)
        {
            return transform.position + Vector3.right * (index * spacing);
        }

        if (queueWaypoints.Count == 1)
        {
            return queueWaypoints[0] != null ? queueWaypoints[0].position : transform.position;
        }

        float remaining = Mathf.Max(0, index) * spacing;
        if (index >= maxVisibleQueueLength)
        {
            Transform tail = queueWaypoints[queueWaypoints.Count - 1];
            return tail != null ? tail.position : transform.position;
        }

        for (int i = 0; i < queueWaypoints.Count - 1; i++)
        {
            Transform a = queueWaypoints[i];
            Transform b = queueWaypoints[i + 1];
            if (a == null || b == null) continue;

            float segLen = Vector3.Distance(a.position, b.position);
            if (segLen <= 0.0001f) continue;

            if (remaining <= segLen)
            {
                return Vector3.Lerp(a.position, b.position, remaining / segLen);
            }
            remaining -= segLen;
        }

        Transform last = queueWaypoints[queueWaypoints.Count - 1];
        return last != null ? last.position : transform.position;
    }

    private void OnBusArrived(BusController bus)
    {
        TryBoardNextPassenger();
    }

    public void TryBoardNextPassenger()
    {
        if (_boardingLoopActive || _queue.Count == 0) return;
        StartCoroutine(RapidBoardRoutine());
    }

    private IEnumerator RapidBoardRoutine()
    {
        _boardingLoopActive = true;

        while (_queue.Count > 0)
        {
            PassengerController head = _queue[0];
            if (head == null)
            {
                _queue.RemoveAt(0);
                continue;
            }

            List<BusController> activeBuses = BusStopManager.Instance != null
                ? BusStopManager.Instance.GetActiveBuses()
                : null;

            if (activeBuses == null || activeBuses.Count == 0) break;

            BusController matchingBus = activeBuses.Find(b =>
                b != null && b.Color == head.Color && b.HasEmptySeat && b.IsReadyForPassengers);

            if (matchingBus == null) break;

            if (!matchingBus.TryReserveNextSeat(out Transform seatPoint)) break;

            _queue.RemoveAt(0);
            _boardingPassengers.Add(head);
            UpdateQueuePositions();

            PassengerController capturedPassenger = head;
            BusController capturedBus = matchingBus;
            capturedPassenger.MoveToSeat(seatPoint, () =>
            {
                _boardingPassengers.Remove(capturedPassenger);
                capturedBus.OnPassengerSeated(capturedPassenger);
                OnPassengerBoarded?.Invoke();
                if (!_boardingLoopActive)
                {
                    TryBoardNextPassenger();
                }
            });

            yield return new WaitForSeconds(Mathf.Max(0.02f, boardingStagger));
        }

        _boardingLoopActive = false;
    }

    /// <summary>
    /// Used by the SortQueue booster: pulls passengers matching priority colors to the front of the queue.
    /// </summary>
    public bool ReorderMatchingToFront(List<BusColor> priorityColors)
    {
        if (priorityColors == null || priorityColors.Count == 0 || _queue.Count <= 1) return false;

        var prioritySet = new HashSet<BusColor>(priorityColors);
        var matching = new List<PassengerController>();
        var others = new List<PassengerController>();

        bool anyChanged = false;
        bool seenNonMatch = false;

        for (int i = 0; i < _queue.Count; i++)
        {
            PassengerController p = _queue[i];
            if (p == null) continue;
            if (prioritySet.Contains(p.Color))
            {
                if (seenNonMatch) anyChanged = true;
                matching.Add(p);
            }
            else
            {
                seenNonMatch = true;
                others.Add(p);
            }
        }

        if (!anyChanged) return false;

        _queue.Clear();
        _queue.AddRange(matching);
        _queue.AddRange(others);
        UpdateQueuePositions();
        TryBoardNextPassenger();
        return true;
    }

    private void UpdateQueuePositions()
    {
        for (int i = 0; i < _queue.Count; i++)
        {
            PassengerController passenger = _queue[i];
            if (passenger != null)
            {
                bool visible = i < maxVisibleQueueLength;
                float delay = Mathf.Min(i * Mathf.Max(0f, stepStagger), Mathf.Max(0f, maxStepDelay));
                passenger.StepForward(GetQueuePosition(i), visible, delay);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (queueWaypoints == null || queueWaypoints.Count == 0) return;

        Gizmos.color = UnityEngine.Color.yellow;
        for (int i = 0; i < queueWaypoints.Count; i++)
        {
            if (queueWaypoints[i] == null) continue;
            Gizmos.DrawWireSphere(queueWaypoints[i].position, 0.16f);
            if (i + 1 < queueWaypoints.Count && queueWaypoints[i + 1] != null)
            {
                Gizmos.DrawLine(queueWaypoints[i].position, queueWaypoints[i + 1].position);
            }
        }

        Gizmos.color = new UnityEngine.Color(0.25f, 1f, 0.45f, 0.65f);
        int previewCount = Mathf.Min(8, maxVisibleQueueLength);
        for (int i = 0; i < previewCount; i++)
        {
            Gizmos.DrawWireSphere(GetQueuePosition(i), 0.14f);
        }
    }
}
