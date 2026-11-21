using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls LosePopup actions: Retry level, optional Revive, or return to Home.
/// </summary>
public class LosePopupController : MonoBehaviour
{
    [Header("Buttons (Authored in Prefab)")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button homeButton;
    [SerializeField] private Button reviveButton;

    private bool _bound;

    private void Awake()
    {
        Bind();
    }

    private void OnEnable()
    {
        Bind();
        if (reviveButton != null)
        {
            bool canRevive = BusStopManager.Instance != null && BusStopManager.Instance.HasLockedSlot;
            reviveButton.gameObject.SetActive(canRevive);
            reviveButton.interactable = canRevive;
        }
    }

    private void Bind()
    {
        if (_bound) return;
        _bound = true;

        if (retryButton != null) retryButton.onClick.AddListener(OnRetryClicked);
        if (homeButton != null) homeButton.onClick.AddListener(OnHomeClicked);
        if (reviveButton != null) reviveButton.onClick.AddListener(OnReviveClicked);
    }

    private void OnRetryClicked()
    {
        UIManager.Instance?.RestartFromLosePopup();
    }

    private void OnHomeClicked()
    {
        UIManager.Instance?.ReturnHomeFromLosePopup();
    }

    private void OnReviveClicked()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.GameOver) return;
        if (BusStopManager.Instance == null || !BusStopManager.Instance.HasLockedSlot) return;
        UIManager.Instance?.OpenContinuePopup(null);
    }
}
