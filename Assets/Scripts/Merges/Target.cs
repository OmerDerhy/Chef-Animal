using UnityEngine;
using UnityEngine.UI;
using System.Collections; 
using System.Collections.Generic;
using TMPro; 

public class Target : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Check this TRUE for the Giraffe/Rare prefab only.")]
    public bool isRareFace = false;

    [Header("Coin Rewards Configuration")]
    [Tooltip("Rewards for Level 1 based on unlocked sources (Index 0 = 1 source, Index 1 = 2 sources...)")]
    private int[] level1Rewards = new int[] { 5, 7, 10 };
    [Tooltip("Rewards for Level 2 based on unlocked sources")]
    private int[] level2Rewards = new int[] { 10, 12, 15, 20 };
    [Tooltip("Rewards for Level 3 based on unlocked sources")]
    private int[] level3Rewards = new int[] { 20, 23, 27, 31 };

    [Header("The Foods the Face Wants")]
    public GameObject[] objectsArray; 

    [Header("UI Settings")]
    public Transform iconContainer; 
    public GameObject iconPrefab;

    [Header("Success Feedback")]
    [Tooltip("Assign your generic coin icon prefab here. It MUST have a TextMeshProUGUI component inside it.")]
    public GameObject coinTextPrefab; 
    [Tooltip("Assign your generic GEM icon prefab here. It MUST have a TextMeshProUGUI component inside it.")]
    public GameObject gemTextPrefab; 

    [Tooltip("Fallback icon if coinTextPrefab is missing")]
    public GameObject successIconPrefab;
    
    public float successIconScale = 0.5f; 
    public float successIconYOffset = 1.4f; 

    [Header("Visual Adjustments")]
    public float iconSize = 0.55f;   
    public float iconSpacing = -0.1f;    

    [Header("Tutorial Settings")]
    [Tooltip("The Finger icon prefab to show drag direction")]
    public GameObject tutorialFingerPrefab;
    private GameObject currentTutorialFinger;
    private const string TUTORIAL_DRAG_KEY = "Tutorial_FirstDishDrag_Count";

    [HideInInspector] public CharacterSpawner parentSpawner;

    private List<GameObject> spawnedIcons = new List<GameObject>();
    private int totalPoints = 0;
    private bool levelCompleted = false; 

    void Start()
    {
        // --- FIX: Trigger Giraffe Tutorial ---
        if (isRareFace)
        {
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.ShowGiraffeTutorial();
            }
        }

        Vector3 finalScale = transform.localScale;
        StartCoroutine(AppearSequence(finalScale));

        AssignRandomFoods();
        InitializeTargetUI();

        // --- Start First Dish Tutorial Check ---
        if (PlayerPrefs.GetInt(TUTORIAL_DRAG_KEY, 0) < 2 && tutorialFingerPrefab != null)
        {
            StartCoroutine(TutorialCheckRoutine());
        }
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

        void ProcessChains(List<Food[]> chains)
        {
            if (chains == null) return;
            foreach (Food[] chain in chains)
            {
                if (chain != null)
                {
                    for (int i = 0; i < chain.Length; i++)
                    {
                        if (chain[i] != null)
                        {
                            // RULE 1: Index 0 (Basic Food) cannot be a target
                            if (i == 0) continue;

                            // RULE 2: Index 3 or above is High Tier (Single Target)
                            if (i >= 3) 
                            {
                                highTierFoods.Add(chain[i]);
                            }
                            // RULE 3: Index 1 or 2 is Low Tier (Double Target)
                            else 
                            {
                                lowTierFoods.Add(chain[i]);
                            }
                        }
                    }
                }
            }
        }

        // --- SELECTION LOGIC ---
        
        if (CookieTime.IsCookieTimeActive)
        {
            // Cookie Time Logic
            if (foodManager.CookieArr != null && foodManager.CookieArr.Length > 0)
            {
                List<Food[]> cookieList = new List<Food[]> { foodManager.CookieArr };
                ProcessChains(cookieList);
            }
        }
        else
        {
            // Normal Mode - Ask FoodManager for unlocked chains based on Level ID logic
            if (PlayerData.Instance != null)
            {
                if (PlayerData.Instance.IsFoodSourceUnlocked(0)) ProcessChains(foodManager.GetFoodsForSource(0));
                if (PlayerData.Instance.IsFoodSourceUnlocked(1)) ProcessChains(foodManager.GetFoodsForSource(1));
                if (PlayerData.Instance.IsFoodSourceUnlocked(2)) ProcessChains(foodManager.GetFoodsForSource(2));
                if (PlayerData.Instance.IsFoodSourceUnlocked(3)) ProcessChains(foodManager.GetFoodsForSource(3));
            }
        }

        if (lowTierFoods.Count == 0 && highTierFoods.Count == 0)
        {
            objectsArray = new GameObject[0];
            return;
        }

        // --- NEW: EASY MODE TUTORIAL (Force 1 Item) ---
        if (PlayerPrefs.GetInt(TUTORIAL_DRAG_KEY, 0) < 2)
        {
            objectsArray = new GameObject[1];
            if (lowTierFoods.Count > 0)
            {
                objectsArray[0] = lowTierFoods[Random.Range(0, lowTierFoods.Count)].gameObject;
            }
            else
            {
                objectsArray[0] = highTierFoods[Random.Range(0, highTierFoods.Count)].gameObject;
            }
            return; 
        }

        // --- RARE FACE LOGIC (Giraffe) ---
        if (isRareFace)
        {
            bool tryHardCombo = (Random.value > 0.5f);
            bool canMakeHardCombo = (highTierFoods.Count > 0 && lowTierFoods.Count > 0);
            
            if (tryHardCombo && canMakeHardCombo)
            {
                objectsArray = new GameObject[2];
                objectsArray[0] = highTierFoods[Random.Range(0, highTierFoods.Count)].gameObject;
                objectsArray[1] = lowTierFoods[Random.Range(0, lowTierFoods.Count)].gameObject;
            }
            else
            {
                if (lowTierFoods.Count > 0)
                {
                    int targetCount = 3;
                    objectsArray = new GameObject[targetCount];
                    for(int i=0; i<targetCount; i++)
                    {
                        objectsArray[i] = lowTierFoods[Random.Range(0, lowTierFoods.Count)].gameObject;
                    }
                }
                else
                {
                    objectsArray = new GameObject[1];
                    objectsArray[0] = highTierFoods[Random.Range(0, highTierFoods.Count)].gameObject;
                }
            }
        }
        else 
        {
            // --- NORMAL FACE LOGIC ---
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

                Button btn = newIcon.GetComponent<Button>();
                if (btn == null) btn = newIcon.AddComponent<Button>();
                
                GameObject foodRef = objectsArray[i]; 
                
                btn.onClick.AddListener(() => {
                    if (RecipeManager.Instance != null)
                    {
                        RecipeManager.Instance.ShowRecipe(foodRef);
                    }
                });
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
            
            // --- NEW: Play Task Complete Sound ---
            AudioClip finishSound = Resources.Load<AudioClip>("Positive Vibes Coin");
            if (finishSound != null)
            {
                // Play strictly at the camera to ensure it's easily heard
                Vector3 playPos = Camera.main != null ? Camera.main.transform.position : transform.position;
                AudioSource.PlayClipAtPoint(finishSound, playPos, 1.0f);
            }
            else
            {
                Debug.LogWarning("Target.cs: Could not find 'Positive Vibes Coin' in Resources folder!");
            }
            
            // --- NEW: Mark Tutorial as Complete ---
            if (PlayerPrefs.GetInt(TUTORIAL_DRAG_KEY, 0) < 2)
            {
                int count = PlayerPrefs.GetInt(TUTORIAL_DRAG_KEY, 0);
                PlayerPrefs.SetInt(TUTORIAL_DRAG_KEY, count + 1);
                PlayerPrefs.Save();
                if (currentTutorialFinger != null) Destroy(currentTutorialFinger);
            }

            // --- 1. GET DATA ---
            int unlockedSources = 1;
            if (PlayerData.Instance != null)
            {
                unlockedSources = PlayerData.Instance.GetFoodSupplierCount();
            }

            FoodManager fm = Object.FindFirstObjectByType<FoodManager>();
            int currentLevel = (fm != null) ? fm.LevelID : 1;

            // --- 2. CALCULATE COINS ---
            int coinsToAdd = 10;
            
            int[] rewardTable = level1Rewards;
            if (currentLevel == 2) rewardTable = level2Rewards;
            else if (currentLevel >= 3) rewardTable = level3Rewards;

            int rewardIndex = Mathf.Clamp(unlockedSources - 1, 0, rewardTable.Length - 1);
            coinsToAdd = rewardTable[rewardIndex];

            // --- 3. APPLY MULTIPLIERS ---
            bool isCookieTime = CookieTime.IsCookieTimeActive;
            // if (isCookieTime)
            // {
            //     coinsToAdd *= 2; 
            // }
            // Note: REMOVED the multiplier for Rare Face on coins as requested.
            // if (isRareFace) coinsToAdd *= 2; 

            // --- 4. GIVE COINS ---
            if (PlayerData.Instance != null) PlayerData.Instance.AddCoins(coinsToAdd);

            // --- 5. VISUALS: COINS ---
            GameObject coinIconToUse = coinTextPrefab; 
            if (coinIconToUse == null) coinIconToUse = successIconPrefab; 
            if (coinIconToUse == null && fm != null) coinIconToUse = fm.successIconPrefab;

            if (coinIconToUse != null && iconContainer != null)
            {
                GameObject successObj = Instantiate(coinIconToUse, iconContainer.parent);
                
                successObj.transform.localPosition = new Vector3(0, successIconYOffset, 0);
                successObj.transform.localScale = Vector3.one * successIconScale; 
                successObj.layer = 6;
                successObj.transform.SetAsLastSibling();

                TextMeshProUGUI tmp = successObj.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = "+" + coinsToAdd;
                else
                {
                    Text legacyText = successObj.GetComponentInChildren<Text>();
                    if (legacyText != null) legacyText.text = "+" + coinsToAdd;
                }

                StartCoroutine(AnimateIconLifecycle(successObj, successIconScale, 2.0f));

                // --- 6. VISUALS: GEMS ---
                if (isCookieTime || isRareFace) 
                {
                    int gemsToAdd = 0;
                    
                    if (currentLevel == 1) gemsToAdd = 7;
                    else if (currentLevel == 2) gemsToAdd = 10;
                    else if (currentLevel == 3) gemsToAdd = 14;
                    else if (currentLevel == 4) gemsToAdd = 17;

                    // NEW LOGIC: Rare Face gives 1.5x Gems
                    if (isRareFace) 
                    {
                        gemsToAdd = Mathf.RoundToInt(gemsToAdd * 1.5f);
                    }

                    if (PlayerData.Instance != null) PlayerData.Instance.AddGems(gemsToAdd);

                    GameObject gemIconToUse = gemTextPrefab;
                    if (gemIconToUse == null && fm != null) gemIconToUse = fm.plus10Gems;

                    if (gemIconToUse != null)
                    {
                        GameObject successObj2 = Instantiate(gemIconToUse, iconContainer.parent);
                        successObj2.transform.localPosition = new Vector3(0, successIconYOffset + 0.6f, 0); 
                        successObj2.transform.localScale = Vector3.one * successIconScale; 
                        successObj2.layer = 6;
                        successObj2.transform.SetAsLastSibling();

                        TextMeshProUGUI gemTmp = successObj2.GetComponentInChildren<TextMeshProUGUI>();
                        if (gemTmp != null) gemTmp.text = "+" + gemsToAdd;
                        else
                        {
                            Text gemLegacyText = successObj2.GetComponentInChildren<Text>();
                            if (gemLegacyText != null) gemLegacyText.text = "+" + gemsToAdd;
                        }

                        StartCoroutine(AnimateIconLifecycle(successObj2, successIconScale, 2.0f));
                    }
                }
            }

            Debug.Log($"Level Complete! Earned {coinsToAdd} coins. Removing Face in 3 seconds...");
            StartCoroutine(RemoveFaceSequence());
        }
    }

    private IEnumerator AnimateIconLifecycle(GameObject iconObj, float targetScale, float lifeTime)
    {
        Transform iconTransform = iconObj.transform;
        iconTransform.localScale = Vector3.zero;

        float popDuration = 0.4f;
        float shrinkDuration = 0.4f;
        float waitDuration = lifeTime - popDuration - shrinkDuration;
        if (waitDuration < 0) waitDuration = 0;

        float elapsed = 0f;
        while (elapsed < popDuration)
        {
            if (iconObj == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / popDuration;
            iconTransform.localScale = Vector3.one * Mathf.SmoothStep(0f, targetScale, t);
            yield return null;
        }
        if(iconObj != null) iconTransform.localScale = Vector3.one * targetScale;

        if (waitDuration > 0) yield return new WaitForSeconds(waitDuration);

        elapsed = 0f;
        while (elapsed < shrinkDuration)
        {
            if (iconObj == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / shrinkDuration;
            iconTransform.localScale = Vector3.one * Mathf.SmoothStep(targetScale, 0f, t);
            yield return null;
        }

        if (iconObj != null) Destroy(iconObj);
    }

    private IEnumerator RemoveFaceSequence()
    {
        float waitTime = CookieTime.IsCookieTimeActive ? 0.5f : 3.0f;
        yield return new WaitForSeconds(waitTime);

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

    // --- Tutorial Logic ---
    private IEnumerator TutorialCheckRoutine()
    {
        BlockManager blockManager = FindObjectOfType<BlockManager>();
        
        while (currentTutorialFinger == null && PlayerPrefs.GetInt(TUTORIAL_DRAG_KEY, 0) < 2)
        {
            if (currentTutorialFinger != null)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            bool fingerSpawned = false;

            if (blockManager != null && objectsArray != null && objectsArray.Length > 0)
            {
                Block[] blocks = blockManager.GetBlocks();
                foreach (var block in blocks)
                {
                    if (block.HasCurrentFood())
                    {
                        Food food = block.GetCurrentFood();
                        if (food != null)
                        {
                            foreach (GameObject reqObj in objectsArray)
                            {
                                if (reqObj != null && food.CompareTag(reqObj.tag))
                                {
                                    SpawnTutorialFinger(food.transform);
                                    fingerSpawned = true;
                                    break; 
                                }
                            }
                        }
                    }
                    if (fingerSpawned) break; 
                }
            }
            yield return new WaitForSeconds(0.5f); 
        }
    }

    private void SpawnTutorialFinger(Transform targetFood)
    {
        if (tutorialFingerPrefab == null) return;

        currentTutorialFinger = Instantiate(tutorialFingerPrefab, transform.parent); 
        StartCoroutine(AnimateFingerRoutine(currentTutorialFinger.transform, targetFood, this.transform));
    }

    private IEnumerator AnimateFingerRoutine(Transform finger, Transform startObj, Transform endObj)
    {
        while (finger != null)
        {
            if (startObj == null || endObj == null)
            {
                Destroy(finger.gameObject);
                yield break;
            }

            float duration = 1.5f;
            float elapsed = 0f;
            Vector3 startPos = startObj.position;
            Vector3 endPos = endObj.position;

            while (elapsed < duration)
            {
                if (finger == null) yield break;
                if (startObj != null) startPos = startObj.position;

                float t = elapsed / duration;
                t = Mathf.SmoothStep(0, 1, t);
                
                finger.position = Vector3.Lerp(startPos, endPos, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(0.2f);
        }
    }
}