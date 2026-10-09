using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WinPopupUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject _popupRoot;
    [SerializeField] private TextMeshProUGUI _levelTitleText;
    [SerializeField] private Button _nextLevelButton;
    [SerializeField] private Button _replayButton;

    void Awake()
    {
        if (_nextLevelButton != null)
        {
            _nextLevelButton.onClick.AddListener(OnNextLevelClicked);
        }

        if (_replayButton != null)
        {
            _replayButton.onClick.AddListener(OnReplayClicked);
        }

        Hide();
    }

    public void Show(int completedLevel)
    {
        if (_levelTitleText != null)
        {
            _levelTitleText.text = $"LEVEL {completedLevel}\nCOMPLETED!";
        }

        if (_popupRoot != null)
        {
            _popupRoot.SetActive(true);
        }
    }

    public void Hide()
    {
        if (_popupRoot != null)
        {
            _popupRoot.SetActive(false);
        }
    }

    private void OnNextLevelClicked()
    {
        Hide();
        LevelManager.Instance.NextLevel();
    }

    private void OnReplayClicked()
    {
        Hide();
        LevelManager.Instance.RestartCurrentLevel();
    }
}