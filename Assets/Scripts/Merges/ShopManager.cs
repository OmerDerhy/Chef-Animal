using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections; 

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
    public float iconScale = 0.2f; 

    [Header("Lights Reward Visuals")]
    [Tooltip("The Lights Icon Prefab (UI Image) to spawn for the animation")]
    public GameObject lightIconPrefab; 
    [Tooltip("The target UI element (Lights Bar/Icon) where the flying lights will go")]
    public Transform lightsTarget;     

    private int currentTargetId = -1;
    private int currentPrice = 0;
    private ShopItemType currentItemType; 

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

    public void OpenShop(int id, string itemName, int price, ShopItemType type, Food[] foodsToPreview = null)
    {
        if (this == null || shopPanel == null) return;

        currentTargetId = id;
        currentPrice = price;
        currentItemType = type; 

        if (titleText != null) titleText.text = "Unlock " + itemName + "?";
        if (priceText != null) priceText.text = " " + price;

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

        // Normalize base size to prevent huge sprites
        RectTransform rt = newIcon.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(100, 100);
        }

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
            // 1. Check Sequential Logic
            bool canBuy = false;
            
            if (currentItemType == ShopItemType.FoodSource)
            {
                if (currentTargetId == 0 || PlayerData.Instance.IsFoodSourceUnlocked(currentTargetId - 1)) canBuy = true;
            }
            else if (currentItemType == ShopItemType.CharacterSlot)
            {
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
                
                // 3. Unlock and Trigger Visuals
                if (currentItemType == ShopItemType.FoodSource)
                {
                    PlayerData.Instance.UnlockNextFoodSupplier();
                    FoodSource[] allSources = FindObjectsOfType<FoodSource>();
                    foreach (var source in allSources) source.CheckLockState();
                }
                else if (currentItemType == ShopItemType.CharacterSlot)
                {
                    PlayerData.Instance.UnlockNextCharacterSlot();
                    CharacterSpawner[] allSpawners = FindObjectsOfType<CharacterSpawner>();
                    foreach (var spawner in allSpawners) spawner.CheckLockState();
                }

                // Trigger Flying Lights Animation
                if (lightIconPrefab != null && lightsTarget != null && buyButton != null)
                {
                    SpawnFlyingLights(buyButton.transform.position);
                }
                
                // --- NEW: Play Purchase Sound ---
                AudioClip purchaseSound = Resources.Load<AudioClip>("Harp Money 1");
                if (purchaseSound != null)
                {
                    Vector3 playPos = Camera.main != null ? Camera.main.transform.position : transform.position;
                    // Lowered volume slightly to 0.7f so it's not deafening relative to music
                    AudioSource.PlayClipAtPoint(purchaseSound, playPos, 0.7f);
                }

                CloseShop();
            }
            else
            {
                Debug.Log("Not enough coins! Opening Currency Shop.");
                // --- NEW: Open Currency Shop if not enough money ---
                if (CurrencyShop.Instance != null)
                {
                    CurrencyShop.Instance.OpenShop();
                    CloseShop(); // Close the item shop to show currency shop
                }
            }
        }
    }

    // --- FLYING ANIMATION LOGIC ---
    public void SpawnFlyingLights(Vector3 startPos)
    {
        // Find main canvas to parent the icons so they are visible on top
        Canvas mainCanvas = GetComponentInParent<Canvas>();
        if (mainCanvas == null) mainCanvas = FindObjectOfType<Canvas>();
        
        if (mainCanvas != null)
        {
            // Spawn 5 icons for effect with slight delay
            for (int i = 0; i < 5; i++)
            {
                StartCoroutine(FlyRoutine(lightIconPrefab, lightsTarget, mainCanvas.transform, startPos, i * 0.1f));
            }
        }
    }

    private IEnumerator FlyRoutine(GameObject prefab, Transform target, Transform parent, Vector3 startPos, float delay)
    {
        yield return new WaitForSeconds(delay);

        GameObject icon = Instantiate(prefab, parent);
        
        // --- Fix: Normalize size ---
        RectTransform rt = icon.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(0.4f, 0.6f);
        }
        
        // Setup initial position
        icon.transform.position = startPos; 
        
        // Reset scale/rotation
        icon.transform.localScale = Vector3.one * 0.8f; // slightly smaller flying icon
        icon.transform.rotation = Quaternion.identity;

        // Bring to front
        icon.transform.SetAsLastSibling();

        float duration = 0.8f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (target == null) break;
            
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t); // Smoothstep ease
            
            icon.transform.position = Vector3.Lerp(startPos, target.position, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(icon);
    }
}