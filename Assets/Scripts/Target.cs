using UnityEngine;
using UnityEngine.UI;
using System.Collections; 
using System.Collections.Generic;

public class Target : MonoBehaviour
{
    [Header("The Foods the Face Wants")]
    public GameObject[] objectsArray; 

    // Removed manual booleans (enableFruits, etc.) as they are now determined by PlayerData save file

    [Header("UI Settings")]
    public Transform iconContainer; 
    public GameObject iconPrefab;

    [Header("Success Feedback")]
    [Tooltip("The icon that appears when level is complete. If empty, it will try to use the one in FoodManager.")]
    public GameObject successIconPrefab;
    [Tooltip("Scale of the success icon (0.5 = half size, 1.0 = full size)")]
    public float successIconScale = 0.5f; 
    [Tooltip("How high above the center the success icon appears")]
    public float successIconYOffset = 1.4f; 

    [Header("Visual Adjustments")]
    public float iconSize = 0.55f;   
    public float iconSpacing = -0.1f;    

    // --- REFERENCE TO SPAWNER ---
    [HideInInspector] public CharacterSpawner parentSpawner;

    private List<GameObject> spawnedIcons = new List<GameObject>();
    private int totalPoints = 0;
    private bool levelCompleted = false; 

    void Start()
    {
        Vector3 finalScale = transform.localScale;
        StartCoroutine(AppearSequence(finalScale));

        AssignRandomFoods();
        InitializeTargetUI();
    }

    public void RefreshTargetFoods()
    {
        totalPoints = 0;
        levelCompleted = false;
        AssignRandomFoods();
        InitializeTargetUI();
    }

    private IEnumerator AppearSequence(Vector3 endScale)
    {
        transform.localScale = Vector3.zero;
        float duration = 0.5f; 
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0, 1, t); 
            transform.localScale = Vector3.Lerp(Vector3.zero, endScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = endScale;
    }

    private void AssignRandomFoods()
    {
        FoodManager foodManager = Object.FindFirstObjectByType<FoodManager>();
        
        if (foodManager == null)
        {
            Debug.LogError("CRITICAL: FoodManager not found in scene!");
            return;
        }

        List<Food> lowTierFoods = new List<Food>();
        List<Food> highTierFoods = new List<Food>();

        void ProcessFoods(Food[] foods)
        {
            if (foods != null)
            {
                for (int i = 0; i < foods.Length; i++)
                {
                    if (foods[i] != null)
                    {
                        if (i >= 2) highTierFoods.Add(foods[i]);
                        else lowTierFoods.Add(foods[i]);
                    }
                }
            }
        }

        // --- SELECTION LOGIC ---
        
        if (CookieTime.IsCookieTimeActive)
        {
            if (foodManager.CookieArr != null && foodManager.CookieArr.Length > 0)
            {
                ProcessFoods(foodManager.CookieArr);
            }
        }
        else
        {
            // 2. Normal Mode - Check Unlocked Sources from PlayerData
            bool unlockBakery = true; 
            bool unlockIce = false;   
            bool unlockFruits = false;

            if (PlayerData.Instance != null)
            {
                unlockBakery = PlayerData.Instance.IsFoodSourceUnlocked(0); 
                unlockIce = PlayerData.Instance.IsFoodSourceUnlocked(1);    
                unlockFruits = PlayerData.Instance.IsFoodSourceUnlocked(2); 
            }

            if (unlockBakery) ProcessFoods(foodManager.FlourArr);
            if (unlockIce){
                ProcessFoods(foodManager.IcePopArr);
                ProcessFoods(foodManager.IceCreamArr);
            }
            if (unlockFruits){
                ProcessFoods(foodManager.AppleArr);
                ProcessFoods(foodManager.OrangeArr);
                ProcessFoods(foodManager.BananaArr);
            }
        }

        // Safety check
        if (lowTierFoods.Count == 0 && highTierFoods.Count == 0)
        {
            objectsArray = new GameObject[0];
            return;
        }

        List<Food> allFoods = new List<Food>();
        allFoods.AddRange(lowTierFoods);
        allFoods.AddRange(highTierFoods);

        Food firstPick = allFoods[Random.Range(0, allFoods.Count)];

        if (highTierFoods.Contains(firstPick))
        {
            objectsArray = new GameObject[1];
            objectsArray[0] = firstPick.gameObject;
        }
        else
        {
            objectsArray = new GameObject[2];
            objectsArray[0] = firstPick.gameObject;

            if (lowTierFoods.Count > 0)
                objectsArray[1] = lowTierFoods[Random.Range(0, lowTierFoods.Count)].gameObject;
            else
                objectsArray[1] = firstPick.gameObject; 
        }
    }

