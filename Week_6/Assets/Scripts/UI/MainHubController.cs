using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the persistent Main Hub shell: TopBar (Coins, Settings), 3 Tab Views, and animated BottomNavBar.
/// </summary>
public class MainHubController : MonoBehaviour
{
    [Header("Shell & Canvas")]
    [SerializeField] private CanvasGroup hubGroup;
    [SerializeField] private GameObject bottomNavRoot;
    [SerializeField] private Button backToGameplayButton;

    [Header("Tab Views")]
    [SerializeField] private ShopScreenController shopScreen;
    [SerializeField] private HomeScreenController homeScreen;
    [SerializeField] private CollectionScreenController collectionScreen;

    [Header("Bottom Navigation")]
    [SerializeField] private Button shopTabButton;
    [SerializeField] private Button homeTabButton;
    [SerializeField] private Button collectionTabButton;
    [SerializeField] private RectTransform tabSelectionIndicator;
    [SerializeField] private RectTransform shopTabAnchor;
    [SerializeField] private RectTransform homeTabAnchor;
    [SerializeField] private RectTransform collectionTabAnchor;
    [SerializeField, Min(0.05f)] private float tabSlideDuration = 0.22f;

    [Header("Top Bar")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private Transform coinTargetTransform;
    [SerializeField] private Button coinAddButton;
    [SerializeField] private Button settingsButton;

    private HubTab _activeTab = HubTab.Home;
    private int _displayedCoins = -1;
    private Tween _coinTween;
    private Tween _tabSlideTween;
    private Tween _fadeTween;
    private Vector3 _coinTextScale = Vector3.one;
    private bool _bound;

    public Transform CoinTargetTransform => coinTargetTransform != null ? coinTargetTransform : transform;

    private void Awake()
    {
        if (coinText != null)
        {
            _coinTextScale = coinText.transform.localScale == Vector3.zero ? Vector3.one : coinText.transform.localScale;
        }
        BindButtons();
    }

    private void OnEnable()
    {
        BindButtons();
        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.OnCoinsChanged += HandleCoinsChanged;
            SetCoinsImmediate(ProgressManager.Instance.TotalCoins);
        }
    }

    private void OnDisable()
    {
        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.OnCoinsChanged -= HandleCoinsChanged;
        }
    }

    private void BindButtons()
    {
        if (_bound) return;
        _bound = true;

        if (shopTabButton != null) shopTabButton.onClick.AddListener(() => SelectTab(HubTab.Shop, true));
        if (homeTabButton != null) homeTabButton.onClick.AddListener(() => SelectTab(HubTab.Home, true));
        if (collectionTabButton != null) collectionTabButton.onClick.AddListener(() => SelectTab(HubTab.Collection, true));
        if (coinAddButton != null) coinAddButton.onClick.AddListener(() => SelectTab(HubTab.Shop, true));
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
        if (backToGameplayButton != null) backToGameplayButton.onClick.AddListener(OnBackToGameplayClicked);
    }

    public void ShowHub(HubTab tab, bool showBackToGameplay)
    {
        _fadeTween?.Kill();
        gameObject.SetActive(true);

        if (backToGameplayButton != null) backToGameplayButton.gameObject.SetActive(showBackToGameplay);
        if (bottomNavRoot != null) bottomNavRoot.SetActive(!showBackToGameplay);

        if (ProgressManager.Instance != null)
        {
            SetCoinsImmediate(ProgressManager.Instance.TotalCoins);
        }

        SelectTab(tab, animateIndicator: false);

        if (hubGroup != null)
        {
            hubGroup.alpha = 0f;
            hubGroup.interactable = false;
            hubGroup.blocksRaycasts = true;
            _fadeTween = hubGroup.DOFade(1f, 0.22f).SetUpdate(true).SetLink(gameObject)
                .OnComplete(() =>
                {
                    _fadeTween = null;
                    hubGroup.interactable = true;
                });
        }
    }

    public void HideHub()
    {
        _fadeTween?.Kill();
        _fadeTween = null;
        gameObject.SetActive(false);
    }

    public void HideImmediate()
    {
        _fadeTween?.Kill();
        _fadeTween = null;
        if (hubGroup != null) hubGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    public void SelectTab(HubTab tab, bool animateIndicator)
    {
        _activeTab = tab;

        if (shopScreen != null) shopScreen.SetVisible(tab == HubTab.Shop);
        if (homeScreen != null) homeScreen.SetVisible(tab == HubTab.Home);
        if (collectionScreen != null) collectionScreen.SetVisible(tab == HubTab.Collection);

        MoveTabIndicator(tab, animateIndicator);
        AppFlowController.Instance?.OnHubTabChanged(tab);
    }

    private void MoveTabIndicator(HubTab tab, bool animate)
    {
        if (tabSelectionIndicator == null) return;

        RectTransform anchor = tab switch
        {
            HubTab.Shop => shopTabAnchor,
            HubTab.Collection => collectionTabAnchor,
            _ => homeTabAnchor
        };
        if (anchor == null) return;

        _tabSlideTween?.Kill();
        float targetX = anchor.anchoredPosition.x;

        if (animate && gameObject.activeInHierarchy)
        {
            _tabSlideTween = tabSelectionIndicator.DOAnchorPosX(targetX, tabSlideDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(() => _tabSlideTween = null);
        }
        else
        {
            Vector2 pos = tabSelectionIndicator.anchoredPosition;
            pos.x = targetX;
            tabSelectionIndicator.anchoredPosition = pos;
        }
    }

    private void SetCoinsImmediate(int amount)
    {
        _coinTween?.Kill();
        _displayedCoins = amount;
        if (coinText != null) coinText.text = amount.ToString();
    }

    private void HandleCoinsChanged(int newTotal)
    {
        if (coinText == null) return;
        if (_displayedCoins < 0)
        {
            SetCoinsImmediate(newTotal);
            return;
        }

        int from = _displayedCoins;
        _coinTween?.Kill();
        _coinTween = DOVirtual.Int(from, newTotal, 0.35f, val =>
        {
            _displayedCoins = val;
            if (coinText != null) coinText.text = val.ToString();
        }).SetUpdate(true).SetLink(gameObject);

        coinText.transform.DOKill();
        coinText.transform.localScale = _coinTextScale;
        coinText.transform.DOPunchScale(_coinTextScale * 0.16f, 0.22f, 2, 0.5f).SetUpdate(true).SetLink(gameObject);
    }

    private void OnSettingsClicked()
    {
        UIManager.Instance?.OpenSettings(inGameplay: false);
    }

    private void OnBackToGameplayClicked()
    {
        AppFlowController.Instance?.ReturnFromShopOverlay();
    }
}
