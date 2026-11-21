using UnityEngine;
using UnityEngine.Pool;

public class GamePoolManager : MonoBehaviour
{
    private static GamePoolManager _instance;

    public static GamePoolManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GamePoolManager>();
            }
            return _instance;
        }
    }

    [Header("Prefabs (Assigned in Editor)")]
    [SerializeField] private BusController bus2x1Prefab;
    [SerializeField] private BusController bus3x1Prefab;
    [SerializeField] private PassengerController passengerPrefab;

    private ObjectPool<BusController> _bus2x1Pool;
    private ObjectPool<BusController> _bus3x1Pool;
    private ObjectPool<PassengerController> _passengerPool;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Initialize();
    }

    public void Initialize()
    {
        InitPools();
    }

    public void InitPools()
    {
        if (_bus2x1Pool != null && _passengerPool != null) return;

        if (bus2x1Prefab == null && LevelLoader.Instance != null)
        {
            bus2x1Prefab = LevelLoader.Instance.Bus2x1Prefab;
            bus3x1Prefab = LevelLoader.Instance.Bus3x1Prefab;
            passengerPrefab = LevelLoader.Instance.PassengerPrefab;
        }

        if (bus2x1Prefab != null && _bus2x1Pool == null)
        {
            _bus2x1Pool = new ObjectPool<BusController>(
                createFunc: () => Instantiate(bus2x1Prefab, transform),
                actionOnGet: bus => bus.gameObject.SetActive(true),
                actionOnRelease: bus =>
                {
                    bus.transform.SetParent(transform, false);
                    bus.ResetBus();
                    bus.gameObject.SetActive(false);
                },
                actionOnDestroy: bus => Destroy(bus.gameObject),
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize: 50
            );
        }

        if (bus3x1Prefab != null && _bus3x1Pool == null)
        {
            _bus3x1Pool = new ObjectPool<BusController>(
                createFunc: () => Instantiate(bus3x1Prefab, transform),
                actionOnGet: bus => bus.gameObject.SetActive(true),
                actionOnRelease: bus =>
                {
                    bus.transform.SetParent(transform, false);
                    bus.ResetBus();
                    bus.gameObject.SetActive(false);
                },
                actionOnDestroy: bus => Destroy(bus.gameObject),
                collectionCheck: false,
                defaultCapacity: 6,
                maxSize: 30
            );
        }

        if (passengerPrefab != null && _passengerPool == null)
        {
            _passengerPool = new ObjectPool<PassengerController>(
                createFunc: () => Instantiate(passengerPrefab, transform),
                actionOnGet: passenger => passenger.gameObject.SetActive(true),
                actionOnRelease: passenger =>
                {
                    passenger.transform.SetParent(transform, false);
                    passenger.ResetPassenger();
                    passenger.gameObject.SetActive(false);
                },
                actionOnDestroy: passenger => Destroy(passenger.gameObject),
                collectionCheck: false,
                defaultCapacity: 20,
                maxSize: 80
            );
        }
    }

    public BusController GetBus(int length, Transform parent = null)
    {
        if (_bus2x1Pool == null || _bus3x1Pool == null) InitPools();

        ObjectPool<BusController> pool = (length >= 3) ? _bus3x1Pool : _bus2x1Pool;
        if (pool == null) return null;

        BusController bus = pool.Get();
        if (parent != null)
        {
            bus.transform.SetParent(parent, false);
        }
        return bus;
    }

    public void ReleaseBus(BusController bus)
    {
        if (bus == null) return;
        if (_bus2x1Pool == null) InitPools();

        if (bus.Length >= 3 && _bus3x1Pool != null)
        {
            _bus3x1Pool.Release(bus);
        }
        else if (_bus2x1Pool != null)
        {
            _bus2x1Pool.Release(bus);
        }
    }

    public PassengerController GetPassenger(Transform parent = null)
    {
        if (_passengerPool == null) InitPools();
        if (_passengerPool == null) return null;

        PassengerController p = _passengerPool.Get();
        if (parent != null)
        {
            p.transform.SetParent(parent, false);
            p.ResetPassenger();
        }
        return p;
    }

    public void ReleasePassenger(PassengerController passenger)
    {
        if (passenger == null) return;
        if (_passengerPool == null) InitPools();

        if (_passengerPool != null)
        {
            _passengerPool.Release(passenger);
        }
    }
}
