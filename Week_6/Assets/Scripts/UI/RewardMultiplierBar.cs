using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Animates the Win popup multiplier pointer back and forth across configured reward zones.
/// </summary>
public class RewardMultiplierBar : MonoBehaviour
{
    [Serializable]
    public class MultiplierZone
    {
        [Range(-1f, 1f)] public float minNormalizedX;
        [Range(-1f, 1f)] public float maxNormalizedX;
        [Min(1)] public int multiplier = 2;
    }

    [Header("Pointer & Motion")]
    [SerializeField] private RectTransform pointerRect;
    [SerializeField, Min(10f)] private float travelHalfWidth = 260f;
    [SerializeField, Min(0.2f)] private float sweepSpeed = 1.65f;

    [Header("Zones (Normalized [-1 .. 1])")]
    [SerializeField] private List<MultiplierZone> zones = new List<MultiplierZone>();

    private bool _running;
    private float _phase;
    private int _currentMultiplier = 2;

    public int CurrentMultiplier => _currentMultiplier;
    public event Action<int> OnMultiplierChanged;

    public void StartOscillating()
    {
        _phase = 0f;
        _running = true;
        UpdatePointer(0f);
    }

    public int StopAndSample()
    {
        _running = false;
        return _currentMultiplier;
    }

    private void Update()
    {
        if (!_running) return;
        _phase += Time.unscaledDeltaTime * sweepSpeed;
        float norm = Mathf.PingPong(_phase, 2f) - 1f;
        UpdatePointer(norm);
    }

    private void UpdatePointer(float normalizedX)
    {
        if (pointerRect != null)
        {
            Vector2 pos = pointerRect.anchoredPosition;
            pos.x = normalizedX * travelHalfWidth;
            pointerRect.anchoredPosition = pos;
        }

        int mult = EvaluateMultiplier(normalizedX);
        if (mult != _currentMultiplier)
        {
            _currentMultiplier = mult;
            OnMultiplierChanged?.Invoke(_currentMultiplier);
        }
    }

    private int EvaluateMultiplier(float normX)
    {
        for (int i = 0; i < zones.Count; i++)
        {
            MultiplierZone z = zones[i];
            if (z != null && normX >= z.minNormalizedX && normX <= z.maxNormalizedX)
            {
                return Mathf.Max(1, z.multiplier);
            }
        }
        return 2;
    }
}
