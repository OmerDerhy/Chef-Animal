using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing; 
using UnityEngine.Advertisements; 
using System; 
using TMPro; 

public class CurrencyShop : MonoBehaviour, IStoreListener, IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
{
    public static CurrencyShop Instance;

    private static IStoreController m_StoreController;          
    private static IExtensionProvider m_StoreExtensionProvider; 

    // --- IAP CONFIGURATION ---
    public const string ID_GEMS_250 = "gems_250";
    public const string ID_GEMS_600 = "gems_600";
    public const string ID_GEMS_2000 = "gems_2000";

    // --- ADS CONFIGURATION ---
    [Header("Unity Ads Settings")]
    [Tooltip("Get this from Unity Dashboard")]
    [SerializeField] string _androidGameId = "6007959"; 
    [Tooltip("Get this from Unity Dashboard")]
    [SerializeField] string _iOSGameId = "6007958";     
    [SerializeField] bool _testMode = false; 
    [SerializeField] string _androidAdUnitId = "Rewarded_Android";
    [SerializeField] string _iOSAdUnitId = "Rewarded_iOS";
    
    [Header("Interstitial Ads")]
    [SerializeField] string _androidInterstitialId = "Interstitial_Android";
    [SerializeField] string _iOSInterstitialId = "Interstitial_iOS";
    private string _interstitialAdUnitId;
    public System.Action onInterstitialClosed;

    private string _adUnitId; 
    private enum AdRewardType { None, Coins, Gems, Lights, MysteryGift }
    private AdRewardType _pendingReward = AdRewardType.None;
    private System.Action _pendingMysteryGiftCallback;
    private bool _isAdLoaded = false;

    // --- LOGIC FLAGS ---
    private static bool isPurchaseInProgress = false; 
    private float lastClickTime = 0f;          

    [Header("UI References")]
    public GameObject shopPanel;
    public Button closeButton;

    [Header("Confirmation Pop-up")]
    [Tooltip("The panel that appears to ask 'Are you sure?'")]
    public GameObject confirmPanel;
    [Tooltip("Text to display the cost/reward")]
    public TextMeshProUGUI confirmText; 
    public Button confirmYesButton;
    public Button confirmNoButton;

    // --- OUT OF LIGHTS POPUP ---
    [Header("Out Of Lights Popup")]
    [Tooltip("Panel that opens when player tries to spawn food with 0 lights")]
    public GameObject outOfLightsPanel;
    [Tooltip("Button to buy 250 Lights for 25 Gems")]
    public Button buyLightsBtn;
    [Tooltip("Button to watch an Ad to get full Lights")]
    public Button watchAdForLightsBtn;
    public Button closeLightsBtn;

    // Internal variables to store the pending transaction
    private int pendingGemCost = 0;
    private int pendingCoinReward = 0;

    [Header("Gem to Coin Exchange Buttons")]
    public Button exchange50GemsBtn;   
    public Button exchange140GemsBtn;  
    public Button exchange320GemsBtn;  

    [Header("IAP Gem Buttons")]
    public Button iap250GemsBtn; 
    public Button iap600GemsBtn; 
    public Button iap2000GemsBtn; 
    
    [Header("Restore Purchase")]
    public Button restorePurchasesBtn; 

    [Header("Ad Rewards Settings")]
    private int adCoinRewardAmount = 50; 
    [Tooltip("Amount of Gems rewarded for watching an ad")]
    private int adGemRewardAmount = 10;

    [Header("Ad Buttons")]
    public Button watchAdCoinsBtn; 
    public Button watchAdGemsBtn;  

    [Header("Debug / Test Buttons")]
    public Button buy1000CoinsBtn; 
    public Button buy100GemsBtn;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else 
        {
            Destroy(gameObject);
            return;
        }

        if (shopPanel != null) shopPanel.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (outOfLightsPanel != null) outOfLightsPanel.SetActive(false);

        // --- BUTTON SETUP ---
        SetupBtn(closeButton, CloseShop);
        
        SetupBtn(confirmYesButton, ProcessGemExchange);
        SetupBtn(confirmNoButton, CancelGemExchange);

        SetupBtn(exchange50GemsBtn, () => AskForConfirmation(50, 500));
        SetupBtn(exchange140GemsBtn, () => AskForConfirmation(140, 1500));
        SetupBtn(exchange320GemsBtn, () => AskForConfirmation(320, 5000));

        SetupBtn(iap250GemsBtn, () => BuyProductID(ID_GEMS_250));
        SetupBtn(iap600GemsBtn, () => BuyProductID(ID_GEMS_600));
        SetupBtn(iap2000GemsBtn, () => BuyProductID(ID_GEMS_2000));
        SetupBtn(restorePurchasesBtn, RestorePurchases);

