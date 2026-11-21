using UnityEngine;

/// <summary>
/// Orchestrates top-level screen transitions between Loading, MainHub (Home/Shop/Collection), and Gameplay.
/// </summary>
public class AppFlowController : MonoBehaviour
{
    public static AppFlowController Instance { get; private set; }

    [Header("Screen Canvases (Wired in Scene)")]
    [SerializeField] private LoadingScreenController loadingScreen;
    [SerializeField] private MainHubController mainHub;
    [SerializeField] private HUDController hudController;
    [SerializeField] private GameObject gameplayWorldRoot;

    public AppScreen CurrentScreen { get; private set; } = AppScreen.Loading;
    public bool IsGameplayActive => CurrentScreen == AppScreen.Gameplay;

    private bool _shopOpenedFromGameplay;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void StartAppFlow()
    {
        CurrentScreen = AppScreen.Loading;
        _shopOpenedFromGameplay = false;

        if (mainHub != null) mainHub.HideImmediate();
        if (hudController != null) hudController.gameObject.SetActive(false);
        if (gameplayWorldRoot != null) gameplayWorldRoot.SetActive(false);

        if (loadingScreen != null)
        {
            loadingScreen.PlayLoading(() => EnterHome(HubTab.Home));
        }
        else
        {
            EnterHome(HubTab.Home);
        }
    }

    public void EnterHome(HubTab initialTab = HubTab.Home)
    {
        _shopOpenedFromGameplay = false;
        CurrentScreen = initialTab switch
        {
            HubTab.Shop => AppScreen.Shop,
            HubTab.Collection => AppScreen.Collection,
            _ => AppScreen.Home
        };

        UIManager.Instance?.HideAllPopups();
        if (hudController != null) hudController.gameObject.SetActive(false);
        if (gameplayWorldRoot != null) gameplayWorldRoot.SetActive(false);

        GameManager.Instance?.EnterMenuState();

        if (mainHub != null)
        {
            mainHub.ShowHub(initialTab, showBackToGameplay: false);
        }
    }

    public void EnterGameplay()
    {
        _shopOpenedFromGameplay = false;
        CurrentScreen = AppScreen.Gameplay;

        UIManager.Instance?.HideAllPopups();
        if (mainHub != null) mainHub.HideHub();
        if (gameplayWorldRoot != null) gameplayWorldRoot.SetActive(true);
        if (hudController != null)
        {
            hudController.gameObject.SetActive(true);
            hudController.RefreshAll();
        }

        GameManager.Instance?.EnterGameplay();
    }

    public void OpenShopFromGameplay()
    {
        if (CurrentScreen != AppScreen.Gameplay || mainHub == null) return;
        _shopOpenedFromGameplay = true;
        GameInputManager.SetLock(true);
        mainHub.ShowHub(HubTab.Shop, showBackToGameplay: true);
    }

    public void ReturnFromShopOverlay()
    {
        if (!_shopOpenedFromGameplay)
        {
            EnterHome(HubTab.Home);
            return;
        }

        _shopOpenedFromGameplay = false;
        CurrentScreen = AppScreen.Gameplay;
        if (mainHub != null) mainHub.HideHub();
        if (hudController != null) hudController.RefreshCoinImmediate();
        GameInputManager.SetLock(false);
    }

    public void OnHubTabChanged(HubTab tab)
    {
        if (_shopOpenedFromGameplay) return;
        CurrentScreen = tab switch
        {
            HubTab.Shop => AppScreen.Shop,
            HubTab.Collection => AppScreen.Collection,
            _ => AppScreen.Home
        };
    }
}
