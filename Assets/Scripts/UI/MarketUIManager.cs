using UnityEngine;
using UnityEngine.UI;
using MergesChef.Feel;

namespace MergesChef.UI
{
    public class MarketUIManager : MonoBehaviour
    {
        public static MarketUIManager Instance;

        [Header("Market UI Elements")]
        [Tooltip("The main market panel that will be shown/hidden")]
        public GameObject marketPanel;
        
        [Tooltip("Reference to the map UI to handle purchases")]
        public MergesChef.Map.MapUIManager mapUIManager;
        
        [Tooltip("The button used to open the market panel")]
        public Button openButton;

        [Tooltip("The button inside the panel used to close it")]
        public Button closeButton;

        [Header("Dynamic Scroll View")]
        [Tooltip("The Content object inside the Scroll View where items will spawn")]
        public Transform contentContainer;
        [Tooltip("The UI prefab for a single market item (needs MarketItemUI script attached)")]
        public GameObject itemPrefab;

        [Header("Category Buttons")]
        public Button kitchenCategoryButton;
        public Button restaurantCategoryButton;
        public Button floorCategoryButton;
        public Button starsCategoryButton;
        public Button animalsCategoryButton;

        [Header("Category Selection")]
        public float selectedButtonOffset = 20f;
        private System.Collections.Generic.Dictionary<Button, Vector2> originalButtonPositions = new System.Collections.Generic.Dictionary<Button, Vector2>();

        [Header("Market Items by Category")]
        public MarketItemData[] kitchenItems;
        public MarketItemData[] restaurantItems;
        public MarketItemData[] floorItems;
        public MarketItemData[] starsItems;
        public MarketItemData[] animalsItems;

        [Header("Confirmation Panel")]
        [Tooltip("The panel that asks 'Are you sure you want to buy?'")]
        public GameObject confirmationPanel;
        public TMPro.TextMeshProUGUI confirmNameText;
        public Image confirmIconImage;
        public TMPro.TextMeshProUGUI confirmPriceText;
        public Button confirmBuyButton;
        public Button confirmCancelButton;

        [Header("Mystery Gift Purchase Panel")]
        [Tooltip("The panel offering '25 Gems' or 'Watch Ad' for a Mystery Gift item")]
        public GameObject mysteryGiftPurchasePanel;
        public TMPro.TextMeshProUGUI mysteryGiftPriceText;
        public Button spendGemsButton;
        public Button watchAdButton;
        public TMPro.TextMeshProUGUI watchAdCooldownText;
        public Button mysteryGiftCancelButton;

        private MarketItemData currentSelectedItem;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            // Record original button positions so we can move them back
            RecordOriginalPosition(kitchenCategoryButton);
            RecordOriginalPosition(restaurantCategoryButton);
            RecordOriginalPosition(floorCategoryButton);
            RecordOriginalPosition(starsCategoryButton);
            RecordOriginalPosition(animalsCategoryButton);

            // Ensure the panel is closed when the game starts
            if (marketPanel != null)
            {
                marketPanel.SetActive(false);
            }

            if (confirmationPanel != null)
            {
                confirmationPanel.SetActive(false);
            }

            if (mysteryGiftPurchasePanel != null)
            {
                mysteryGiftPurchasePanel.SetActive(false);
            }

            // Hook up the open button listener
            if (openButton != null)
            {
                openButton.onClick.AddListener(OpenMarket);
            }
            else
            {
                Debug.LogWarning("[MarketUIManager] Open Button is not assigned in the Inspector!");
            }

