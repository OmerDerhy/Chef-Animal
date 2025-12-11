using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// Enum to define what we are buying
public enum ShopItemType 
{ 
    FoodSource, 
    CharacterSlot 
}

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    [Header("UI Elements")]
    public GameObject shopPanel;       
    public TextMeshProUGUI titleText;             
    public TextMeshProUGUI priceText;             
    public Button buyButton;           
    public Button closeButton;

    [Header("Preview Settings")]
    public Transform iconContainer1; 
    public Transform iconContainer2;
    public Transform iconContainer3;
    public GameObject iconPrefab;         
    public float iconScale = 0.01f; 

    private int currentTargetId = -1;
    private int currentPrice = 0;
    private ShopItemType currentItemType; // Track what we are buying

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (shopPanel != null) shopPanel.SetActive(false);

        if (buyButton != null) buyButton.onClick.AddListener(TryBuyItem);
        if (closeButton != null) closeButton.onClick.AddListener(CloseShop);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // RENAMED & UPDATED: Now takes 'ShopItemType'
    public void OpenShop(int id, string itemName, int price, ShopItemType type, Food[] foodsToPreview = null)
    {
        if (this == null || shopPanel == null) return;

        currentTargetId = id;
        currentPrice = price;
        currentItemType = type; // Store the type

        if (titleText != null) titleText.text = "Unlock " + itemName + "?";
        if (priceText != null) priceText.text = " " + price;

        // Generate Icons (Will be empty for Character Slots, which is fine)
        GeneratePreviewIcons(foodsToPreview);

        if (shopPanel != null) shopPanel.SetActive(true);
    }

    private void GeneratePreviewIcons(Food[] foods)
    {
        // Clear containers
        ClearContainer(iconContainer1);
        ClearContainer(iconContainer2);
        ClearContainer(iconContainer3);

        if (iconPrefab == null || foods == null) return;

        List<Food> validFoods = new List<Food>();
        foreach (var f in foods) if (f != null) validFoods.Add(f);

        int count = validFoods.Count;

        if (count == 1)
        {
            SpawnIcon(validFoods[0], iconContainer1);
        }
        else if (count == 2)
        {
            SpawnIcon(validFoods[0], iconContainer2);
            SpawnIcon(validFoods[1], iconContainer3);
        }
        else if (count >= 3)
        {
            SpawnIcon(validFoods[0], iconContainer2);
            SpawnIcon(validFoods[1], iconContainer1);
            SpawnIcon(validFoods[2], iconContainer3);
        }
    }

    private void ClearContainer(Transform container)
    {
        if (container == null) return;
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Transform child = container.GetChild(i);
            if (child != null) Destroy(child.gameObject);
        }
    }

    private void SpawnIcon(Food foodItem, Transform container)
    {
        if (container == null || foodItem == null) return;

        GameObject newIcon = Instantiate(iconPrefab, container);
        newIcon.transform.localPosition = Vector3.zero; 
        newIcon.transform.localScale = Vector3.one * iconScale; 

        Sprite foodSprite = null;
        SpriteRenderer sr = foodItem.GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null) foodSprite = sr.sprite;
        else
        {
            Image img = foodItem.GetComponentInChildren<Image>(true);
            if (img != null) foodSprite = img.sprite;
        }

        Image iconImage = newIcon.GetComponent<Image>();
        if (iconImage != null && foodSprite != null)
        {
            iconImage.sprite = foodSprite;
            iconImage.color = Color.white;
            iconImage.preserveAspect = true;
        }
    }

    public void CloseShop()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
    }

    private void TryBuyItem()
    {
        if (currentTargetId == -1) return;

        if (PlayerData.Instance != null)
        {
            // 1. Check Sequential Logic based on Type
            bool canBuy = false;
            
            if (currentItemType == ShopItemType.FoodSource)
            {
                // Check if previous food source is unlocked
                if (currentTargetId == 0 || PlayerData.Instance.IsFoodSourceUnlocked(currentTargetId - 1)) canBuy = true;
            }
            else if (currentItemType == ShopItemType.CharacterSlot)
            {
                // Check if previous character slot is unlocked
                if (currentTargetId == 0 || PlayerData.Instance.IsCharacterSlotUnlocked(currentTargetId - 1)) canBuy = true;
            }

            if (!canBuy)
            {
                Debug.Log("Locked! Purchase previous item first.");
                return;
            }

            // 2. Spend Money
            if (PlayerData.Instance.SpendCoins(currentPrice))
            {
                Debug.Log($"Purchased {currentItemType} {currentTargetId}");
                
                // 3. Unlock Specific Type
                if (currentItemType == ShopItemType.FoodSource)
                {
                    PlayerData.Instance.UnlockNextFoodSupplier();
                    // Update all FoodSources
                    FoodSource[] allSources = FindObjectsOfType<FoodSource>();
                    foreach (var source in allSources) source.CheckLockState();
                }
                else if (currentItemType == ShopItemType.CharacterSlot)
                {
                    PlayerData.Instance.UnlockNextCharacterSlot();
                    // Update all CharacterSpawners
                    CharacterSpawner[] allSpawners = FindObjectsOfType<CharacterSpawner>();
                    foreach (var spawner in allSpawners) spawner.CheckLockState();
                }

                CloseShop();
            }
            else
            {
                Debug.Log("Not enough coins!");
            }
        }
    }
}