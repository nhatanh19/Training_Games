using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class CoinFlyEffectManager : MonoBehaviour
{
    public static CoinFlyEffectManager Instance { get; private set; }

    [Header("Pool Configuration")]
    [SerializeField] private RectTransform _coinPrefab;
    [SerializeField] private Transform _poolContainer;
    [SerializeField] private int _initialPoolSize = 20;

    [Header("Fly Configuration")]
    [SerializeField] private float _burstRadius = 95f;
    [SerializeField] private float _burstDuration = 0.25f;
    [SerializeField] private float _flyDuration = 0.52f;
    [SerializeField] private float _staggerDelay = 0.045f;

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

        if (_coinPrefab == null)
        {
            Debug.LogError("[CoinFlyEffectManager] _coinPrefab is not assigned in Inspector.");
            return;
        }

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

    private RectTransform CreateNewCoin()
    {
        if (_coinPrefab == null) return null;
        return Instantiate(_coinPrefab, _poolContainer);
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

    public void PlayFlyCoins(Vector3 startScreenPos, Vector3 targetScreenPos, int coinCount = 8, Action onComplete = null)
    {
        Transform fallbackTarget = UIManager.Instance != null && UIManager.Instance.HUD != null
            ? UIManager.Instance.HUD.CoinTargetTransform
            : null;
        PlayFlyCoinsInternal(startScreenPos, targetScreenPos, fallbackTarget, coinCount, onComplete);
    }

    public void PlayFlyCoins(Vector3 startScreenPos, Transform targetTransform, int coinCount = 8, Action onComplete = null)
    {
        Vector3 targetPos = targetTransform != null ? targetTransform.position : startScreenPos;
        PlayFlyCoinsInternal(startScreenPos, targetPos, targetTransform, coinCount, onComplete);
    }

    private void PlayFlyCoinsInternal(Vector3 startScreenPos, Vector3 targetScreenPos, Transform punchTarget, int coinCount, Action onComplete)
    {
        if (!_isInitialized) InitializePool();
        if (coinCount <= 0 || _coinPrefab == null)
        {
            onComplete?.Invoke();
            return;
        }

        Vector3 targetBaseScale = punchTarget != null && punchTarget.localScale != Vector3.zero
            ? punchTarget.localScale
            : Vector3.one;

        int completedCoins = 0;
        int scheduledCoins = 0;

        for (int i = 0; i < coinCount; i++)
        {
            int index = i;
            RectTransform coin = GetCoin();
            if (coin == null) continue;
            scheduledCoins++;

            coin.gameObject.SetActive(true);
            coin.position = startScreenPos;
            coin.localScale = Vector3.zero;

            float angle = (index * (Mathf.PI * 2f / coinCount)) + UnityEngine.Random.Range(-0.25f, 0.25f);
            float radius = UnityEngine.Random.Range(_burstRadius * 0.65f, _burstRadius);
            Vector3 burstTarget = startScreenPos + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);

            Sequence seq = DOTween.Sequence().SetUpdate(true).SetLink(coin.gameObject);
            seq.Append(coin.DOScale(_coinPrefab.localScale, _burstDuration * 0.6f).SetEase(Ease.OutBack));
            seq.Join(coin.DOMove(burstTarget, _burstDuration).SetEase(Ease.OutQuad));

            float delay = index * _staggerDelay;
            if (delay > 0f) seq.AppendInterval(delay);

            Vector3 midPoint = (burstTarget + targetScreenPos) * 0.5f;
            Vector3 perpendicular = Vector3.Cross(targetScreenPos - burstTarget, Vector3.forward).normalized;
            float curveOffset = UnityEngine.Random.Range(-80f, 80f);
            Vector3 controlPoint = midPoint + perpendicular * curveOffset;
            Vector3[] path = new Vector3[] { burstTarget, controlPoint, targetScreenPos };

            seq.Append(coin.DOPath(path, _flyDuration, PathType.CatmullRom).SetEase(Ease.InQuad));

            seq.OnComplete(() =>
            {
                ReturnCoin(coin);

                if (punchTarget != null && punchTarget.gameObject.activeInHierarchy)
                {
                    punchTarget.DOKill();
                    punchTarget.localScale = targetBaseScale;
                    punchTarget.DOPunchScale(targetBaseScale * 0.25f, 0.16f, 2, 0.5f)
                        .SetUpdate(true)
                        .SetLink(punchTarget.gameObject);
                }

                float pitch = 0.95f + (index * 0.04f);
                AudioManager.Instance?.PlayCoin(pitch);

                completedCoins++;
                if (completedCoins >= scheduledCoins)
                {
                    DOVirtual.DelayedCall(0.18f, () => onComplete?.Invoke(), true)
                        .SetLink(gameObject);
                }
            });
        }

        if (scheduledCoins == 0) onComplete?.Invoke();
    }
}
