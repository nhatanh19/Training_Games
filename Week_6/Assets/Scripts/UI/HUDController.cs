using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [Header("Display Elements")]
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _coinText;
    [SerializeField] private Transform _coinTargetTransform;

    [Header("Buttons")]
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _settingsButton;

    private int _currentCoin = 0;

    public Transform CoinTargetTransform => _coinTargetTransform != null ? _coinTargetTransform : transform;

    private void Awake()
    {
        BindButtons();
    }

    private void BindButtons()
    {
        if (_restartButton != null)
        {
            _restartButton.onClick.RemoveAllListeners();
            _restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (_settingsButton != null)
        {
            _settingsButton.onClick.RemoveAllListeners();
            _settingsButton.onClick.AddListener(OnSettingsClicked);
        }
    }

    public void SetLevel(int levelNumber)
    {
        if (_levelText != null)
        {
            _levelText.text = $"LEVEL {levelNumber}";
        }
    }

    public void SetCoin(int amount, bool animate = false)
    {
        _currentCoin = amount;
        if (_coinText != null)
        {
            _coinText.text = _currentCoin.ToString();
            if (animate)
            {
                _coinText.transform.DOKill();
                _coinText.transform.localScale = Vector3.one;
                _coinText.transform.DOPunchScale(Vector3.one * 0.25f, 0.2f, 2, 0.5f)
                    .SetLink(gameObject);
            }
        }
    }

    public void AddCoin(int addedAmount, bool animate = true)
    {
        SetCoin(_currentCoin + addedAmount, animate);
    }

    private void OnRestartClicked()
    {
        AudioManager.Instance?.PlayClick();
        GameManager.Instance?.RestartLevel();
    }

    private void OnSettingsClicked()
    {
        AudioManager.Instance?.PlayClick();
        UIManager.Instance?.OpenSettings();
    }
}
