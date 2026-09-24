using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CharacterSpawner : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("ID 0 is the first (Free) spot. ID 1 is the second (Locked) spot.")]
    public int spawnerID = 0; 
    public int unlockPrice = 500;
    public string spawnerName = "Chair";

    [Header("UI References")]
    [Tooltip("The Shopping Cart Button GameObject (Child of this object)")]
    public GameObject shoppingCartButtonObj;

    [Tooltip("Icon that appears if the player has enough money to buy this spot")]
    public GameObject readyToBuyIcon;

    [Header("Finger Animation")]
    public float animationSpeed = 5f;
    public float scaleAmount = 0.1f;
    private Vector3 initialFingerScale;

    [Header("Face Configuration")]
    public GameObject[] facePrefabs;
    public Transform spawnPoint; 

    private bool isLocked = false;
    private bool canPurchase = false; // Tracks if this is the next unlocked spot
    private GameObject currentFace; 
    private bool isRespawning = false; 

    void Start()
    {
        if (shoppingCartButtonObj != null)
        {
            Button btn = shoppingCartButtonObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnShoppingCartClicked);
            }
        }

        // Capture initial scale for breathing animation
        if (readyToBuyIcon != null)
        {
            initialFingerScale = readyToBuyIcon.transform.localScale;
            readyToBuyIcon.SetActive(false); 
        }

        // Subscribe to coin updates to show/hide the finger icon in real-time
        if (PlayerData.Instance != null) 
        {
            PlayerData.Instance.OnCoinsChanged += UpdatePurchaseIcon;
        }

        CheckLockState();
    }

    void OnDestroy()
    {
        // Clean up event subscription
        if (PlayerData.Instance != null) 
        {
            PlayerData.Instance.OnCoinsChanged -= UpdatePurchaseIcon;
        }
    }

    void Update()
    {
        // Animate the finger if it is visible
        if (readyToBuyIcon != null && readyToBuyIcon.activeInHierarchy)
        {
            AnimateFinger();
        }
    }

    private void AnimateFinger()
    {
        if (readyToBuyIcon != null)
        {
            // Creates a smooth wave between (1 - scaleAmount) and (1 + scaleAmount)
            float scaleFactor = 1f + Mathf.Sin(Time.time * animationSpeed) * scaleAmount;
            readyToBuyIcon.transform.localScale = initialFingerScale * scaleFactor;
        }
    }

    // --- Visual Update Logic ---
    private void UpdatePurchaseIcon(int currentCoins)
    {
        if (readyToBuyIcon != null)
        {
            // Show icon ONLY if: Locked AND Is Next in Line AND Have Enough Money
            bool show = isLocked && canPurchase && currentCoins >= unlockPrice;
            readyToBuyIcon.SetActive(show);
        }
    }

    public void CheckLockState()
    {
        if (PlayerData.Instance != null)
        {
            bool isUnlocked = PlayerData.Instance.IsCharacterSlotUnlocked(spawnerID);
            
            if (isUnlocked)
            {
                UnlockVisuals();
            }
            else
            {
                // Check if the previous one is unlocked to see if we can buy this one
                bool prevUnlocked = (spawnerID == 0) || PlayerData.Instance.IsCharacterSlotUnlocked(spawnerID - 1);
                LockVisuals(prevUnlocked);
            }

            // Initial check for icon visibility
            UpdatePurchaseIcon(PlayerData.Instance.GetCoins());
        }
    }

    private void LockVisuals(bool isNextAvailable)
    {
        isLocked = true;
        canPurchase = isNextAvailable;

        // Show lock button
        if (shoppingCartButtonObj != null) 
        {
            shoppingCartButtonObj.SetActive(true);
            
            Button btn = shoppingCartButtonObj.GetComponent<Button>();
            Image img = shoppingCartButtonObj.GetComponent<Image>();

            if (btn != null) btn.interactable = isNextAvailable;
            if (img != null) img.color = isNextAvailable ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f);
        }

        // Remove any existing face if we get locked
        if (currentFace != null) Destroy(currentFace);
        
        // Reset flags
        isRespawning = false;
        StopAllCoroutines();

        // Refresh icon state
        if (PlayerData.Instance != null) UpdatePurchaseIcon(PlayerData.Instance.GetCoins());
    }

    private void UnlockVisuals()
    {
        if (!isLocked && currentFace != null) return; // Already running
        isLocked = false;
        canPurchase = false;

        // Hide lock button
        if (shoppingCartButtonObj != null) shoppingCartButtonObj.SetActive(false);
        if (readyToBuyIcon != null) readyToBuyIcon.SetActive(false); // Hide icon

        // Only spawn if empty AND NOT waiting for a respawn
        if (currentFace == null && !isRespawning)
        {
            SpawnRandomFace();
        }
    }

    public void OnShoppingCartClicked()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OpenShop(spawnerID, spawnerName, unlockPrice, ShopItemType.CharacterSlot, null);
        }
    }

    public void SpawnRandomFace()
    {
        if (isLocked) return; 
        isRespawning = false;

        if (facePrefabs == null || facePrefabs.Length == 0) return;

        int spawnIndex = Random.Range(0, facePrefabs.Length);

        GameObject selectedPrefab = facePrefabs[spawnIndex];
        if (selectedPrefab == null) return;

        Vector3 pos = spawnPoint ? spawnPoint.position : transform.position;
        Quaternion rot = spawnPoint ? spawnPoint.rotation : transform.rotation;

        GameObject newFace = Instantiate(selectedPrefab, pos, rot);
        currentFace = newFace; 

        Target faceTarget = newFace.GetComponent<Target>();
        if (faceTarget == null) faceTarget = newFace.GetComponentInChildren<Target>();

        if (faceTarget != null)
        {
            faceTarget.parentSpawner = this;
        }
    }

    public void ScheduleRespawn(float delay)
    {
        if (isLocked) return;
        
        // Set flag to block UnlockVisuals from intervening
        isRespawning = true;
        StartCoroutine(RespawnRoutine(delay));
    }

    private IEnumerator RespawnRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnRandomFace();
    }
}