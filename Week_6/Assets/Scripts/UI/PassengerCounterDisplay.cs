using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// Displays remaining passengers count on the HUD sign with punch-scale feedback.
/// </summary>
public class PassengerCounterDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI countText;

    private int _lastCount = -1;
    private Vector3 _baseScale = Vector3.one;
    private bool _capturedScale;

    private void Awake()
    {
        if (countText == null) countText = GetComponent<TextMeshProUGUI>();
        CaptureScale();
    }

    private void CaptureScale()
    {
        if (_capturedScale || countText == null) return;
        _baseScale = countText.transform.localScale == Vector3.zero ? Vector3.one : countText.transform.localScale;
        _capturedScale = true;
    }

    private void LateUpdate()
    {
        PassengerQueueManager queue = PassengerQueueManager.Instance;
        int count = queue != null ? queue.RemainingPassengersCount : 0;
        if (count == _lastCount) return;

        bool animate = _lastCount >= 0;
        _lastCount = count;
        if (countText == null) return;

        countText.text = count.ToString();
        if (animate)
        {
            CaptureScale();
            countText.transform.DOKill();
            countText.transform.localScale = _baseScale;
            countText.transform.DOPunchScale(_baseScale * 0.16f, 0.2f, 2, 0.5f).SetLink(gameObject);
        }
    }

    public void ResetCounter()
    {
        _lastCount = -1;
    }
}
