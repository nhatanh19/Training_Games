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

    [SerializeField] private ParticleSystem dustPrefab;
    [SerializeField] private ParticleSystem hitSparkPrefab;
    [SerializeField] private ParticleSystem confettiPrefab;

    [Header("Manual Burst When Emission Is Disabled")]
    [SerializeField] private bool emitWhenEmissionDisabled = true;
    [SerializeField, Min(0)] private int dustBurstCount = 12;
    [SerializeField, Min(0)] private int hitBurstCount = 16;
    [SerializeField, Min(0)] private int confettiBurstCount = 50;

    private ObjectPool<ParticleSystem> _dustPool;
    private ObjectPool<ParticleSystem> _hitSparkPool;
    private ObjectPool<ParticleSystem> _confettiPool;
    private readonly Dictionary<ParticleSystem, ObjectPool<ParticleSystem>> _activeEffects =
        new Dictionary<ParticleSystem, ObjectPool<ParticleSystem>>();

    private ParticleSystem _runtimeDust;
    private ParticleSystem _runtimeHit;
    private ParticleSystem _runtimeConfetti;

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

        InitializeEffects();
    }

    private void InitializeEffects()
    {
        if (dustPrefab != null && _dustPool == null)
        {
            try
            {
                _dustPool = new ObjectPool<ParticleSystem>(
                    createFunc: () => Instantiate(dustPrefab, transform),
                    actionOnGet: ps => ps.gameObject.SetActive(true),
                    actionOnRelease: ps => ps.gameObject.SetActive(false),
                    actionOnDestroy: ps => Destroy(ps.gameObject),
                    collectionCheck: false,
                    defaultCapacity: 5,
                    maxSize: 20
                );
            }
            catch
            {
                _dustPool = null;
            }
        }

        if (hitSparkPrefab != null && _hitSparkPool == null)
        {
            try
            {
                _hitSparkPool = new ObjectPool<ParticleSystem>(
                    createFunc: () => Instantiate(hitSparkPrefab, transform),
                    actionOnGet: ps => ps.gameObject.SetActive(true),
                    actionOnRelease: ps => ps.gameObject.SetActive(false),
                    actionOnDestroy: ps => Destroy(ps.gameObject),
                    collectionCheck: false,
                    defaultCapacity: 5,
                    maxSize: 20
                );
            }
            catch
            {
                _hitSparkPool = null;
            }
        }

        if (confettiPrefab != null && _confettiPool == null)
        {
            try
            {
                _confettiPool = new ObjectPool<ParticleSystem>(
                    createFunc: () => Instantiate(confettiPrefab, transform),
                    actionOnGet: ps => ps.gameObject.SetActive(true),
                    actionOnRelease: ps => ps.gameObject.SetActive(false),
                    actionOnDestroy: ps => Destroy(ps.gameObject),
                    collectionCheck: false,
                    defaultCapacity: 2,
                    maxSize: 10
                );
            }
            catch
            {
                _confettiPool = null;
            }
        }

        if (_runtimeDust == null) _runtimeDust = CreateProceduralDust();
        if (_runtimeHit == null) _runtimeHit = CreateProceduralHitSpark();
        if (_runtimeConfetti == null) _runtimeConfetti = CreateProceduralConfetti();
    }

    public void PlayDust(Vector3 position, Quaternion rotation)
    {
        Vector3 spawnPos = new Vector3(position.x, position.y, -1f);

        if (_dustPool != null)
        {
            try
            {
                ParticleSystem ps = _dustPool.Get();
                ps.transform.position = spawnPos;
                ps.transform.rotation = rotation;
                EnsureSorting(ps, 100);
                PlayPooledEffect(ps, _dustPool, dustBurstCount);
                return;
            }
            catch
            {
                _dustPool = null;
            }
        }

        if (_runtimeDust == null) _runtimeDust = CreateProceduralDust();
        _runtimeDust.transform.position = spawnPos;
        _runtimeDust.transform.rotation = rotation;
        _runtimeDust.Emit(Mathf.Max(0, dustBurstCount));
    }

    public void PlayHitSpark(Vector3 position)
    {
        Vector3 spawnPos = new Vector3(position.x, position.y, -1f);

        if (_hitSparkPool != null)
        {
            try
            {
                ParticleSystem ps = _hitSparkPool.Get();
                ps.transform.position = spawnPos;
                ps.transform.rotation = Quaternion.identity;
                EnsureSorting(ps, 100);
                PlayPooledEffect(ps, _hitSparkPool, hitBurstCount);
                return;
            }
            catch
            {
                _hitSparkPool = null;
            }
        }

        if (_runtimeHit == null) _runtimeHit = CreateProceduralHitSpark();
        _runtimeHit.transform.position = spawnPos;
        _runtimeHit.Emit(Mathf.Max(0, hitBurstCount));
    }

    public void PlayConfetti(Vector3? position = null)
    {
        Vector3 spawnPos = position ?? new Vector3(0f, 5.5f, -1f);

        if (_confettiPool != null)
        {
            try
            {
                ParticleSystem ps = _confettiPool.Get();
                ps.transform.position = spawnPos;
                ps.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                EnsureSorting(ps, 100);
                PlayPooledEffect(ps, _confettiPool, confettiBurstCount);
                return;
            }
            catch
            {
                _confettiPool = null;
            }
        }

        if (_runtimeConfetti == null) _runtimeConfetti = CreateProceduralConfetti();
        _runtimeConfetti.transform.position = spawnPos;
        _runtimeConfetti.Emit(Mathf.Max(0, confettiBurstCount));
    }

    private void EnsureSorting(ParticleSystem ps, int order)
    {
        if (ps == null) return;
        var r = ps.GetComponent<ParticleSystemRenderer>();
        if (r != null) r.sortingOrder = order;
    }

    private void PlayPooledEffect(ParticleSystem ps, ObjectPool<ParticleSystem> pool, int burstCount)
    {
        ps.Play();
        if (emitWhenEmissionDisabled && !ps.emission.enabled)
            ps.Emit(Mathf.Max(0, burstCount));
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

        if (_runtimeDust != null) _runtimeDust.Clear(true);
        if (_runtimeHit != null) _runtimeHit.Clear(true);
        if (_runtimeConfetti != null) _runtimeConfetti.Clear(true);
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

    private ParticleSystem CreateProceduralDust()
    {
        GameObject go = new GameObject("VFX_Dust");
        go.transform.SetParent(transform);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startLifetime = 0.45f;
        main.startSpeed = 2.0f;
        main.startSize = 0.40f;
        main.startColor = new Color(0.92f, 0.92f, 0.92f, 0.75f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;
        shape.radius = 0.15f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = grad;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0.5f);
        curve.AddKey(1f, 1.2f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.sortingOrder = 100;

        return ps;
    }

    private ParticleSystem CreateProceduralHitSpark()
    {
        GameObject go = new GameObject("VFX_HitSpark");
        go.transform.SetParent(transform);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startLifetime = 0.25f;
        main.startSpeed = 4.5f;
        main.startSize = 0.20f;
        main.startColor = new Color(1f, 0.85f, 0.2f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.20f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.9f, 0.3f), 0.0f), new GradientColorKey(new Color(1f, 0.3f, 0.1f), 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = grad;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.sortingOrder = 100;

        return ps;
    }

    private ParticleSystem CreateProceduralConfetti()
    {
        GameObject go = new GameObject("VFX_Confetti");
        go.transform.SetParent(transform);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startLifetime = 2.5f;
        main.startSpeed = 3.0f;
        main.startSize = 0.25f;
        main.gravityModifier = 0.6f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.2f, 0.2f, 1f),
            new Color(0.2f, 0.8f, 1f, 1f)
        );

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(8f, 0.5f, 0.1f);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = grad;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.sortingOrder = 100;

        return ps;
    }
}
