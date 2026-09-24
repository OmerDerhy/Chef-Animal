using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace MergesChef.Map
{
    public class KitchenAccessoryVisual : AccessoryVisual
    {
        [Header("Cooking Setup")]
        [Tooltip("Assign the dishes this specific kitchen tool can cook!")]
        public Dish[] availableDishes;

        [Header("UI Indicators")]
        [Tooltip("Assign a child GameObject (like a SpriteRenderer) with an exclamation mark to show when ready to cook.")]
        public GameObject readyToCookIndicator;

        [Header("Cooking Animations & Effects")]
        [Tooltip("Optional: If you want to change the main sprite while cooking, assign the 'cooking' sprites here. Index 0 corresponds to Level 1, Index 1 to Level 2, etc.")]
        public Sprite[] cookingSprites;

        [Tooltip("Assign a GameObject (like particles or an animated sprite) that should only be active while cooking.")]
        public GameObject cookingEffects;

        [Tooltip("Optional: Assign an Animator to play a cooking animation. The script will set a boolean parameter called 'IsCooking' on it.")]
        public Animator toolAnimator;

        [Header("Stock UI")]
        [Tooltip("The parent GameObject containing the circle background, icon, and text.")]
        public GameObject dishStockContainer;
        [Tooltip("The SpriteRenderer inside the circle to show which dish was cooked.")]
        public SpriteRenderer dishStockIcon;
        [Tooltip("The TextMeshPro text inside the circle showing how many are left.")]
        public TextMeshPro dishStockText;

        [Header("Stock UI Visuals")]
        [Tooltip("The sorting order of the dish sprite to ensure it renders above the background.")]
        public int dishSpriteSortingOrder = 32000;
        [Tooltip("The scale of the dish sprite to make it smaller.")]
        private Vector3 dishSpriteScale = new Vector3(0.4f, 0.4f, 1f);

        public bool IsCooking { get; private set; }
        
        private int remainingDishes = 0;
        private int reservedDishes = 0;
        private Dish currentDishType;

        private Sprite originalSprite;
        private SpriteRenderer mainSpriteRenderer;
        
        // Cache original sprites of variations so we can restore them after cooking
        private System.Collections.Generic.Dictionary<SpriteRenderer, Sprite> originalVariationSprites = new System.Collections.Generic.Dictionary<SpriteRenderer, Sprite>();

        private void Start()
        {
            // Find the main SpriteRenderer so we can remember its original sprite
            mainSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (mainSpriteRenderer != null)
            {
                originalSprite = mainSpriteRenderer.sprite;
            }

            UpdateIndicator();
            UpdateCookingEffects();
            UpdateStockUI();
        }

        public void StartCooking(Dish dish)
        {
            // Only allow cooking if we are not currently cooking AND we have zero dishes left in stock
            if (IsCooking || remainingDishes > 0) return;

            if (LinkedAccessory != null && LinkedAccessory.Level < dish.requiredLevel)
            {
                Debug.LogWarning($"[{gameObject.name}] Cannot cook {dish.dishName}! Requires machine level {dish.requiredLevel}, but this machine is level {LinkedAccessory.Level}.");
                return;
            }

            if (PlayerData.Instance != null && !PlayerData.Instance.SpendLights(dish.cost))
            {
                Debug.LogWarning($"[{gameObject.name}] Not enough lights to cook {dish.dishName}! Requires {dish.cost} lights.");
                // We don't have enough lights to cook this dish
                return;
            }
            
            StartCoroutine(CookingRoutine(dish));
        }

        private IEnumerator CookingRoutine(Dish dish)
        {
            IsCooking = true;
            UpdateIndicator();
            UpdateCookingEffects();

            Debug.Log($"[{gameObject.name}] Started cooking {dish.dishName}. Will take {dish.timeToCook} seconds.");

            yield return new WaitForSeconds(dish.timeToCook);

            Debug.Log($"[{gameObject.name}] Finished cooking {dish.dishName}!");
            IsCooking = false;
            remainingDishes = dish.amountOfDishes;
            reservedDishes = 0;
            currentDishType = dish;
            
            UpdateIndicator();
            UpdateCookingEffects();
            UpdateStockUI();
        }

        public int GetAvailableUnreservedDishes()
        {
            return remainingDishes - reservedDishes;
        }

        public void ReserveDish()
        {
            if (GetAvailableUnreservedDishes() > 0)
            {
                reservedDishes++;
            }
        }

        /// <summary>
        /// Called by Staff when they arrive to pick up the dish.
        /// Returns the cooked dish and decrements stock.
        /// </summary>
        public Dish TakeDish()
        {
            if (remainingDishes <= 0) return null;
            
            Dish taken = currentDishType;
            remainingDishes--;
            if (reservedDishes > 0) reservedDishes--;

            if (remainingDishes <= 0)
            {
                currentDishType = null;
            }
            
            UpdateIndicator();
            UpdateStockUI();
            
            return taken;
        }

        public void UpdateIndicator()
        {
            if (readyToCookIndicator != null)
            {
                // Only show exclamation mark if we are empty and not cooking
                bool isReady = !IsCooking && remainingDishes <= 0 && availableDishes != null && availableDishes.Length > 0;
                readyToCookIndicator.SetActive(isReady);
            }
        }

        public void UpdateStockUI()
        {
            if (dishStockContainer != null)
            {
                bool hasStock = remainingDishes > 0;
                dishStockContainer.SetActive(hasStock);

                if (hasStock)
                {
                    if (dishStockText != null) dishStockText.text = remainingDishes.ToString();
                    if (dishStockIcon != null && currentDishType != null) 
                    {
                        dishStockIcon.sprite = currentDishType.icon;
                        dishStockIcon.sortingOrder = dishSpriteSortingOrder;
                        
                        // We only scale if the icon is a separate GameObject from the container 
                        // to avoid shrinking the entire stock UI.
                        if (dishStockIcon.gameObject != dishStockContainer)
                        {
                            dishStockIcon.transform.localScale = dishSpriteScale;
                        }
                    }
                }
            }
        }

        public void UpdateCookingEffects()
        {
            int currentLevelIndex = LinkedAccessory != null ? LinkedAccessory.Level - 1 : 0;
            
            Sprite targetCookingSprite = null;
            if (cookingSprites != null && currentLevelIndex >= 0 && currentLevelIndex < cookingSprites.Length)
            {
                targetCookingSprite = cookingSprites[currentLevelIndex];
            }

            // Find the active renderer (either the specific visual variation or the main one)
            SpriteRenderer activeRenderer = mainSpriteRenderer;
            Sprite activeOriginalSprite = originalSprite;

            if (visualVariations != null && currentLevelIndex >= 0 && currentLevelIndex < visualVariations.Length)
            {
                if (visualVariations[currentLevelIndex] != null && visualVariations[currentLevelIndex].activeSelf)
                {
                    activeRenderer = visualVariations[currentLevelIndex].GetComponent<SpriteRenderer>();
                    if (activeRenderer == null) activeRenderer = visualVariations[currentLevelIndex].GetComponentInChildren<SpriteRenderer>();

                    if (activeRenderer != null)
                    {
                        if (!originalVariationSprites.ContainsKey(activeRenderer))
                        {
                            originalVariationSprites[activeRenderer] = activeRenderer.sprite;
                        }
                        activeOriginalSprite = originalVariationSprites[activeRenderer];
                    }
                }
            }

            // Swap sprite if one is assigned
            if (activeRenderer != null)
            {
                if (targetCookingSprite != null)
                {
                    activeRenderer.sprite = IsCooking ? targetCookingSprite : activeOriginalSprite;
                }
                else
                {
                    activeRenderer.sprite = activeOriginalSprite; // Restore if no cooking sprite
                }
            }

            // Toggle any particle effects or active game objects
            if (cookingEffects != null)
            {
                cookingEffects.SetActive(IsCooking);
            }

            // Trigger animator state if a global one is assigned
            if (toolAnimator != null)
            {
                toolAnimator.SetBool("IsCooking", IsCooking);
            }

            // Also trigger animator on the specific visual variation that is currently active (if any)
            if (visualVariations != null && visualVariations.Length > 0)
            {
                foreach (GameObject variation in visualVariations)
                {
                    if (variation != null && variation.activeSelf)
                    {
                        Animator varAnim = variation.GetComponent<Animator>();
                        if (varAnim == null) varAnim = variation.GetComponentInChildren<Animator>();
                        
                        if (varAnim != null)
                        {
                            varAnim.SetBool("IsCooking", IsCooking);
                        }
                    }
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (dishStockIcon != null)
            {
                dishStockIcon.sortingOrder = dishSpriteSortingOrder;
                
                if (dishStockContainer != null && dishStockIcon.gameObject != dishStockContainer)
                {
                    dishStockIcon.transform.localScale = dishSpriteScale;
                }

                // Editor preview: show the first available dish's icon if the game isn't running and it's empty
                if (!Application.isPlaying && dishStockIcon.sprite == null && availableDishes != null && availableDishes.Length > 0 && availableDishes[0] != null)
                {
                    dishStockIcon.sprite = availableDishes[0].icon;
                }
            }
        }
#endif
    }
}
