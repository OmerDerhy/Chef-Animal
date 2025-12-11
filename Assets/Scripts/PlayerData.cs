using UnityEngine;
using UnityEngine.SceneManagement; // Required to get Level Name
using System; 

public class PlayerData : MonoBehaviour
{
    public static PlayerData Instance;

    public event Action<int> OnCoinsChanged;

    [Header("Global Data")]
    [SerializeField] private int coins;

    // Keys for saving data
    private const string KEY_COINS = "Save_Coins";
    
    // We don't use constant keys for unlocks anymore, 
    // we generate them dynamically based on the Scene name.

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

    // --- COINS (Global) ---
    public int GetCoins() { return coins; }
    
    public void AddCoins(int amount) 
    { 
        coins += amount; 
        SaveCoins(); 
        OnCoinsChanged?.Invoke(coins); 
    }
    
    public bool SpendCoins(int amount) 
    { 
        if (coins >= amount) 
        { 
            coins -= amount; 
            SaveCoins(); 
            OnCoinsChanged?.Invoke(coins); 
            return true; 
        } 
        return false; 
    }

    private void SaveCoins()
    {
        PlayerPrefs.SetInt(KEY_COINS, coins);
        PlayerPrefs.Save();
    }

    // --- LEVEL SPECIFIC KEYS ---
    // Generates a unique key like "Level1_FoodUnlocks"
    private string GetFoodKey()
    {
        return SceneManager.GetActiveScene().name + "_FoodUnlocks";
    }

    private string GetCharKey()
    {
        return SceneManager.GetActiveScene().name + "_CharUnlocks";
    }

    // --- FOOD SUPPLIERS (Per Level) ---
    public int GetFoodSupplierCount()
    {
        // Default is 1 (ID 0 is unlocked)
        return PlayerPrefs.GetInt(GetFoodKey(), 1);
    }
    
    public bool IsFoodSourceUnlocked(int id) 
    { 
        // Checks the save data for THIS specific level
        return id < GetFoodSupplierCount(); 
    }
    
    public void UnlockNextFoodSupplier() 
    { 
        int current = GetFoodSupplierCount();
        PlayerPrefs.SetInt(GetFoodKey(), current + 1);
        PlayerPrefs.Save();
    }

    // --- CHARACTER SLOTS (Per Level) ---
    public int GetCharacterCount()
    {
        // Default is 1 (ID 0 is unlocked)
        return PlayerPrefs.GetInt(GetCharKey(), 1);
    }

    public bool IsCharacterSlotUnlocked(int id) 
    {
        return id < GetCharacterCount(); 
    }

    public void UnlockNextCharacterSlot()
    {
        int current = GetCharacterCount();
        PlayerPrefs.SetInt(GetCharKey(), current + 1);
        PlayerPrefs.Save();
    }

    // --- LOAD SYSTEM ---
    private void LoadData()
    {
        // We only load coins into a variable. 
        // Unlocks are read directly from PlayerPrefs when needed to support scene changes.
        coins = PlayerPrefs.GetInt(KEY_COINS, 1000); 
        OnCoinsChanged?.Invoke(coins);
    }

    [ContextMenu("Delete All Save Data")]
    public void ResetData()
    {
        PlayerPrefs.DeleteAll();
        LoadData();
        Debug.Log("Save Data Reset!");
    }
}