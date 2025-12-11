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
    
    [Header("Spawning")]
    public Food[] fruits;
    private BlockManager blockManager;
    private bool isLocked = false;
    private bool canPurchase = false; 

    void Start()
    {
        blockManager = Object.FindFirstObjectByType<BlockManager>();
        
        if (shoppingCartButtonObj != null)
        {
            Button btn = shoppingCartButtonObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnShoppingCartClicked);
            }
        }

        CheckLockState();
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
    }

    private void UnlockVisuals()
    {
        isLocked = false;
        canPurchase = false;
        
        if (shoppingCartButtonObj != null) shoppingCartButtonObj.SetActive(false);
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
            // UPDATED: Use the new generic OpenShop method with ShopItemType.FoodSource
            ShopManager.Instance.OpenShop(sourceID, sourceName, unlockPrice, ShopItemType.FoodSource, fruits);
        }
        else
        {
            Debug.LogError("ShopManager not found in the scene!");
        }
    }

    public void SpawnFruit()
    {
        if (isLocked)
        {
            OnShoppingCartClicked(); 
            return;
        }

        if (blockManager == null || fruits.Length == 0) return;

        int randomIndex = Random.Range(0, fruits.Length);
        Food randomFood = fruits[randomIndex];
        
        int blockIndex = blockManager.FillBlock();
        
        if (blockIndex != -1)
        {
            Block targetBlock = blockManager.blocks[blockIndex];
            targetBlock.ReserveBlock();

            Food spawnedFood = Instantiate(randomFood, transform.position, Quaternion.identity);
            spawnedFood.AnimateMoveToBlock(targetBlock);
        }
    }
}