            // Hook up the close button listener
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseMarket);
            }
            else
            {
                Debug.LogWarning("[MarketUIManager] Close Button is not assigned in the Inspector!");
            }

            // Hook up category buttons to load their respective arrays AND pass the button itself
            if (kitchenCategoryButton != null) kitchenCategoryButton.onClick.AddListener(() => LoadCategoryItems(kitchenItems, kitchenCategoryButton));
            if (restaurantCategoryButton != null) restaurantCategoryButton.onClick.AddListener(() => LoadCategoryItems(restaurantItems, restaurantCategoryButton));
            if (floorCategoryButton != null) floorCategoryButton.onClick.AddListener(() => LoadCategoryItems(floorItems, floorCategoryButton));
            if (starsCategoryButton != null) starsCategoryButton.onClick.AddListener(() => LoadCategoryItems(starsItems, starsCategoryButton));
            if (animalsCategoryButton != null) animalsCategoryButton.onClick.AddListener(() => LoadCategoryItems(animalsItems, animalsCategoryButton));

            // Hook up Confirmation buttons
            if (confirmCancelButton != null)
            {
                confirmCancelButton.onClick.AddListener(CloseConfirmationPanel);
            }
            
            if (confirmBuyButton != null)
            {
                confirmBuyButton.onClick.AddListener(ConfirmPurchase);
            }

            // Hook up Mystery Gift purchase buttons
            if (mysteryGiftCancelButton != null)
            {
                mysteryGiftCancelButton.onClick.AddListener(CloseMysteryGiftPurchasePanel);
            }

            if (spendGemsButton != null)
            {
                spendGemsButton.onClick.AddListener(OnSpendGemsForMysteryGift);
            }

            if (watchAdButton != null)
            {
                watchAdButton.onClick.AddListener(OnWatchAdForMysteryGift);
            }

            // Press feedback for every button this panel owns. Added in code so the existing
            // prefabs and their onClick wiring stay untouched.
            JuicyButton.Attach(openButton, closeButton,
                               kitchenCategoryButton, restaurantCategoryButton, floorCategoryButton,
                               starsCategoryButton, animalsCategoryButton,
                               confirmBuyButton, confirmCancelButton,
                               spendGemsButton, watchAdButton, mysteryGiftCancelButton);
        }

        private void RecordOriginalPosition(Button btn)
        {
            if (btn != null)
            {
                RectTransform rect = btn.GetComponent<RectTransform>();
                if (rect != null)
                {
                    originalButtonPositions[btn] = rect.anchoredPosition;
                }
            }
        }

        private void HighlightSelectedButton(Button selectedBtn)
        {
            // Reset all buttons to original positions
            foreach (var kvp in originalButtonPositions)
            {
                if (kvp.Key != null && kvp.Key != selectedBtn)
                {
                    // The Button is the tween id; it is never a tween target itself, so killing by it
                    // can't clobber the press-scale tween living on the same transform.
                    Juice.SlideAnchored(kvp.Key.GetComponent<RectTransform>(), kvp.Value, 0.22f, kvp.Key);
                }
            }

            // Move the selected button slightly to the right
            if (selectedBtn != null && originalButtonPositions.ContainsKey(selectedBtn))
            {
                Juice.SlideAnchored(selectedBtn.GetComponent<RectTransform>(),
                                    originalButtonPositions[selectedBtn] + new Vector2(selectedButtonOffset, 0),
                                    0.22f, selectedBtn);
            }
        }

        public void OpenMarket()
        {
            if (marketPanel != null)
            {
                UIPopAnimator.Show(marketPanel);
                // Optionally show the first category by default when opening
                LoadCategoryItems(kitchenItems, kitchenCategoryButton);
            }
        }

        public void CloseMarket()
        {
            if (marketPanel != null)
            {
                UIPopAnimator.Hide(marketPanel);
            }
            CloseConfirmationPanel();
            CloseMysteryGiftPurchasePanel();
        }

        public void LoadCategoryItems(MarketItemData[] itemsToLoad, Button selectedCategoryButton)
        {
            HighlightSelectedButton(selectedCategoryButton);

            if (contentContainer == null || itemPrefab == null)
            {
                Debug.LogError("[MarketUIManager] Content Container or Item Prefab is not assigned!");
                return;
            }

            // 1. Destroy old items currently in the list
            foreach (Transform child in contentContainer)
            {
                Destroy(child.gameObject);
            }

            // 2. Spawn new items for the selected category
            if (itemsToLoad != null)
            {
                int index = 0;
                foreach (MarketItemData item in itemsToLoad)
                {
                    GameObject spawnedItem = Instantiate(itemPrefab, contentContainer);
                    MarketItemUI itemUI = spawnedItem.GetComponent<MarketItemUI>();
                    if (itemUI != null)
                    {
                        itemUI.Setup(item, this);
                    }
                    else
                    {
                        Debug.LogWarning("[MarketUIManager] The itemPrefab is missing the MarketItemUI component!");
                    }

                    // Cascade the list in rather than having it appear all at once.
                    Juice.PopInListItem(spawnedItem, Juice.StaggerDelay(index));
                    index++;
                }
            }
        }

        public void ShowConfirmationPanel(MarketItemData itemData)
        {
            currentSelectedItem = itemData;

            if (itemData.isMysteryGift)
            {
                ShowMysteryGiftPurchasePanel(itemData);
                return;
            }

            if (confirmationPanel != null)
            {
                UIPopAnimator.Show(confirmationPanel);

                if (confirmNameText != null) confirmNameText.text = itemData.itemName;
                if (confirmPriceText != null) confirmPriceText.text = itemData.price.ToString();
                if (confirmIconImage != null)
                {
                    confirmIconImage.sprite = itemData.itemIcon;
                    confirmIconImage.preserveAspect = true;
                    confirmIconImage.gameObject.SetActive(itemData.itemIcon != null);
                }
            }
        }

        public void CloseConfirmationPanel()
        {
            if (confirmationPanel != null)
            {
                UIPopAnimator.Hide(confirmationPanel);
            }
        }

        private void ShowMysteryGiftPurchasePanel(MarketItemData itemData)
        {
            if (mysteryGiftPurchasePanel == null) return;

            UIPopAnimator.Show(mysteryGiftPurchasePanel);

            if (mysteryGiftPriceText != null) mysteryGiftPriceText.text = itemData.price.ToString();

            bool adAvailable = PlayerData.Instance == null || PlayerData.Instance.GetTimeSinceLastMysteryGiftAd().TotalHours >= 24.0;
            if (watchAdButton != null) watchAdButton.interactable = adAvailable;
            if (watchAdCooldownText != null)
            {
                if (adAvailable)
                {
                    watchAdCooldownText.text = "Watch Ad";
                }
                else
                {
                    System.TimeSpan remaining = System.TimeSpan.FromHours(24.0) - PlayerData.Instance.GetTimeSinceLastMysteryGiftAd();
                    watchAdCooldownText.text = $"Come back in {remaining.Hours}h {remaining.Minutes}m";
                }
            }
        }

        public void CloseMysteryGiftPurchasePanel()
        {
            if (mysteryGiftPurchasePanel != null)
            {
                UIPopAnimator.Hide(mysteryGiftPurchasePanel);
            }
        }

        private void OnSpendGemsForMysteryGift()
        {
            if (PlayerData.Instance != null && !PlayerData.Instance.SpendGems(currentSelectedItem.price))
            {
                Debug.LogWarning("[Market] Not enough gems for Mystery Gift!");
                return;
            }

            CloseMysteryGiftPurchasePanel();
            CloseMarket();

            if (MysteryGiftManager.Instance != null)
            {
                MysteryGiftManager.Instance.OpenMysteryGift();
            }
        }

        private void OnWatchAdForMysteryGift()
        {
            if (CurrencyShop.Instance == null)
            {
                Debug.LogWarning("[Market] CurrencyShop not available, cannot watch ad for Mystery Gift.");
                return;
            }

            CurrencyShop.Instance.WatchAdForMysteryGift(() =>
            {
                CloseMysteryGiftPurchasePanel();
                CloseMarket();

                if (MysteryGiftManager.Instance != null)
                {
                    MysteryGiftManager.Instance.OpenMysteryGift();
                }
            });
        }

        private void ConfirmPurchase()
        {
            if (PlayerData.Instance != null)
            {
                if (!PlayerData.Instance.SpendCoins(currentSelectedItem.price))
                {
                    Debug.LogWarning("[Market] Not enough coins!");
                    // Shake the price instead of closing silently, so the refusal is legible.
                    if (confirmPriceText != null) Juice.Shake(confirmPriceText.transform, 12f, 0.35f, unscaled: true);
                    return;
                }
            }

            Debug.Log($"[Market] Purchased: {currentSelectedItem.itemName} for {currentSelectedItem.price} coins!");
            
            if (mapUIManager != null)
            {
                mapUIManager.HandlePurchase(currentSelectedItem);
            }
            
            CloseConfirmationPanel();
            CloseMarket(); 
        }

        public MarketItemData? GetItemDataByName(string itemName)
        {
            MarketItemData[][] allCategories = new MarketItemData[][] {
                kitchenItems, restaurantItems, floorItems, starsItems, animalsItems
            };

            foreach (var category in allCategories)
            {
                if (category == null) continue;
                foreach (var item in category)
                {
                    if (item.itemName == itemName) return item;
                }
            }
            return null;
        }
    }
}
