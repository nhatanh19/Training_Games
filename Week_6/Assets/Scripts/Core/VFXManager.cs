using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class VFXManager : MonoBehaviour
{
    private static VFXManager _instance;

    public static VFXManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<VFXManager>();
            }
            return _instance;
        }
    }

    [Header("VFX Prefabs (Assigned in Editor)")]
    [SerializeField] private ParticleSystem dustPrefab;
    [SerializeField] private ParticleSystem hitSparkPrefab;
    [SerializeField] private ParticleSystem confettiPrefab;

    [Header("Burst & Anchors")]
    [SerializeField] private Transform confettiAnchor;
    [SerializeField] private bool emitWhenEmissionDisabled = true;
    [SerializeField, Min(0)] private int dustBurstCount = 12;
    [SerializeField, Min(0)] private int hitBurstCount = 16;
    [SerializeField, Min(0)] private int confettiBurstCount = 50;

    private ObjectPool<ParticleSystem> _dustPool;
    private ObjectPool<ParticleSystem> _hitSparkPool;
    private ObjectPool<ParticleSystem> _confettiPool;
    private readonly Dictionary<ParticleSystem, ObjectPool<ParticleSystem>> _activeEffects =
        new Dictionary<ParticleSystem, ObjectPool<ParticleSystem>>();

    private bool _initialized;

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
        if (_initialized) return;
        _initialized = true;

        _dustPool = CreatePool(dustPrefab, 5, 20);
        _hitSparkPool = CreatePool(hitSparkPrefab, 5, 20);
        _confettiPool = CreatePool(confettiPrefab, 2, 10);
    }

    private ObjectPool<ParticleSystem> CreatePool(ParticleSystem prefab, int defaultCap, int maxSize)
    {
        if (prefab == null) return null;
        return new ObjectPool<ParticleSystem>(
            createFunc: () => Instantiate(prefab, transform),
            actionOnGet: ps => ps.gameObject.SetActive(true),
            actionOnRelease: ps => ps.gameObject.SetActive(false),
            actionOnDestroy: ps => Destroy(ps.gameObject),
            collectionCheck: false,
            defaultCapacity: defaultCap,
            maxSize: maxSize
        );
    }

    public void PlayDust(Vector3 position, Quaternion rotation)
    {
        PlayFromPool(_dustPool, new Vector3(position.x, position.y, -1f), rotation, dustBurstCount);
    }

    public void PlayHitSpark(Vector3 position)
    {
        PlayFromPool(_hitSparkPool, new Vector3(position.x, position.y, -1f), Quaternion.identity, hitBurstCount);
    }

    public void PlayConfetti(Vector3? position = null)
    {
        Vector3 defaultPos = confettiAnchor != null ? confettiAnchor.position : new Vector3(0f, 5.5f, -1f);
        PlayFromPool(_confettiPool, position ?? defaultPos, Quaternion.Euler(90f, 0f, 0f), confettiBurstCount);
    }

    private void PlayFromPool(ObjectPool<ParticleSystem> pool, Vector3 spawnPos, Quaternion rotation, int burstCount)
    {
        if (pool == null) return;
        ParticleSystem ps = pool.Get();
        if (ps == null) return;

        ps.transform.position = spawnPos;
        ps.transform.rotation = rotation;
        ParticleSystemRenderer r = ps.GetComponent<ParticleSystemRenderer>();
        if (r != null) r.sortingOrder = 100;

        ps.Play();
        if (emitWhenEmissionDisabled && !ps.emission.enabled)
        {
            ps.Emit(Mathf.Max(0, burstCount));
        }

        _activeEffects[ps] = pool;
        StartCoroutine(ReleaseRoutine(ps, pool, ps.main.duration + ps.main.startLifetime.constantMax));
    }

    public void ClearLevelEffects()
    {
        StopAllCoroutines();
        foreach (var effect in _activeEffects)
        {
            if (effect.Key == null) continue;
            effect.Key.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.Value.Release(effect.Key);
        }
        _activeEffects.Clear();
    }

    private IEnumerator ReleaseRoutine(ParticleSystem ps, ObjectPool<ParticleSystem> pool, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (ps != null && _activeEffects.Remove(ps))
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            pool.Release(ps);
        }
    }
}