        SetupBtn(watchAdCoinsBtn, WatchAdForCoins);
        SetupBtn(watchAdGemsBtn, WatchAdForGems);

        SetupBtn(buyLightsBtn, BuyLightsWithGems);
        SetupBtn(watchAdForLightsBtn, WatchAdForLights);
        SetupBtn(closeLightsBtn, CloseLightsPopup);

        SetupBtn(buy1000CoinsBtn, () => BuyCoins(1000));
        SetupBtn(buy100GemsBtn, () => BuyGems(100));

        InitializePurchasing();
        InitializeAds();
    }

    private void SetupBtn(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners(); 
            btn.onClick.AddListener(() => 
            {
                if (Time.time - lastClickTime > 0.5f) 
                {
                    lastClickTime = Time.time;
                    action.Invoke();
                }
            });
        }
    }

    public void OpenShop()
    {
        if (shopPanel != null) 
        {
            shopPanel.SetActive(true);
            shopPanel.transform.SetAsLastSibling(); 
        }
        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (outOfLightsPanel != null) outOfLightsPanel.SetActive(false);
    }

    public void CloseShop()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
    }

    public void ShowOutOfLights()
    {
        if (outOfLightsPanel != null) 
        {
            outOfLightsPanel.SetActive(true);
            outOfLightsPanel.transform.SetAsLastSibling(); 
            
            if (watchAdForLightsBtn != null)
            {
                watchAdForLightsBtn.gameObject.SetActive(true); // Always ensure it's visible
                
                if (PlayerData.Instance != null && PlayerData.Instance.GetTimeSinceLastAdForLights().TotalHours >= 12f)
                {
                    watchAdForLightsBtn.interactable = true;
                }
                else
                {
                    watchAdForLightsBtn.interactable = false; // Makes it unpressable and automatically tints it darker
                }
            }
        }
    }

    public void CloseLightsPopup()
    {
        if (outOfLightsPanel != null) outOfLightsPanel.SetActive(false);
    }

    public void BuyLightsWithGems()
    {
        if (PlayerData.Instance != null)
        {
            if (PlayerData.Instance.SpendGems(25))
            {
                PlayerData.Instance.AddLights(250);
                Debug.Log("Purchased 250 Lights for 25 Gems!");
                PlayPremiumPurchaseSound();
                
                if (ShopManager.Instance != null && buyLightsBtn != null)
                {
                    ShopManager.Instance.SpawnFlyingLights(buyLightsBtn.transform.position);
                }
                
                CloseLightsPopup();
            }
            else
            {
                Debug.Log("Not enough Gems to buy Lights! Opening Shop.");
                // --- NEW: Open main shop if afford lights ---
                OpenShop();
                CloseLightsPopup();
            }
        }
    }

    // --------------------------------------------------------------------
    // CONFIRMATION LOGIC
    // --------------------------------------------------------------------

    public void AskForConfirmation(int gemCost, int coinReward)
    {
        if (PlayerData.Instance != null)
        {
            if (PlayerData.Instance.GetGems() < gemCost)
            {
                Debug.Log("Not enough Gems to exchange! Opening Shop.");
                // --- NEW: Open Shop to buy gems ---
                OpenShop();
                return;
            }
        }

        pendingGemCost = gemCost;
        pendingCoinReward = coinReward;

        if (confirmText != null)
        {
            confirmText.text = $"Spend {gemCost} Gems to get {coinReward} Coins?";
        }

        if (confirmPanel != null) 
        {
            confirmPanel.SetActive(true);
            confirmPanel.transform.SetAsLastSibling(); 
        }
    }

    public void ProcessGemExchange()
    {
        if (PlayerData.Instance != null)
        {
            if (PlayerData.Instance.SpendGems(pendingGemCost))
            {
                PlayerData.Instance.AddCoins(pendingCoinReward);
                Debug.Log($"Exchanged {pendingGemCost} Gems for {pendingCoinReward} Coins!");
                PlayPremiumPurchaseSound();
            }
        }
        CancelGemExchange();
    }

    public void CancelGemExchange()
    {
        if (confirmPanel != null) confirmPanel.SetActive(false);
        pendingGemCost = 0;
        pendingCoinReward = 0;
    }

    // --------------------------------------------------------------------
    // UNITY ADS LOGIC
    // --------------------------------------------------------------------

    public void InitializeAds()
    {
        #if UNITY_IOS
            _adUnitId = _iOSAdUnitId;
            _interstitialAdUnitId = _iOSInterstitialId;
            string gameId = _iOSGameId;
        #elif UNITY_ANDROID
            _adUnitId = _androidAdUnitId;
            _interstitialAdUnitId = _androidInterstitialId;
            string gameId = _androidGameId;
        #else
            _adUnitId = _androidAdUnitId; 
            _interstitialAdUnitId = _androidInterstitialId;
            string gameId = _androidGameId;
        #endif

        if (string.IsNullOrEmpty(gameId) || gameId == "1234567")
        {
            Debug.LogError("Unity Ads: GAME ID is missing or default!");
            return;
        }

        if (!Advertisement.isInitialized && Advertisement.isSupported)
        {
            Advertisement.Initialize(gameId, _testMode, this);
        }
        else
        {
            LoadAd();
        }
    }

    public void OnInitializationComplete()
    {
        Debug.Log("Unity Ads Initialized.");
        LoadAd();
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.Log($"Unity Ads Initialization Failed: {error} - {message}");
    }

    public void LoadAd()
    {
        Debug.Log("Loading Ad: " + _adUnitId);
        Advertisement.Load(_adUnitId, this);
        Advertisement.Load(_interstitialAdUnitId, this);
    }

    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        Debug.Log("Ad Loaded: " + adUnitId);
        if (adUnitId == _adUnitId)
        {
            _isAdLoaded = true;
        }
    }

    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
        Debug.Log($"Error loading Ad Unit: {adUnitId} - {error} - {message}");
        if (adUnitId == _adUnitId)
        {
            _isAdLoaded = false;
        }
    }

    public void WatchAdForCoins()
    {
        _pendingReward = AdRewardType.Coins;
        ShowAd();
    }

    public void WatchAdForGems()
    {
        _pendingReward = AdRewardType.Gems;
        ShowAd();
    }

    public void WatchAdForLights()
    {
        _pendingReward = AdRewardType.Lights;
        ShowAd();
    }

    public void WatchAdForMysteryGift(System.Action onSuccess)
    {
        _pendingMysteryGiftCallback = onSuccess;
        _pendingReward = AdRewardType.MysteryGift;
        ShowAd();
    }

    private void ShowAd()
    {
        if (!_isAdLoaded)
        {
            Debug.LogWarning("Ad not ready yet. Attempting to load...");
            LoadAd();
            return;
        }

        Debug.Log("Showing Ad: " + _adUnitId);
        Advertisement.Show(_adUnitId, this);
        _isAdLoaded = false; 
    }

    public void ShowInterstitialAd(System.Action onClosed)
    {
        onInterstitialClosed = onClosed;
        
        if (Advertisement.isInitialized)
        {
            Debug.Log("Showing Interstitial Ad: " + _interstitialAdUnitId);
            Advertisement.Show(_interstitialAdUnitId, this);
        }
        else
        {
            Debug.LogWarning("Ads not initialized, skipping interstitial.");
            onClosed?.Invoke();
            onInterstitialClosed = null;
        }
    }

    public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
    {
        if (adUnitId.Equals(_interstitialAdUnitId))
        {
            onInterstitialClosed?.Invoke();
            onInterstitialClosed = null;
            Advertisement.Load(_interstitialAdUnitId, this);
            return;
        }

        if (adUnitId.Equals(_adUnitId) && showCompletionState.Equals(UnityAdsShowCompletionState.COMPLETED))
        {
            Debug.Log("Unity Ads Rewarded Ad Completed");
            
            if (_pendingReward == AdRewardType.Coins)
            {
                if (PlayerData.Instance != null) PlayerData.Instance.AddCoins(adCoinRewardAmount);
            }
            else if (_pendingReward == AdRewardType.Gems)
            {
                if (PlayerData.Instance != null) PlayerData.Instance.AddGems(adGemRewardAmount);
            }
            else if (_pendingReward == AdRewardType.Lights)
            {
                if (PlayerData.Instance != null) 
                {
                    PlayerData.Instance.AddLights(PlayerData.MAX_LIGHTS);
                    PlayerData.Instance.ClaimAdForLights();
                }
                CloseLightsPopup();
                
                if (ShopManager.Instance != null && watchAdForLightsBtn != null)
                {
                    ShopManager.Instance.SpawnFlyingLights(watchAdForLightsBtn.transform.position);
                }
            }
            else if (_pendingReward == AdRewardType.MysteryGift)
            {
                if (PlayerData.Instance != null) PlayerData.Instance.ClaimMysteryGiftAd();

                System.Action callback = _pendingMysteryGiftCallback;
                _pendingMysteryGiftCallback = null;
                callback?.Invoke();
            }
        }

        _pendingReward = AdRewardType.None;
        Advertisement.Load(_adUnitId, this);
    }

    public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
    {
        Debug.Log($"Error showing Ad Unit {adUnitId}: {error} - {message}");

        if (adUnitId.Equals(_interstitialAdUnitId))
        {
            onInterstitialClosed?.Invoke();
            onInterstitialClosed = null;
            Advertisement.Load(_interstitialAdUnitId, this);
            return;
        }

        _pendingMysteryGiftCallback = null;
        _pendingReward = AdRewardType.None;
        Advertisement.Load(_adUnitId, this);
    }

    public void OnUnityAdsShowStart(string adUnitId) { }
    public void OnUnityAdsShowClick(string adUnitId) { }

    // --------------------------------------------------------------------
    // IAP LOGIC
    // --------------------------------------------------------------------

    public void InitializePurchasing()
    {
        if (IsInitialized()) return;

        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        builder.AddProduct(ID_GEMS_250, ProductType.Consumable);
        builder.AddProduct(ID_GEMS_600, ProductType.Consumable);
        builder.AddProduct(ID_GEMS_2000, ProductType.Consumable);

        UnityPurchasing.Initialize(this, builder);
    }

    private bool IsInitialized()
    {
        return m_StoreController != null && m_StoreExtensionProvider != null;
    }

    void BuyProductID(string productId)
    {
        if (isPurchaseInProgress) 
        {
            Debug.LogWarning("Purchase already in progress. Ignoring click.");
            return;
        }

        if (IsInitialized())
        {
            Product product = m_StoreController.products.WithID(productId);

            if (product != null && product.availableToPurchase)
            {
                Debug.Log($"Purchasing product asynchronously: '{product.definition.id}'");
                isPurchaseInProgress = true;
                m_StoreController.InitiatePurchase(product);
            }
            else
            {
                Debug.Log("BuyProductID: FAIL. Product not found or not available.");
            }
        }
        else
        {
            Debug.Log("BuyProductID FAIL. Not initialized.");
        }
    }

    public void RestorePurchases()
    {
        if (!IsInitialized())
        {
            Debug.Log("RestorePurchases FAIL. Not initialized.");
            return;
        }

        if (Application.platform == RuntimePlatform.IPhonePlayer || 
            Application.platform == RuntimePlatform.OSXPlayer)
        {
            Debug.Log("Restoring Purchases started ...");

            var apple = m_StoreExtensionProvider.GetExtension<IAppleExtensions>();
            apple.RestoreTransactions((result, error) => {
                Debug.Log("RestorePurchases continuing: " + result + ". If no further messages, no purchases available to restore.");
            });
        }
        else
        {
            Debug.Log("RestorePurchases FAIL. Not supported on this platform. Current = " + Application.platform);
        }
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        Debug.Log("OnInitialized: PASS");
        m_StoreController = controller;
        m_StoreExtensionProvider = extensions;
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.Log("OnInitializeFailed InitializationFailureReason:" + error);
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.Log($"OnInitializeFailed: {error}, Message: {message}");
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        isPurchaseInProgress = false;

        if (String.Equals(args.purchasedProduct.definition.id, ID_GEMS_250, StringComparison.Ordinal))
        {
            if (PlayerData.Instance != null) PlayerData.Instance.AddGems(250); 
            PlayPremiumPurchaseSound();
        }
        else if (String.Equals(args.purchasedProduct.definition.id, ID_GEMS_600, StringComparison.Ordinal))
        {
            if (PlayerData.Instance != null) PlayerData.Instance.AddGems(600);
            PlayPremiumPurchaseSound();
        }
        else if (String.Equals(args.purchasedProduct.definition.id, ID_GEMS_2000, StringComparison.Ordinal))
        {
            if (PlayerData.Instance != null) PlayerData.Instance.AddGems(2000);
            PlayPremiumPurchaseSound();
        }
        else
        {
            Debug.Log($"ProcessPurchase: FAIL. Unrecognized product: '{args.purchasedProduct.definition.id}'");
        }

        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        isPurchaseInProgress = false;
        Debug.Log($"OnPurchaseFailed: FAIL. Product: '{product.definition.storeSpecificId}', PurchaseFailureReason: {failureReason}");
    }

    public void BuyCoins(int amount)
    {
        if (PlayerData.Instance != null)
        {
            PlayerData.Instance.AddCoins(amount);
            Debug.Log($"Purchased {amount} Coins (Debug)!");
        }
    }

    public void BuyGems(int amount)
    {
        if (PlayerData.Instance != null)
        {
            PlayerData.Instance.AddGems(amount);
            Debug.Log($"Purchased {amount} Gems (Debug)!");
            PlayPremiumPurchaseSound();
        }
    }

    private void PlayPremiumPurchaseSound()
    {
        AudioClip sound = Resources.Load<AudioClip>("Magic Score 9");
        if (sound != null)
        {
            Vector3 playPos = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(sound, playPos, 1.0f);
        }
    }
}