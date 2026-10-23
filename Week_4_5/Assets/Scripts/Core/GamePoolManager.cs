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
                if (_instance == null)
                {
                    GameObject go = new GameObject("GamePoolManager");
                    _instance = go.AddComponent<GamePoolManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Prefabs")]
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

        InitPools();
    }

    public void SetPrefabs(BusController b2, BusController b3, PassengerController p)
    {
        bus2x1Prefab = b2;
        bus3x1Prefab = b3;
        passengerPrefab = p;
        _bus2x1Pool = null;
        _bus3x1Pool = null;
        _passengerPool = null;
        InitPools();
    }

    public void InitPools()
    {
        if (_bus2x1Pool != null && _passengerPool != null) return;

        if (bus2x1Prefab == null)
        {
            LevelLoader loader = LevelLoader.Instance != null ? LevelLoader.Instance : FindObjectOfType<LevelLoader>();
            if (loader != null)
            {
                bus2x1Prefab = loader.Bus2x1Prefab;
                bus3x1Prefab = loader.Bus3x1Prefab;
                passengerPrefab = loader.PassengerPrefab;
            }
        }

        if (bus2x1Prefab != null && _bus2x1Pool == null)
        {
            _bus2x1Pool = new ObjectPool<BusController>(
                createFunc: () => Instantiate(bus2x1Prefab, transform),
                actionOnGet: bus => bus.gameObject.SetActive(true),
                actionOnRelease: bus =>
                {
                    bus.ResetBus();
                    bus.gameObject.SetActive(false);
                    bus.transform.SetParent(transform);
                },
                actionOnDestroy: bus => Destroy(bus.gameObject),
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize: 50
            );
        }

        if (bus3x1Prefab != null)
        {
            _bus3x1Pool = new ObjectPool<BusController>(
                createFunc: () => Instantiate(bus3x1Prefab, transform),
                actionOnGet: bus => bus.gameObject.SetActive(true),
                actionOnRelease: bus =>
                {
                    bus.ResetBus();
                    bus.gameObject.SetActive(false);
                    bus.transform.SetParent(transform);
                },
                actionOnDestroy: bus => Destroy(bus.gameObject),
                collectionCheck: false,
                defaultCapacity: 5,
                maxSize: 30
            );
        }

        if (passengerPrefab != null)
        {
            _passengerPool = new ObjectPool<PassengerController>(
                createFunc: () => Instantiate(passengerPrefab, transform),
                actionOnGet: passenger => passenger.gameObject.SetActive(true),
                actionOnRelease: passenger =>
                {
                    passenger.gameObject.SetActive(false);
                    passenger.transform.SetParent(transform);
                },
                actionOnDestroy: passenger => Destroy(passenger.gameObject),
                collectionCheck: false,
                defaultCapacity: 15,
                maxSize: 60
            );
        }
    }

    public BusController GetBus(int length, Transform parent = null)
    {
        if (_bus2x1Pool == null || _bus3x1Pool == null) InitPools();

        ObjectPool<BusController> pool = (length == 3) ? _bus3x1Pool : _bus2x1Pool;
        if (pool == null)
        {
            InitPools();
            pool = (length == 3) ? _bus3x1Pool : _bus2x1Pool;
            if (pool == null) return null;
        }

        BusController bus = pool.Get();
        if (parent != null)
        {
            bus.transform.SetParent(parent);
        }
        return bus;
    }

    public void ReleaseBus(BusController bus)
    {
        if (bus == null) return;
        if (_bus2x1Pool == null) InitPools();

        if (bus.Length == 3 && _bus3x1Pool != null)
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
            p.transform.SetParent(parent);
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
