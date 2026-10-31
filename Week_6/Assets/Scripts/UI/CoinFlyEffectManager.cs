using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CoinFlyEffectManager : MonoBehaviour
{
    public static CoinFlyEffectManager Instance { get; private set; }

    [Header("Pool Configuration")]
    [SerializeField] private GameObject _coinPrefab;
    [SerializeField] private Transform _poolContainer;
    [SerializeField] private int _initialPoolSize = 20;

    [Header("Fly Configuration")]
    [SerializeField] private float _burstRadius = 95f;
    [SerializeField] private float _burstDuration = 0.25f;
    [SerializeField] private float _flyDuration = 0.55f;
    [SerializeField] private float _staggerDelay = 0.05f;

    private readonly Queue<RectTransform> _availableCoins = new Queue<RectTransform>();
    private readonly List<RectTransform> _allCoins = new List<RectTransform>();
    private bool _isInitialized;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }

        InitializePool();
    }

    public void InitializePool()
    {
        if (_isInitialized) return;
        _isInitialized = true;

        if (_poolContainer == null)
        {
            _poolContainer = transform;
        }

        EnsureCoinPrefab();

        for (int i = 0; i < _initialPoolSize; i++)
        {
            RectTransform coin = CreateNewCoin();
            if (coin != null)
            {
                coin.gameObject.SetActive(false);
                _availableCoins.Enqueue(coin);
                _allCoins.Add(coin);
            }
        }
    }

    private void EnsureCoinPrefab()
    {
        if (_coinPrefab == null)
        {
            _coinPrefab = Resources.Load<GameObject>("Prefabs/UI/CoinItem");
#if UNITY_EDITOR
            if (_coinPrefab == null)
            {
                _coinPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/CoinItem.prefab");
            }
#endif
        }
    }

    private static Sprite _cachedCoinSprite;

    private static Sprite GetCoinSprite()
    {
        if (_cachedCoinSprite != null) return _cachedCoinSprite;

        _cachedCoinSprite = Resources.Load<Sprite>("Sprites/coin_icon");
        if (_cachedCoinSprite == null)
        {
            Texture2D tex = Resources.Load<Texture2D>("Sprites/coin_icon");
            if (tex != null)
            {
                _cachedCoinSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
        }
        return _cachedCoinSprite;
    }

    private RectTransform CreateNewCoin()
    {
        EnsureCoinPrefab();
        GameObject obj;
        if (_coinPrefab != null)
        {
            obj = Instantiate(_coinPrefab, _poolContainer);
        }
        else
        {
            obj = new GameObject("CoinItem_Pooled", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(_poolContainer, false);
        }

        Image img = obj.GetComponent<Image>();
        if (img == null)
        {
            img = obj.AddComponent<Image>();
        }

        Sprite coinSprite = GetCoinSprite();
        if (coinSprite != null)
        {
            img.sprite = coinSprite;
        }
        img.color = Color.white;
        img.preserveAspect = true;
        img.raycastTarget = false;

        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt == null)
        {
            rt = obj.AddComponent<RectTransform>();
        }
        rt.sizeDelta = new Vector2(56f, 56f);
        return rt;
    }

    private RectTransform GetCoin()
    {
        if (_availableCoins.Count > 0)
        {
            return _availableCoins.Dequeue();
        }

        RectTransform newCoin = CreateNewCoin();
        if (newCoin != null)
        {
            _allCoins.Add(newCoin);
        }
        return newCoin;
    }

    private void ReturnCoin(RectTransform coin)
    {
        if (coin == null) return;
        coin.DOKill();
        coin.gameObject.SetActive(false);
        coin.transform.SetParent(_poolContainer, false);
        _availableCoins.Enqueue(coin);
    }

    /// <summary>
    /// Executes the Fly-to-Target Coin Effect matching Week 6 criteria:
    /// 1. Spawn 5-10 coin icons from UI Object Pool.
    /// 2. Burst outward randomly around starting point (Ease.OutQuad).
    /// 3. Curved fly towards HUD Coin Icon (DOMove / DOPath).
    /// 4. HUD Coin punch, coin counter increment, and leng keng ASMR audio.
    /// 5. Recycle coin back into pool.
    /// </summary>
    public void PlayFlyCoins(Vector3 startScreenPos, Vector3 targetScreenPos, int coinCount = 8, int totalReward = 100, Action onComplete = null)
    {
        if (!_isInitialized) InitializePool();
        if (coinCount <= 0)
        {
            onComplete?.Invoke();
            return;
        }

        int rewardPerCoin = Mathf.Max(1, totalReward / coinCount);
        int remainingReward = totalReward - (rewardPerCoin * coinCount);
        int completedCoins = 0;

        for (int i = 0; i < coinCount; i++)
        {
            int index = i;
            RectTransform coin = GetCoin();
            if (coin == null) continue;

            coin.gameObject.SetActive(true);
            coin.position = startScreenPos;
            coin.localScale = Vector3.zero;

            // Step 2: Nổ bung nhẹ ra xung quanh vị trí xuất phát (góc phân kỳ ngẫu nhiên)
            float angle = (index * (Mathf.PI * 2f / coinCount)) + UnityEngine.Random.Range(-0.25f, 0.25f);
            float radius = UnityEngine.Random.Range(_burstRadius * 0.65f, _burstRadius);
            Vector3 burstTarget = startScreenPos + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);

            Sequence seq = DOTween.Sequence();
            seq.SetLink(coin.gameObject);

            // Phase 1: Nổ bung (Burst)
            seq.Append(coin.DOScale(Vector3.one, _burstDuration * 0.6f).SetEase(Ease.OutBack));
            seq.Join(coin.DOMove(burstTarget, _burstDuration).SetEase(Ease.OutQuad));

            // Phase 2: Stagger delay
            float delay = index * _staggerDelay;
            if (delay > 0)
            {
                seq.AppendInterval(delay);
            }

            // Step 3: Từng đồng tiền bay về phía Icon Tiền trên HUD theo quỹ đạo mượt mà (DOMove / DOPath)
            Vector3 midPoint = (burstTarget + targetScreenPos) * 0.5f;
            Vector3 perpendicular = Vector3.Cross(targetScreenPos - burstTarget, Vector3.forward).normalized;
            float curveOffset = UnityEngine.Random.Range(-80f, 80f);
            Vector3 controlPoint = midPoint + perpendicular * curveOffset;
            Vector3[] path = new Vector3[] { burstTarget, controlPoint, targetScreenPos };

            seq.Append(coin.DOPath(path, _flyDuration, PathType.CatmullRom).SetEase(Ease.InQuad));

            // Step 4 & 5: Chạm đích, nảy nhẹ icon, leng keng, nảy số, thu hồi về Pool
            seq.OnComplete(() =>
            {
                // Step 5: Thu hồi icon tiền về Pool
                ReturnCoin(coin);

                // Step 4: Khi mỗi đồng tiền chạm đích: Icon trên HUD nảy nhẹ (DOPunchScale), âm thanh tích xu kêu leng keng, bộ đếm số tăng dần theo từng nhịp chạm
                HUDController hud = UIManager.Instance?.HUD;
                if (hud != null)
                {
                    Transform targetTransform = hud.CoinTargetTransform;
                    if (targetTransform != null)
                    {
                        targetTransform.DOKill();
                        targetTransform.localScale = Vector3.one;
                        targetTransform.DOPunchScale(Vector3.one * 0.28f, 0.16f, 2, 0.5f)
                            .SetLink(hud.gameObject);
                    }

                    int addAmount = rewardPerCoin;
                    if (index == coinCount - 1)
                    {
                        addAmount += remainingReward;
                    }
                    hud.AddCoin(addAmount, animate: true);
                }

                float pitch = 0.95f + (index * 0.04f);
                AudioManager.Instance?.PlayCoin(pitch);

                completedCoins++;
                if (completedCoins >= coinCount)
                {
                    DOVirtual.DelayedCall(0.2f, () => onComplete?.Invoke())
                        .SetLink(gameObject);
                }
            });
        }
    }
}
