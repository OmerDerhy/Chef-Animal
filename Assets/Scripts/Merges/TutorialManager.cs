using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("UI References")]
    [Tooltip("A UI Panel with a black, semi-transparent Image (e.g. Alpha 200) that covers the whole screen.")]
    public GameObject darkOverlay;

    [Header("Gift Reward")]
    [Tooltip("The Giant Gift object in the scene (initially inactive).")]
    public GameObject giantGift;

    [Header("Giraffe Tutorial")]
    [Tooltip("The Panel containing the text 'Hurry the giraffe viewer is here...'.")]
    public GameObject giraffeTutorialPanel;
    [Tooltip("The Button inside the panel that closes the tutorial (e.g. 'OK' or 'Got it').")]
    public Button giraffeCloseButton;

    [Header("Cookie Time Tutorial")]
    [Tooltip("The Panel explaining Cookie Time.")]
    public GameObject cookieTimeTutorialPanel;
    [Tooltip("The Button inside the panel that closes it.")]
    public Button cookieTimeCloseButton;
    [Tooltip("The actual Cookie Time Button in the UI (to lock/unlock).")]
    public GameObject cookieTimeButtonObj;

    // Keys to track which tutorials have been shown
    private const string TUTORIAL_KEY = "FirstTimeTutorial_Completed";
    private const string BUY_CHAR_KEY = "Tutorial_BuyChar_Completed";
    private const string GIRAFFE_TUTORIAL_KEY = "Tutorial_Giraffe_Completed"; 
    private const string GIFT_OPENED_KEY = "Tutorial_GiftOpened_Completed"; 

    private int _knownCharSlots;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (giraffeTutorialPanel != null) giraffeTutorialPanel.SetActive(false);
        if (cookieTimeTutorialPanel != null) cookieTimeTutorialPanel.SetActive(false);

        // Auto-wire the close buttons
        if (giraffeCloseButton != null)
        {
            giraffeCloseButton.onClick.RemoveAllListeners();
            giraffeCloseButton.onClick.AddListener(CloseGiraffeTutorial);
        }
        if (cookieTimeCloseButton != null)
        {
            cookieTimeCloseButton.onClick.RemoveAllListeners();
            cookieTimeCloseButton.onClick.AddListener(CloseCookieTimeTutorial);
        }

        // Initialize known slots count
        if (PlayerData.Instance != null)
        {
            _knownCharSlots = PlayerData.Instance.GetCharacterCount();
            PlayerData.Instance.OnCoinsChanged += CheckPurchaseTutorials;
        }

        // --- ENFORCE BLOCKING LAYERS ---
        SetupOverlayBlocking();

        // --- CHECK GIFT / COOKIE TIME STATE ---
        if (PlayerPrefs.GetInt(GIFT_OPENED_KEY, 0) == 1)
        {
            if (cookieTimeButtonObj != null) cookieTimeButtonObj.SetActive(true);
        }
        else
        {
            if (cookieTimeButtonObj != null) cookieTimeButtonObj.SetActive(false);
        }

        // 1. Check if the basic intro tutorial is already done
        if (PlayerPrefs.GetInt(TUTORIAL_KEY, 0) == 1)
        {
            if (darkOverlay != null) darkOverlay.SetActive(false);
            
            if (PlayerData.Instance != null) CheckPurchaseTutorials(PlayerData.Instance.GetCoins());
            return;
        }

        // 2. If not, Start Intro Tutorial
        StartIntroTutorial();
    }

    void OnDestroy()
    {
        if (PlayerData.Instance != null)
        {
            PlayerData.Instance.OnCoinsChanged -= CheckPurchaseTutorials;
        }
    }

    void Update()
    {
        if (PlayerData.Instance != null)
        {
            int currentSlots = PlayerData.Instance.GetCharacterCount();
            if (currentSlots > _knownCharSlots)
            {
                _knownCharSlots = currentSlots;
                if (currentSlots == 2)
                {
                    ActivateGift();
                }
            }
        }
    }

    // --- SETUP BLOCKING ---
    private void SetupOverlayBlocking()
    {
        if (darkOverlay != null)
        {
            // 1. Add Canvas to ensure it draws on top of normal UI (Order 29000)
            Canvas c = darkOverlay.GetComponent<Canvas>();
            if (c == null) c = darkOverlay.AddComponent<Canvas>();
            c.overrideSorting = true;
            c.sortingOrder = 29000; 

            // 2. Add Raycaster to intercept clicks
            if (darkOverlay.GetComponent<GraphicRaycaster>() == null)
                darkOverlay.AddComponent<GraphicRaycaster>();

            // 3. Ensure Image blocks raycasts
            Image img = darkOverlay.GetComponent<Image>();
            if (img != null) img.raycastTarget = true;
        }
    }

    // --- NEW: GIFT & COOKIE TIME LOGIC ---
    
    // Call this from your GiftBox script when it Explodes!
    public void OnGiftOpened()
    {
        if (PlayerPrefs.GetInt(GIFT_OPENED_KEY, 0) == 1) return;

        // Set Flag
        PlayerPrefs.SetInt(GIFT_OPENED_KEY, 1);
        PlayerPrefs.Save();

        // Unlock Cookie Time Button
        if (cookieTimeButtonObj != null) cookieTimeButtonObj.SetActive(true);

        // --- NEW: Make it instantly available ---
        CookieTime ct = FindObjectOfType<CookieTime>();
        if (ct != null) ct.ForceReady();

        // Show Explanation
        ShowCookieTimeTutorial();
    }

    public bool IsGiraffeAllowed()
    {
        return PlayerPrefs.GetInt(GIFT_OPENED_KEY, 0) == 1;
    }

    public void ShowCookieTimeTutorial()
    {
        if (cookieTimeTutorialPanel != null)
        {
            if (darkOverlay != null) darkOverlay.SetActive(true);
            cookieTimeTutorialPanel.SetActive(true);
            
            Canvas c = cookieTimeTutorialPanel.GetComponent<Canvas>();
            if (c == null) c = cookieTimeTutorialPanel.AddComponent<Canvas>();
            c.overrideSorting = true;
            c.sortingOrder = 30000;
            if (cookieTimeTutorialPanel.GetComponent<GraphicRaycaster>() == null)
                cookieTimeTutorialPanel.AddComponent<GraphicRaycaster>();

            Time.timeScale = 0f;
        }
    }

    public void CloseCookieTimeTutorial()
    {
        if (cookieTimeTutorialPanel != null) cookieTimeTutorialPanel.SetActive(false);
        if (darkOverlay != null) darkOverlay.SetActive(false);
        Time.timeScale = 1f;
    }

    // --- GIRAFFE TUTORIAL ---
    public void ShowGiraffeTutorial()
    {
        // Only show once
        if (PlayerPrefs.GetInt(GIRAFFE_TUTORIAL_KEY, 0) == 1) return;

        if (giraffeTutorialPanel != null)
        {
            Debug.Log("[Tutorial] Showing Giraffe Tutorial - Pausing Game");
            
            if (darkOverlay != null) darkOverlay.SetActive(true);
            
            giraffeTutorialPanel.SetActive(true);
            
            // Pop the panel above the overlay
            Canvas c = giraffeTutorialPanel.GetComponent<Canvas>();
            if (c == null) c = giraffeTutorialPanel.AddComponent<Canvas>();
            c.overrideSorting = true;
            c.sortingOrder = 30000; 

            if (giraffeTutorialPanel.GetComponent<GraphicRaycaster>() == null)
                giraffeTutorialPanel.AddComponent<GraphicRaycaster>();
            
            // --- PAUSE GAME TIME ---
            Time.timeScale = 0f;
        }
    }
