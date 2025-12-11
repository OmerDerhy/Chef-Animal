using UnityEngine;
using UnityEngine.InputSystem;
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
        HandleTouch();
    }

    // --- INPUT HANDLING ---
    void HandleTouch()
    {
        if (Touchscreen.current == null) return;

        foreach (var touch in Touchscreen.current.touches)
        {
            if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
            {
                Vector2 touchPos = touch.position.ReadValue();
                Ray ray = mainCamera.ScreenPointToRay(touchPos);
                
                RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(ray);

                foreach (var hit in hits)
                {
                    // Check if we clicked the Food sitting on this block
                    Food hitFood = hit.collider.GetComponent<Food>();
                    if (hitFood != null) 
                    {
                        if (hitFood == currentFood)
                        {
                            ActivateImage();
                            return;
                        }
                        continue;
                    }

                    // Check if we clicked the Block itself
                    if (hit.collider.gameObject == gameObject)
                    {
                        ActivateImage();
                        return;
                    }
                }
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

        // 3. MERGE LOGIC: If tags match, destroy both and evolve
        if (currentFood.CompareTag(food.tag))
        {
            string tag = currentFood.tag;
            
            if(currentFood != null) Destroy(currentFood.gameObject);
            if(food != null) Destroy(food.gameObject);

            EvolveFood(tag);
            
            // NEW: Highlight the block when merging happens!
            ActivateImage();
            return;
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

    private void EvolveFood(string Tag)
    {
        Debug.Log("Evolving food with tag: " + Tag);
        Food foodPrefab = foodManager.Evolve(Tag);
        
        if (foodPrefab != null)
        {
            Food newFood = Instantiate(foodPrefab, transform.position, Quaternion.identity);
            newFood.AssignToBlock(this); 
            this.currentFood = newFood;
        }
        else
        {
            this.currentFood = null;
        }
    }
}