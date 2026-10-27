using UnityEngine;
using UnityEngine.UI;
using MergesChef.Map;
using MergesChef.Feel;

namespace MergesChef.UI
{
    public class CookingUIManager : MonoBehaviour
    {
        public static CookingUIManager Instance;

        [Header("UI Panels")]
        public GameObject cookingPanel;
        
        [Header("UI Elements")]
        public Transform recipesContainer; // The layout group to spawn recipe buttons in
        public GameObject recipeButtonPrefab; // The button prefab to instantiate
        
        [Tooltip("Use this to shrink or grow the dish image from code!")]
        public float dishIconScale = 1.0f;
        
        public Button closeButton;

        private KitchenAccessoryVisual currentAccessory;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (cookingPanel != null) cookingPanel.SetActive(false);
            if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
            JuicyButton.Attach(closeButton);
        }

        public void OpenPanel(KitchenAccessoryVisual accessory)
        {
            if (accessory == null) return;
            
            currentAccessory = accessory;

            if (cookingPanel != null) UIPopAnimator.Show(cookingPanel);

            PopulateRecipes(accessory.availableDishes);
            
            // Reset the scroll position to the top
            if (recipesContainer != null)
            {
                RectTransform rt = recipesContainer.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, 0);
                }
            }
        }

        public void ClosePanel()
        {
            if (cookingPanel != null) UIPopAnimator.Hide(cookingPanel);
            currentAccessory = null;
        }

        private void PopulateRecipes(Dish[] dishes)
        {
            // Clear old buttons
            foreach (Transform child in recipesContainer)
            {
                Destroy(child.gameObject);
            }

            if (dishes == null || dishes.Length == 0)
            {
                Debug.LogWarning("[CookingUIManager] No dishes available on this kitchen accessory!");
                return;
            }

            // Spawn new buttons
            int recipeIndex = 0;
            foreach (Dish dish in dishes)
            {
                if (dish == null) continue;

                GameObject btnObj = Instantiate(recipeButtonPrefab, recipesContainer, false);
                
                // Force the scale back to normal so it doesn't spawn invisible
                btnObj.transform.localScale = Vector3.one;

                // Cascade the recipe list in (captures the scale set on the line above as its rest pose).
                Juice.PopInListItem(btnObj, Juice.StaggerDelay(recipeIndex));
                recipeIndex++;
                // We leave localPosition alone because the Vertical Layout Group handles positioning

                RecipeButtonUI ui = btnObj.GetComponent<RecipeButtonUI>();

                if (ui != null)
                {
                    int currentLevel = currentAccessory != null && currentAccessory.LinkedAccessory != null ? currentAccessory.LinkedAccessory.Level : 1;
                    bool isLocked = currentLevel < dish.requiredLevel;

                    if (ui.dishNameTMP != null)
                    {
                        ui.dishNameTMP.text = isLocked ? $"{dish.dishName} (Requires Level {dish.requiredLevel})" : dish.dishName;
                    }

                    if (ui.costText != null) ui.costText.text = dish.cost.ToString();
                    if (ui.revenueText != null) ui.revenueText.text = dish.revenuePerDish.ToString();
                    if (ui.timeText != null) ui.timeText.text = dish.timeToCook.ToString() + "s";
                    if (ui.yieldText != null) ui.yieldText.text = dish.amountOfDishes.ToString();
                        
                    if (ui.dishIcon == null) 
                    {
                        Debug.LogError("[CookingUIManager] ui.dishIcon is NULL! You forgot to assign the Image in the RecipeButtonUI script on the Prefab!");
                    }
                    else if (dish.icon == null) 
                    {
                        Debug.LogError($"[CookingUIManager] dish.icon is NULL! The Dish '{dish.dishName}' does not have a Sprite assigned in the Project folder!");
                    }
                    else 
                    {
                        ui.dishIcon.sprite = dish.icon;
                        Debug.Log($"[CookingUIManager] Successfully set sprite '{dish.icon.name}' to image! Scale is {ui.dishIcon.transform.localScale}, Alpha is {ui.dishIcon.color.a}");
                        
                        // Force alpha to 1 just in case it was accidentally transparent, but dim it out if locked
                        Color c = ui.dishIcon.color;
                        c.a = isLocked ? 0.4f : 1f;
                        ui.dishIcon.color = c;
                    }

                    if (ui.cookButton != null)
                    {
                        JuicyButton.Attach(ui.cookButton);
                        ui.cookButton.interactable = !isLocked;
                        if (!isLocked)
                        {
                            ui.cookButton.onClick.AddListener(() => OnDishSelected(dish));
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("[CookingUIManager] RecipeButtonPrefab is missing the RecipeButtonUI script!");
                }
            }
        }

        private void OnDishSelected(Dish dish)
        {
            Debug.Log($"[CookingUIManager] Selected {dish.dishName} to cook on {currentAccessory.gameObject.name}!");
            
            if (currentAccessory != null)
            {
                currentAccessory.StartCooking(dish);
            }
            
            ClosePanel();
        }
    }
}
