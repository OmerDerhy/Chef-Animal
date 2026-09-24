using UnityEngine;
using UnityEngine.SceneManagement; 
using System; 

public class PlayerData : MonoBehaviour
{
    public static PlayerData Instance;

    public event Action<int> OnCoinsChanged;
    public event Action<int> OnGemsChanged; 
    public event Action<int> OnLightsChanged; 

    // Define Max Lights constant for global access
    public const int MAX_LIGHTS = 200;

    [Header("Global Data")]
    [SerializeField] private int coins;
    [SerializeField] private int gems; 
    [SerializeField] private int lights; 
    [SerializeField] private int unlockedLevels = 1; 
    [SerializeField] private bool UnlockedCookieTime = false; // Tracks if the specific Cookie Time event happened
    [SerializeField] private bool TutorialGiftOpened = false; // Tracks if the Level 1 gift was opened

    [Header("Regeneration")]
    [Tooltip("Time in seconds to regenerate 1 light")]
    public float lightRegenTime = 30f;
    [SerializeField] private float currentRegenTimer = 0f;

    // Keys for saving data
    private const string KEY_COINS = "Save_Coins";
    private const string KEY_GEMS = "Save_Gems";
    private const string KEY_LIGHTS = "Save_Lights"; 
    private const string KEY_LEVELS = "Save_UnlockedLevels"; 
    private const string KEY_COOKIE_UNLOCKED = "Save_CookieTimeUnlocked"; 
    private const string KEY_TUTORIAL_GIFT = "Save_TutorialGiftOpened"; // <--- NEW KEY
    private const string KEY_LAST_LOGIN = "Save_LastLoginTime";
    private const string KEY_REGEN_TIMER = "Save_RegenTimer";
    private const string KEY_DAILY_GIFT_TIME = "Save_LastDailyGiftTime"; // <--- DAILY GIFT KEY
    private const string KEY_AD_LIGHTS_TIME = "Save_LastAdForLightsTime"; // <--- AD FOR LIGHTS KEY
    private const string KEY_AD_MYSTERY_GIFT_TIME = "Save_LastAdForMysteryGiftTime"; // <--- AD FOR MYSTERY GIFT KEY
    private const string KEY_MAP_DATA = "Save_MapData"; // Map save string
    
    public void SaveMapData(string json)
    {
        PlayerPrefs.SetString(KEY_MAP_DATA, json);
        PlayerPrefs.Save();
    }

    public string LoadMapData()
    {
        return PlayerPrefs.GetString(KEY_MAP_DATA, "");
    }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
            LoadData(); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (lights >= MAX_LIGHTS) 
        {
            currentRegenTimer = 0f;
            return;
        }

        currentRegenTimer += Time.deltaTime;