    private void InitializeTargetUI()
    {
        if (iconContainer == null || iconPrefab == null) return;
        if (objectsArray == null) return; 

        HorizontalLayoutGroup layoutGroup = iconContainer.GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup != null)
        {
            layoutGroup.spacing = iconSpacing;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        }

        foreach(Transform child in iconContainer) Destroy(child.gameObject);
        spawnedIcons.Clear();

        for (int i = 0; i < objectsArray.Length; i++)
        {
            if (objectsArray[i] != null)
            {
                GameObject newIcon = Instantiate(iconPrefab, iconContainer);
                newIcon.layer = 6; 
                newIcon.transform.localScale = Vector3.one; 
                newIcon.transform.localPosition = new Vector3(newIcon.transform.localPosition.x, newIcon.transform.localPosition.y, 0);

                RectTransform rt = newIcon.GetComponent<RectTransform>();
                if (rt != null) rt.sizeDelta = new Vector2(iconSize, iconSize);

                spawnedIcons.Add(newIcon);

                Sprite foodSprite = null;
                SpriteRenderer sr = objectsArray[i].GetComponentInChildren<SpriteRenderer>(true);
                if (sr != null) foodSprite = sr.sprite;
                else
                {
                    Image img = objectsArray[i].GetComponentInChildren<Image>(true);
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
            else spawnedIcons.Add(null); 
        }
    }

    public void addpoint()
    {
        totalPoints++;
        if (CheckIfComplete() && !levelCompleted) 
        {
            levelCompleted = true;
            
            // --- UPDATED: Reward Logic based on Unlock Count ---
            int unlockedSources = 1;
            if (PlayerData.Instance != null)
            {
                unlockedSources = PlayerData.Instance.GetFoodSupplierCount();
            }

            int coinsToAdd = 50;
            GameObject iconToUse = null;
            FoodManager fm = Object.FindFirstObjectByType<FoodManager>();

            // Determine base coins and icon
            if (unlockedSources == 1)
            {
                coinsToAdd = 50;
                if (fm != null) iconToUse = fm.plus50;
            }
            else if (unlockedSources == 2)
            {
                coinsToAdd = 100;
                if (fm != null) iconToUse = fm.plus100;
            }
            else if (unlockedSources >= 3)
            {
                coinsToAdd = 200;
                if (fm != null) iconToUse = fm.plus200;
            }

            // Fallback if specific plus icon is missing
            if (iconToUse == null)
            {
                iconToUse = successIconPrefab; 
                if (iconToUse == null && fm != null) iconToUse = fm.successIconPrefab; 
            }

            // --- COOKIE TIME BONUS ---
            bool isCookieTime = CookieTime.IsCookieTimeActive;
            if (isCookieTime)
            {
                coinsToAdd *= 2; // Double Coins
            }

            // Add Coins
            if (PlayerData.Instance != null) PlayerData.Instance.AddCoins(coinsToAdd);

            // Show Icons
            if (iconToUse != null && iconContainer != null)
            {
                // 1. First Icon
                GameObject successObj = Instantiate(iconToUse, iconContainer.parent);
                successObj.transform.localPosition = new Vector3(0, successIconYOffset, 0);
                successObj.layer = 6;
                Destroy(successObj, 2.0f);

                // 2. Second Icon (If Cookie Time)
                if (isCookieTime)
                {
                    GameObject successObj2 = Instantiate(iconToUse, iconContainer.parent);
                    // Spawn slightly higher (Y offset + 0.6)
                    successObj2.transform.localPosition = new Vector3(0, successIconYOffset + 0.6f, 0); 
                    successObj2.layer = 6;
                    Destroy(successObj2, 2.0f);
                }
            }

            Debug.Log($"Level Complete! Earned {coinsToAdd} coins. Removing Face in 3 seconds...");
            StartCoroutine(RemoveFaceSequence());
        }
    }

    private IEnumerator RemoveFaceSequence()
    {
        yield return new WaitForSeconds(3.0f);

        Face thisFace = GetComponent<Face>();
        if (thisFace != null)
        {
            thisFace.TriggerFinishSequence();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RemoveFood(int index)
    {
        if (index >= 0 && index < objectsArray.Length) objectsArray[index] = null;
    }

    public void RemoveFoodImage(int index)
    {
        if (index >= 0 && index < spawnedIcons.Count && spawnedIcons[index] != null)
            spawnedIcons[index].SetActive(false);
    }

    public void AnimationAgain() { }

    private bool CheckIfComplete()
    {
        if(objectsArray == null) return false;
        if(totalPoints >= objectsArray.Length) return true;
        return false;   
    }
}