using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class BottlePoolManager : MonoBehaviour
{
    public static BottlePoolManager Instance { get; private set; }
    [Header("Pool Configuration")]
    [SerializeField] private BottleController _bottlePrefab;
    [SerializeField] private int _defaultCapacity = 10;
    [SerializeField] private int _maxPoolSize = 20;

    private ObjectPool<BottleController> _pool;
    void Awake(){
        if(Instance == null){
            Instance = this;
        } else{
            Destroy(gameObject);
            return;
        }

        _pool = new ObjectPool<BottleController>(
            createFunc: CreateBottle,
            actionOnGet: OnTakeBottleFromPool,
            actionOnRelease: OnReturnBottleToPool,
            actionOnDestroy: OnDestroyBottleObject,
            collectionCheck: true,
            defaultCapacity: _defaultCapacity,
            maxSize: _maxPoolSize
        );
    }
    private BottleController CreateBottle()
    {
        BottleController newBottle = Instantiate(_bottlePrefab, transform);
        newBottle.gameObject.SetActive(false);
        return newBottle;
    }
    private void OnTakeBottleFromPool(BottleController bottle)
    {
        bottle.gameObject.SetActive(true);
    }
    private void OnReturnBottleToPool(BottleController bottle)
    {
        bottle.ResetState(); // Hủy tween, tắt hạt VFX, xóa màu cũ
        bottle.gameObject.SetActive(false);
    }
    private void OnDestroyBottleObject(BottleController bottle)
    {
        Destroy(bottle.gameObject);
    }

    public BottleController GetBottle()
    {
        return _pool.Get();
    }

    public void ReturnBottle(BottleController bottle)
    {
        if (bottle != null)
        {
            _pool.Release(bottle);
        }
    }

    public void ReturnAll(List<BottleController> activeBottles)
    {
        if (activeBottles == null) return;
        for (int i = activeBottles.Count - 1; i >= 0; i--)
        {
            if (activeBottles[i] != null)
            {
                _pool.Release(activeBottles[i]);
            }
        }
        activeBottles.Clear();
    }
}
