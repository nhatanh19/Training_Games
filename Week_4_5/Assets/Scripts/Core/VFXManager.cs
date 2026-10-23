using System.Collections;
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
                if (_instance == null)
                {
                    GameObject go = new GameObject("VFXManager");
                    _instance = go.AddComponent<VFXManager>();
                }
            }
            return _instance;
        }
    }

    [SerializeField] private ParticleSystem dustPrefab;
    [SerializeField] private ParticleSystem hitSparkPrefab;
    [SerializeField] private ParticleSystem confettiPrefab;

    private ObjectPool<ParticleSystem> _dustPool;
    private ObjectPool<ParticleSystem> _hitSparkPool;
    private ObjectPool<ParticleSystem> _confettiPool;

    private ParticleSystem _runtimeDust;
    private ParticleSystem _runtimeHit;
    private ParticleSystem _runtimeConfetti;

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

        InitializeEffects();
    }

    private void InitializeEffects()
    {
        if (dustPrefab != null && _dustPool == null)
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
        else if (dustPrefab == null && _runtimeDust == null)
        {
            _runtimeDust = CreateProceduralDust();
        }

        if (hitSparkPrefab != null && _hitSparkPool == null)
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
        else if (hitSparkPrefab == null && _runtimeHit == null)
        {
            _runtimeHit = CreateProceduralHitSpark();
        }

        if (confettiPrefab != null && _confettiPool == null)
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
        else if (confettiPrefab == null && _runtimeConfetti == null)
        {
            _runtimeConfetti = CreateProceduralConfetti();
        }
    }

    public void PlayDust(Vector3 position, Quaternion rotation)
    {
        if (_dustPool != null)
        {
            ParticleSystem ps = _dustPool.Get();
            ps.transform.position = position;
            ps.transform.rotation = rotation;
            ps.Play();
            StartCoroutine(ReleaseRoutine(ps, _dustPool, ps.main.duration + ps.main.startLifetime.constantMax));
            return;
        }

        if (_runtimeDust == null) _runtimeDust = CreateProceduralDust();
        _runtimeDust.transform.position = position;
        _runtimeDust.transform.rotation = rotation;
        _runtimeDust.Emit(8);
    }

    public void PlayHitSpark(Vector3 position)
    {
        if (_hitSparkPool != null)
        {
            ParticleSystem ps = _hitSparkPool.Get();
            ps.transform.position = position;
            ps.transform.rotation = Quaternion.identity;
            ps.Play();
            StartCoroutine(ReleaseRoutine(ps, _hitSparkPool, ps.main.duration + ps.main.startLifetime.constantMax));
            return;
        }

        if (_runtimeHit == null) _runtimeHit = CreateProceduralHitSpark();
        _runtimeHit.transform.position = position;
        _runtimeHit.Emit(12);
    }

    public void PlayConfetti(Vector3? position = null)
    {
        Vector3 spawnPos = position ?? new Vector3(0f, 6f, 0f);

        if (_confettiPool != null)
        {
            ParticleSystem ps = _confettiPool.Get();
            ps.transform.position = spawnPos;
            ps.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            ps.Play();
            StartCoroutine(ReleaseRoutine(ps, _confettiPool, ps.main.duration + ps.main.startLifetime.constantMax));
            return;
        }

        if (_runtimeConfetti == null) _runtimeConfetti = CreateProceduralConfetti();
        _runtimeConfetti.transform.position = spawnPos;
        _runtimeConfetti.Emit(40);
    }

    private IEnumerator ReleaseRoutine(ParticleSystem ps, ObjectPool<ParticleSystem> pool, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (ps != null && ps.gameObject.activeSelf)
        {
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
        main.startLifetime = 0.35f;
        main.startSpeed = 1.5f;
        main.startSize = 0.25f;
        main.startColor = new Color(0.85f, 0.85f, 0.85f, 0.6f);

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 0.1f;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));

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
        main.startLifetime = 0.2f;
        main.startSpeed = 3.5f;
        main.startSize = 0.15f;
        main.startColor = new Color(1f, 0.8f, 0.2f, 1f);

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.15f;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));

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
        main.startLifetime = 2.0f;
        main.startSpeed = 2.5f;
        main.startSize = 0.18f;
        main.gravityModifier = 0.5f;

        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.2f, 0.2f, 1f),
            new Color(0.2f, 0.8f, 1f, 1f)
        );

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(6f, 0.5f, 0.1f);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));

        return ps;
    }
}
