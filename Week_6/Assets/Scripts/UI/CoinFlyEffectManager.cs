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
    [SerializeField] private float _burstRadius = 90f;
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

        for (int i = 0; i < _initialPoolSize; i++)
        {
            RectTransform coin = CreateNewCoin();
            coin.gameObject.SetActive(false);
            _availableCoins.Enqueue(coin);
            _allCoins.Add(coin);
        }
    }

    private RectTransform CreateNewCoin()
    {
        GameObject obj;
        if (_coinPrefab != null)
        {
            obj = Instantiate(_coinPrefab, _poolContainer);
        }
        else
        {
            obj = new GameObject("CoinIcon_Pooled", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(_poolContainer, false);
            Image img = obj.GetComponent<Image>();
            img.color = new Color(1f, 0.82f, 0.12f, 1f);

            Sprite knob = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
            if (knob != null)
            {
                img.sprite = knob;
            }
            img.raycastTarget = false;
        }

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(46f, 46f);
        return rt;
    }

    private RectTransform GetCoin()
    {
        if (_availableCoins.Count > 0)
        {
            return _availableCoins.Dequeue();
        }

        RectTransform newCoin = CreateNewCoin();
        _allCoins.Add(newCoin);
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
            coin.gameObject.SetActive(true);
            coin.position = startScreenPos;
            coin.localScale = Vector3.zero;

            // Random burst offset
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float radius = UnityEngine.Random.Range(_burstRadius * 0.45f, _burstRadius);
            Vector3 burstTarget = startScreenPos + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);

            Sequence seq = DOTween.Sequence();
            seq.SetLink(coin.gameObject);

            // Phase 1: Burst explosion
            seq.Append(coin.DOScale(Vector3.one, _burstDuration * 0.6f).SetEase(Ease.OutBack));
            seq.Join(coin.DOMove(burstTarget, _burstDuration).SetEase(Ease.OutQuad));

            // Phase 2: Stagger delay before flight
            float delay = index * _staggerDelay;
            if (delay > 0)
            {
                seq.AppendInterval(delay);
            }

            // Phase 3: Curved Flight to target
            Vector3 midPoint = (burstTarget + targetScreenPos) * 0.5f;
            Vector3 perpendicular = Vector3.Cross(targetScreenPos - burstTarget, Vector3.forward).normalized;
            float curveOffset = UnityEngine.Random.Range(-90f, 90f);
            Vector3 controlPoint = midPoint + perpendicular * curveOffset;
            Vector3[] path = new Vector3[] { burstTarget, controlPoint, targetScreenPos };

            seq.Append(coin.DOPath(path, _flyDuration, PathType.CatmullRom).SetEase(Ease.InQuad));

            // Phase 4: Arrival hit
            seq.OnComplete(() =>
            {
                ReturnCoin(coin);

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
