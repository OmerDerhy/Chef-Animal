using UnityEngine;
using UnityEngine.UI; 

public class FoodSource : MonoBehaviour
{
    [Header("Configuration")]
    public int sourceID = 0; 
    public string sourceName = "Food Source";
    public int unlockPrice = 100;

    [Header("UI References")]
    public GameObject shoppingCartButtonObj;
    
    [Tooltip("Icon that appears if the player has enough money to buy this source")]
    public GameObject readyToBuyIcon;

    [Header("Finger Animation")]
    public float animationSpeed = 5f;
    public float scaleAmount = 0.1f;
    private Vector3 initialFingerScale;

    [Header("Spawning")]
    public Food[] fruits;
    private BlockManager blockManager;
    private bool isLocked = false;
    private bool canPurchase = false; 

    [Header("Audio")]
    public AudioClip clickSound;

    void Start()
    {
        blockManager = Object.FindFirstObjectByType<BlockManager>();
        
        clickSound = Resources.Load<AudioClip>("FoodClick");
        
        if (shoppingCartButtonObj != null)
        {
            Button btn = shoppingCartButtonObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnShoppingCartClicked);
            }
        }

        if (readyToBuyIcon != null)
        {
            initialFingerScale = readyToBuyIcon.transform.localScale;
            readyToBuyIcon.SetActive(false); 
        }

        if (PlayerData.Instance != null) 
        {
            PlayerData.Instance.OnCoinsChanged += UpdatePurchaseIcon;
        }

        CheckLockState();
    }

    void OnDestroy()
    {
        if (PlayerData.Instance != null) 
        {
            PlayerData.Instance.OnCoinsChanged -= UpdatePurchaseIcon;
        }
    }

    void Update()
    {
        if (readyToBuyIcon != null && readyToBuyIcon.activeInHierarchy)
        {
            AnimateFinger();
        }
    }

    private void AnimateFinger()
    {
        if (readyToBuyIcon != null)
        {
            float scaleFactor = 1f + Mathf.Sin(Time.time * animationSpeed) * scaleAmount;
            readyToBuyIcon.transform.localScale = initialFingerScale * scaleFactor;
        }
    }

    private void UpdatePurchaseIcon(int currentCoins)
    {
        if (readyToBuyIcon != null)
        {
            bool show = isLocked && canPurchase && currentCoins >= unlockPrice;
            readyToBuyIcon.SetActive(show);
        }
    }

    public void CheckLockState()
    {
        if (PlayerData.Instance != null)
        {
            bool isUnlocked = PlayerData.Instance.IsFoodSourceUnlocked(sourceID);
            
            if (isUnlocked)
            {
                UnlockVisuals();
            }
            else
            {
                bool prevUnlocked = (sourceID == 0) || PlayerData.Instance.IsFoodSourceUnlocked(sourceID - 1);
                LockVisuals(prevUnlocked);
            }
            
            UpdatePurchaseIcon(PlayerData.Instance.GetCoins());
        }
    }

    private void LockVisuals(bool isNextAvailable)
    {
        isLocked = true;
        canPurchase = isNextAvailable;
        
        if (shoppingCartButtonObj != null) 
        {
            shoppingCartButtonObj.SetActive(true);
            
            Button btn = shoppingCartButtonObj.GetComponent<Button>();
            Image btnImg = shoppingCartButtonObj.GetComponent<Image>();

            if (btn != null) btn.interactable = isNextAvailable;
            if (btnImg != null) btnImg.color = isNextAvailable ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f);
        }
        
        SetMainVisualsActive(false);
        if (PlayerData.Instance != null) UpdatePurchaseIcon(PlayerData.Instance.GetCoins());
    }

    private void UnlockVisuals()
    {
        isLocked = false;
        canPurchase = false;
        
        if (shoppingCartButtonObj != null) shoppingCartButtonObj.SetActive(false);
        if (readyToBuyIcon != null) readyToBuyIcon.SetActive(false); 

        SetMainVisualsActive(true);
    }

    private void SetMainVisualsActive(bool isActive)
    {
        Button myButton = GetComponent<Button>();
        if (myButton != null) myButton.enabled = isActive;

        Image myImage = GetComponent<Image>();
        if (myImage != null) myImage.enabled = isActive;

        SpriteRenderer mySprite = GetComponent<SpriteRenderer>();
        if (mySprite != null) mySprite.enabled = isActive;
        
        Collider2D myCol = GetComponent<Collider2D>();
        if (myCol != null) myCol.enabled = isActive;
    }

    public void OnShoppingCartClicked()
    {
        if (!canPurchase) return;

        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OpenShop(sourceID, sourceName, unlockPrice, ShopItemType.FoodSource, fruits);
        }
        else
        {
            Debug.LogError("ShopManager not found in the scene!");
        }
    }

    public void SpawnFruit()
    {
        // 1. Check Locked State
        if (isLocked)
        {
            OnShoppingCartClicked(); 
            return;
        }

        // 2. Validate Setup
        if (blockManager == null || fruits.Length == 0) return;

        // 3. Check for Empty Space
        int blockIndex = blockManager.FillBlock();
        
        if (blockIndex != -1)
        {
            // 4. CHECK LIGHTS COST
            // Only try to spend if we actually have a place to put the food
            if (PlayerData.Instance != null)
            {
                if (!PlayerData.Instance.SpendLights(1))
                {
                    Debug.Log("Not enough Lights to spawn food!");
                    // Open "Out of Lights" Panel
                    if (CurrencyShop.Instance != null)
                    {
                        CurrencyShop.Instance.ShowOutOfLights();
                    }
                    return; // Stop here
                }
            }

            // 5. Spawn Logic
            int randomIndex = Random.Range(0, fruits.Length);
            Food randomFood = fruits[randomIndex];

            Block targetBlock = blockManager.blocks[blockIndex];
            targetBlock.ReserveBlock();

            Food spawnedFood = Instantiate(randomFood, transform.position, Quaternion.identity);
            spawnedFood.AnimateMoveToBlock(targetBlock);
        }
        else
        {
            Debug.Log("Board is full! Cannot spawn.");
        }
    }


}