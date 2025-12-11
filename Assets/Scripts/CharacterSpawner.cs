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

    [Header("Face Configuration")]
    public GameObject[] facePrefabs;
    public Transform spawnPoint; 

    private bool isLocked = false;
    private GameObject currentFace; 

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

        CheckLockState();
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
        }
    }

    private void LockVisuals(bool isNextAvailable)
    {
        // FIX: Removed "if (isLocked) return;" 
        // We must proceed to update the visual state (button color/interactability) 
        // even if we are already locked, because "isNextAvailable" might have changed.
        
        isLocked = true;

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
    }

    private void UnlockVisuals()
    {
        if (!isLocked && currentFace != null) return; // Already running
        isLocked = false;

        // Hide lock button
        if (shoppingCartButtonObj != null) shoppingCartButtonObj.SetActive(false);

        // Start spawning
        if (currentFace == null)
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

        if (facePrefabs == null || facePrefabs.Length == 0) return;

        int randomIndex = Random.Range(0, facePrefabs.Length);
        GameObject selectedPrefab = facePrefabs[randomIndex];

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
        StartCoroutine(RespawnRoutine(delay));
    }

    private IEnumerator RespawnRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnRandomFace();
    }
}