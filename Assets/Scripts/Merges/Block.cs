using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections;

public class Block : MonoBehaviour
{
    [Header("UI Image to Activate")]
    public GameObject imageToActivate; 

    private FoodManager foodManager;
    private Camera mainCamera;
    private Coroutine hideCoroutine;
    private Food currentFood;

    // STATIC variable to track the single active marker across all blocks
    private static Block currentlyActiveBlock;

    // FLAG to reserve block before food physically arrives
    private bool isReserved = false;

    void Start()
    {
        mainCamera = Camera.main;
        foodManager = Object.FindFirstObjectByType<FoodManager>();

        if (foodManager == null) Debug.LogError("Error: No FoodManager found!");

        if (imageToActivate != null)
            imageToActivate.SetActive(false);
    }

    void Update()
    {
        HandleUnifiedInput();
    }

    // --- INPUT HANDLING ---
    private void HandleUnifiedInput()
    {
        bool touchActive = false;

        if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
        {
            touchActive = true;
            foreach (var touch in Touchscreen.current.touches)
            {
                int touchId = touch.touchId.ReadValue();
                
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touchId))
                    continue; // Skip touches on UI

                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    ProcessClick(touch.position.ReadValue());
                }
            }
        }

        if (!touchActive && Mouse.current != null)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return; // UI Blocking

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                ProcessClick(Mouse.current.position.ReadValue());
            }
        }
    }

    private void ProcessClick(Vector2 screenPos)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(ray);

        // Sort hits by Z depth (ascending: objects closest to the camera/lowest Z are processed first)
        System.Array.Sort(hits, (a, b) => a.transform.position.z.CompareTo(b.transform.position.z));

        foreach (var hit in hits)
        {
            Food hitFood = hit.collider.GetComponent<Food>();
            
            // If the ray hits ANY food first (since they are sorted Z-closest), 
            // the player is trying to interact with Food. Abort Block interaction!
            if (hitFood != null) 
            {
                // Let Food.cs handle the interaction instead
                return;
            }

            if (hit.collider.gameObject == gameObject)
            {
                ActivateImage();
                return;
            }
        }
    }

    public void ActivateImage()
    {
        if (imageToActivate == null) return;

        // 1. Deactivate the previous block's marker if it exists
        if (currentlyActiveBlock != null && currentlyActiveBlock != this)
        {
            currentlyActiveBlock.DeactivateImage();
        }

        // 2. Set this block as the active one
        currentlyActiveBlock = this;
        imageToActivate.SetActive(true);

        // 3. Start the fade timer
        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideAfterDelay(0.5f));
    }

    public void DeactivateImage()
    {
        if (imageToActivate != null) imageToActivate.SetActive(false);
        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = null;
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        DeactivateImage();
        if (currentlyActiveBlock == this) currentlyActiveBlock = null;
    }

    // --- LOGIC: SETTING FOOD ---
    public void SetCurrentFood(Food food)
    {
        isReserved = false;

        // 1. If this block is empty, just accept the food
        if (currentFood == null)
        {
            currentFood = food;
            return;
        }

        // 2. Prevent comparing object with itself
        if (currentFood == food) return;

        // 3. MERGE LOGIC: If tags match AND EVOLUTION EXISTS
        if (currentFood.CompareTag(food.tag))
        {
            // FIX: Check if this item is Max Level (Evolve returns null)
            bool canEvolve = false;
            if (foodManager != null)
            {
                canEvolve = (foodManager.Evolve(food.tag) != null);
            }

            if (canEvolve)
            {
                string tag = currentFood.tag;
                
                if(currentFood != null) Destroy(currentFood.gameObject);
                if(food != null) Destroy(food.gameObject);

                EvolveFood(tag, food);
                
                // Highlight the block when merging happens
                ActivateImage();
                return;
            }
            // If cannot evolve, fall through to Overwrite/Swap logic
        }

        if(food.tag == "cookie")
        {
            if(currentFood != null) Destroy(currentFood.gameObject);
            if(food != null) Destroy(food.gameObject);

            Food foodPrefab = foodManager.SetCookieFood();
            Food newFood = Instantiate(foodPrefab, transform.position, Quaternion.identity);
            newFood.AssignToBlock(this); 
            this.currentFood = newFood;
            return;
        }

        // 4. OVERWRITE / SWAP LOGIC
        currentFood = food;
    }

    public Food GetCurrentFood()
    {
        return currentFood;
    }

    public void ResetCurrentFood()
    {
        currentFood = null;
        isReserved = false;
    }
    
    public void ReserveBlock()
    {
        isReserved = true;
    }

    public bool HasCurrentFood()
    {
        return currentFood != null || isReserved;
    }

    private void EvolveFood(string Tag, Food originalFoodToMerge)
    {
        Debug.Log("Evolving food with tag: " + Tag);
        Food foodPrefab = foodManager.Evolve(Tag);
        
        if (foodPrefab != null)
        {
            // Determine the current evolution index before we destroy the original
            int evolutionIndex = -1;
            if (foodManager != null && originalFoodToMerge != null)
            {
                Food[] chain = foodManager.GetEvolutionChain(originalFoodToMerge.gameObject);
                if (chain != null)
                {
                    // Find where the OLD food was in the chain, so the NEW food is index + 1
                    for (int i = 0; i < chain.Length; i++)
                    {
                        if (chain[i].gameObject.tag == Tag)
                        {
                            evolutionIndex = i + 1; // +1 because we are creating the NEXT evolution
                            break;
                        }
                    }
                }
            }

            Food newFood = Instantiate(foodPrefab, transform.position, Quaternion.identity);
            newFood.AssignToBlock(this); 
            this.currentFood = newFood;
            
            // Play the dynamic sound
            if (evolutionIndex >= 0)
            {
                newFood.PlayEvolutionSound(evolutionIndex);
            }
        }
        else
        {
            this.currentFood = null;
        }
    }
}