        if (currentRegenTimer >= lightRegenTime)
        {
            AddLights(1); 
            currentRegenTimer = 0f; // Reset timer
        }
    }

    // --- COOKIE TIME STATE ---
    public bool IsCookieTimeUnlocked()
    {
        return UnlockedCookieTime;
    }

    public void UnlockCookieTime()
    {
        if (!UnlockedCookieTime)
        {
            UnlockedCookieTime = true;
            SaveGlobalData(); // Save immediately when state changes
        }
    }

    // --- TUTORIAL GIFT STATE ---
    public bool IsTutorialGiftOpened()
    {
        return TutorialGiftOpened;
    }

    public void UnlockTutorialGift()
    {
        if (!TutorialGiftOpened)
        {
            TutorialGiftOpened = true;
            SaveGlobalData();
        }
    }

    // --- SAVE / LOAD SYSTEM ---
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) SaveGlobalData();
    }

    private void OnApplicationQuit()
    {
        SaveGlobalData();
    }

    // --- CURRENCY LOGIC ---
    public int GetCoins() { return coins; }
    
    public void AddCoins(int amount) 
    { 
        coins += amount; 
        SaveGlobalData(); 
        OnCoinsChanged?.Invoke(coins); 
    }
    
    public bool SpendCoins(int amount) 
    { 
        if (coins >= amount) 
        { 
            coins -= amount; 
            SaveGlobalData(); 
            OnCoinsChanged?.Invoke(coins); 
            return true; 
        } 
        return false; 
    }

    public int GetGems() { return gems; }

    public void AddGems(int amount)
    {
        gems += amount;
        SaveGlobalData();
        OnGemsChanged?.Invoke(gems);
    }

    public bool SpendGems(int amount)
    {
        if (gems >= amount)
        {
            gems -= amount;
            SaveGlobalData();
            OnGemsChanged?.Invoke(gems);
            return true;
        }
        return false;
    }

    public int GetLights() { return lights; }

    public void AddLights(int amount)
    {
        lights += amount;
        if (lights > MAX_LIGHTS) lights = MAX_LIGHTS;
        SaveGlobalData();
        OnLightsChanged?.Invoke(lights);
    }

    public bool SpendLights(int amount)
    {
        if (lights >= amount)
        {
            lights -= amount;
            SaveGlobalData();
            OnLightsChanged?.Invoke(lights);
            return true;
        }
        return false;
    }

    public bool IsLevelUnlocked(int levelIndex)
    {
        return levelIndex <= unlockedLevels;
    }

    public void UnlockLevel(int levelIndex)
    {
        if (levelIndex > unlockedLevels)
        {
            unlockedLevels = levelIndex;
            SaveGlobalData();
        }
    }

    private void SaveGlobalData()
    {
        PlayerPrefs.SetInt(KEY_COINS, coins);
        PlayerPrefs.SetInt(KEY_GEMS, gems);
        PlayerPrefs.SetInt(KEY_LIGHTS, lights);
        PlayerPrefs.SetInt(KEY_LEVELS, unlockedLevels);
        
        // Save the boolean as 1 (true) or 0 (false)
        PlayerPrefs.SetInt(KEY_COOKIE_UNLOCKED, UnlockedCookieTime ? 1 : 0);
        PlayerPrefs.SetInt(KEY_TUTORIAL_GIFT, TutorialGiftOpened ? 1 : 0);

        PlayerPrefs.SetString(KEY_LAST_LOGIN, DateTime.UtcNow.ToBinary().ToString());
        PlayerPrefs.SetFloat(KEY_REGEN_TIMER, currentRegenTimer);

        PlayerPrefs.Save();
    }

    // --- SCENE SPECIFIC DATA ---
    private string GetFoodKey() { return SceneManager.GetActiveScene().name + "_FoodUnlocks"; }
    private string GetCharKey() { return SceneManager.GetActiveScene().name + "_CharUnlocks"; }

    public int GetFoodSupplierCount() { return PlayerPrefs.GetInt(GetFoodKey(), 1); }
    public bool IsFoodSourceUnlocked(int id) { return id < GetFoodSupplierCount(); }
    
    public void UnlockNextFoodSupplier() { 
        PlayerPrefs.SetInt(GetFoodKey(), GetFoodSupplierCount() + 1); 
        PlayerPrefs.Save(); 
        
        // --- ADDED: Reward 25 Lights ---
        AddLights(25);
    }

    public int GetCharacterCount() { return PlayerPrefs.GetInt(GetCharKey(), 1); }
    public bool IsCharacterSlotUnlocked(int id) { return id < GetCharacterCount(); }
    
    public void UnlockNextCharacterSlot() { 
        PlayerPrefs.SetInt(GetCharKey(), GetCharacterCount() + 1); 
        PlayerPrefs.Save(); 
        
        // --- ADDED: Reward 25 Lights ---
        AddLights(25);
    }

    // --- LOAD DATA ---
    private void LoadData()
    {
        coins = PlayerPrefs.GetInt(KEY_COINS, 100000); 
        gems = PlayerPrefs.GetInt(KEY_GEMS, 50);
        lights = PlayerPrefs.GetInt(KEY_LIGHTS, 200); 
        unlockedLevels = PlayerPrefs.GetInt(KEY_LEVELS, 1);
        
        // Load the booleans
        UnlockedCookieTime = PlayerPrefs.GetInt(KEY_COOKIE_UNLOCKED, 0) == 1;
        TutorialGiftOpened = PlayerPrefs.GetInt(KEY_TUTORIAL_GIFT, 0) == 1;

        currentRegenTimer = PlayerPrefs.GetFloat(KEY_REGEN_TIMER, 0f);

        if (lights < MAX_LIGHTS)
        {
            string timeStr = PlayerPrefs.GetString(KEY_LAST_LOGIN, string.Empty);
            if (!string.IsNullOrEmpty(timeStr))
            {
                long binaryTime = Convert.ToInt64(timeStr);
                DateTime lastLogin = DateTime.FromBinary(binaryTime);
                TimeSpan timeAway = DateTime.UtcNow - lastLogin;
                double secondsPassed = timeAway.TotalSeconds;
                double totalSeconds = secondsPassed + currentRegenTimer;
                int lightsEarned = (int)(totalSeconds / lightRegenTime);
                currentRegenTimer = (float)(totalSeconds % lightRegenTime);

                if (lightsEarned > 0)
                {
                    Debug.Log($"[PlayerData] Offline: Earned {lightsEarned} lights.");
                    lights += lightsEarned;
                    if (lights > MAX_LIGHTS) lights = MAX_LIGHTS;
                }
            }
        }

        OnCoinsChanged?.Invoke(coins);
        OnGemsChanged?.Invoke(gems);
        OnLightsChanged?.Invoke(lights);
    }

    // --- DAILY GIFT TIMING LOGIC ---
    public TimeSpan GetTimeSinceLastDailyGift()
    {
        string timeStr = PlayerPrefs.GetString(KEY_DAILY_GIFT_TIME, string.Empty);
        
        if (string.IsNullOrEmpty(timeStr))
        {
            // If never claimed, return a time span larger than the cooldown so it's instantly available
            return TimeSpan.FromHours(24);
        }

        try
        {
            long binaryTime = Convert.ToInt64(timeStr);
            DateTime lastClaim = DateTime.FromBinary(binaryTime);
            return DateTime.UtcNow - lastClaim;
        }
        catch
        {
            // Failsafe if data is corrupted
            return TimeSpan.FromHours(24);
        }
    }

    public void ClaimDailyGift()
    {
        PlayerPrefs.SetString(KEY_DAILY_GIFT_TIME, DateTime.UtcNow.ToBinary().ToString());
        PlayerPrefs.Save();
        Debug.Log("[PlayerData] Daily Gift Claimed at " + DateTime.UtcNow);
    }

    // --- AD FOR LIGHTS TIMING LOGIC ---
    public TimeSpan GetTimeSinceLastAdForLights()
    {
        string timeStr = PlayerPrefs.GetString(KEY_AD_LIGHTS_TIME, string.Empty);
        
        if (string.IsNullOrEmpty(timeStr)) return TimeSpan.FromHours(24);

        try
        {
            long binaryTime = Convert.ToInt64(timeStr);
            DateTime lastClaim = DateTime.FromBinary(binaryTime);
            return DateTime.UtcNow - lastClaim;
        }
        catch { return TimeSpan.FromHours(24); }
    }

    public void ClaimAdForLights()
    {
        PlayerPrefs.SetString(KEY_AD_LIGHTS_TIME, DateTime.UtcNow.ToBinary().ToString());
        PlayerPrefs.Save();
    }

    // --- AD FOR MYSTERY GIFT TIMING LOGIC ---
    public TimeSpan GetTimeSinceLastMysteryGiftAd()
    {
        string timeStr = PlayerPrefs.GetString(KEY_AD_MYSTERY_GIFT_TIME, string.Empty);

        if (string.IsNullOrEmpty(timeStr)) return TimeSpan.FromHours(24);

        try
        {
            long binaryTime = Convert.ToInt64(timeStr);
            DateTime lastClaim = DateTime.FromBinary(binaryTime);
            return DateTime.UtcNow - lastClaim;
        }
        catch { return TimeSpan.FromHours(24); }
    }

    public void ClaimMysteryGiftAd()
    {
        PlayerPrefs.SetString(KEY_AD_MYSTERY_GIFT_TIME, DateTime.UtcNow.ToBinary().ToString());
        PlayerPrefs.Save();
    }

    [ContextMenu("Delete All Save Data")]
    public void ResetData()
    {
        PlayerPrefs.DeleteAll();
        LoadData();
        Debug.Log("Save Data Reset!");
    }
}