public void TriggerCookieTimeUnlocked()
{
    // 1. Activate the Dark Overlay
    if (darkOverlay != null) darkOverlay.SetActive(true);

    // 2. Add Canvas & Raycaster to pop the button above the overlay
    Canvas buttonCanvas = cookieTimeButtonObj.GetComponent<Canvas>();
    if (buttonCanvas == null) buttonCanvas = cookieTimeButtonObj.AddComponent<Canvas>();
    
    buttonCanvas.overrideSorting = true;
    buttonCanvas.sortingOrder = 30000;

    // We need a GraphicRaycaster so the button can still receive clicks
    if (cookieTimeButtonObj.GetComponent<GraphicRaycaster>() == null)
    {
        cookieTimeButtonObj.AddComponent<GraphicRaycaster>();
    }

    // 3. Add a listener to handle the click
    Button btn = cookieTimeButtonObj.GetComponent<Button>();
    if (btn != null)
    {
        // Ensure we don't stack multiple listeners if this is called twice
        btn.onClick.RemoveListener(OnCookieTimeClicked); 
        btn.onClick.AddListener(OnCookieTimeClicked);
    }
}

// This method runs when the button is clicked
private void OnCookieTimeClicked()
{
    // 1. Hide the overlay
    if (darkOverlay != null) darkOverlay.SetActive(false);

    // 3. Remove this specific listener so it doesn't run again unnecessarily
    Button btn = cookieTimeButtonObj.GetComponent<Button>();
    if (btn != null) btn.onClick.RemoveListener(OnCookieTimeClicked);
    
    Debug.Log("Overlay closed and button restored!");
}

    public void CloseGiraffeTutorial()
    {
        if (giraffeTutorialPanel != null) giraffeTutorialPanel.SetActive(false);
        if (darkOverlay != null) darkOverlay.SetActive(false);
        
        // --- RESUME GAME TIME ---
        Time.timeScale = 1f;

        // Save completion
        PlayerPrefs.SetInt(GIRAFFE_TUTORIAL_KEY, 1);
        PlayerPrefs.Save();
    }

    // --- GIFT LOGIC ---
    private void ActivateGift()
    {
        if (giantGift != null && !giantGift.activeSelf)
        {
            Debug.Log("[TutorialManager] First Character Slot purchased! Activating Giant Gift.");
            giantGift.SetActive(true);
            
            GiftBox giftScript = giantGift.GetComponent<GiftBox>();
            if (giftScript != null) giftScript.ResetGift();
        }
    }

    // --- INTRO TUTORIAL ---
    void StartIntroTutorial()
    {
        if (darkOverlay != null) darkOverlay.SetActive(true);

        FoodSource[] sources = FindObjectsOfType<FoodSource>();
        FoodSource firstSource = null;

        foreach (var source in sources)
        {
            if (source.sourceID == 0)
            {
                firstSource = source;
                break;
            }
        }

        if (firstSource != null)
        {
            HighlightObject(firstSource.gameObject, TUTORIAL_KEY);
        }
        else
        {
            CompleteTutorial(TUTORIAL_KEY);
        }
    }

    // --- PURCHASE TUTORIALS ---
    private void CheckPurchaseTutorials(int coins)
    {
        if (darkOverlay != null && darkOverlay.activeSelf) return;

        if (PlayerPrefs.GetInt(BUY_CHAR_KEY, 0) == 0)
        {
            CharacterSpawner[] spawners = FindObjectsOfType<CharacterSpawner>();
            foreach (var spawner in spawners)
            {
                bool isUnlocked = PlayerData.Instance.IsCharacterSlotUnlocked(spawner.spawnerID);
                if (!isUnlocked)
                {
                    bool prevUnlocked = (spawner.spawnerID == 0) || PlayerData.Instance.IsCharacterSlotUnlocked(spawner.spawnerID - 1);
                    if (prevUnlocked && coins >= spawner.unlockPrice)
                    {
                        if (spawner.shoppingCartButtonObj != null)
                        {
                            StartHighlightSequence(spawner.shoppingCartButtonObj, BUY_CHAR_KEY);
                            return;
                        }
                    }
                }
            }
        }
    }

    private void StartHighlightSequence(GameObject targetButton, string saveKey)
    {
        if (darkOverlay != null) darkOverlay.SetActive(true);
        HighlightObject(targetButton, saveKey);
    }

    private void HighlightObject(GameObject targetObj, string completionKey)
    {
        Canvas canvas = targetObj.GetComponent<Canvas>();
        if (canvas == null) canvas = targetObj.AddComponent<Canvas>();
        
        canvas.overrideSorting = true;
        canvas.sortingOrder = 30000; 

        if (targetObj.GetComponent<GraphicRaycaster>() == null)
        {
            targetObj.AddComponent<GraphicRaycaster>();
        }

        Button btn = targetObj.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(() => OnTargetClicked(targetObj, completionKey));
        }
    }

    private void OnTargetClicked(GameObject targetObj, string completionKey)
    {
        GraphicRaycaster gr = targetObj.GetComponent<GraphicRaycaster>();
        if (gr != null) Destroy(gr);

        Canvas c = targetObj.GetComponent<Canvas>();
        if (c != null) Destroy(c);

        if (completionKey == BUY_CHAR_KEY)
        {
            if (ShopManager.Instance != null && ShopManager.Instance.buyButton != null)
            {
                GameObject buyBtnObj = ShopManager.Instance.buyButton.gameObject;
                
                if (targetObj != buyBtnObj)
                {
                    HighlightObject(buyBtnObj, completionKey);
                    return; 
                }
            }
        }

        CompleteTutorial(completionKey);
    }

    public void CompleteTutorial(string key)
    {
        if (darkOverlay != null) darkOverlay.SetActive(false);
        
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
        
        Debug.Log($"[Tutorial] Completed: {key}");
    }
    
    [ContextMenu("Reset All Tutorials")]
    public void ResetTutorial()
    {
        PlayerPrefs.DeleteKey(TUTORIAL_KEY);
        // PlayerPrefs.DeleteKey(BUY_FOOD_KEY);
        PlayerPrefs.DeleteKey(BUY_CHAR_KEY);
        PlayerPrefs.DeleteKey(GIRAFFE_TUTORIAL_KEY);
        PlayerPrefs.DeleteKey(GIFT_OPENED_KEY);
        Debug.Log("All Tutorials Reset! Play again to see them.");
    }
}