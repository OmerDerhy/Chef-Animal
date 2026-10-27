using UnityEngine;
using UnityEngine.UI;
using MergesChef.Map;
using MergesChef.Feel;

namespace MergesChef.UI
{
    public class SelectionUIManager : MonoBehaviour
    {
        public static SelectionUIManager Instance { get; private set; }

        [Header("Main Action Panel")]
        public GameObject selectionPanel;
        public Button upgradeButton;
        public Button cookButton;
        public Button flipButton;

        [Header("Upgrade Confirmation Panel")]
        public GameObject upgradeConfirmationPanel;
        public TMPro.TextMeshProUGUI upgradeTargetText;
        public UnityEngine.UI.Image upgradeIconImage;
        public TMPro.TextMeshProUGUI upgradePriceText;
        public Button confirmUpgradeButton;
        public Button cancelUpgradeButton;

        private AccessoryVisual currentAccessory;
        private AreaVisual currentArea;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (selectionPanel != null) selectionPanel.SetActive(false);
            if (upgradeButton != null) upgradeButton.gameObject.SetActive(false);
            if (cookButton != null) cookButton.gameObject.SetActive(false);
            if (flipButton != null) flipButton.gameObject.SetActive(false);
            if (upgradeConfirmationPanel != null) upgradeConfirmationPanel.SetActive(false);

            if (upgradeButton != null) upgradeButton.onClick.AddListener(OnUpgradeClicked);
            if (cookButton != null) cookButton.onClick.AddListener(OnCookClicked);
            if (flipButton != null) flipButton.onClick.AddListener(OnFlipClicked);

            if (confirmUpgradeButton != null) confirmUpgradeButton.onClick.AddListener(ConfirmUpgrade);
            if (cancelUpgradeButton != null) cancelUpgradeButton.onClick.AddListener(CloseUpgradePanel);

            JuicyButton.Attach(upgradeButton, cookButton, flipButton,
                               confirmUpgradeButton, cancelUpgradeButton);
        }

        public void ShowPanel(AccessoryVisual accessory, AreaVisual area)
        {
            currentAccessory = accessory;
            currentArea = area;

            if (selectionPanel != null) UIPopAnimator.Show(selectionPanel);
            if (upgradeButton != null) upgradeButton.gameObject.SetActive(true);
            if (upgradeConfirmationPanel != null) UIPopAnimator.Hide(upgradeConfirmationPanel);

            // Show Cook button only if it's a Kitchen Accessory. Flip is Build Mode only (see ShowBuildModeFlip).
            bool isKitchenAccessory = accessory != null && accessory is KitchenAccessoryVisual;
            if (cookButton != null)
            {
                cookButton.gameObject.SetActive(isKitchenAccessory);
            }
            if (flipButton != null)
            {
                flipButton.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Called from Build Mode when a kitchen accessory is selected. Shows only the Flip button
        /// (no Upgrade/Cook, which are Play Mode actions).
        /// </summary>
        public void ShowBuildModeFlip(AccessoryVisual accessory)
        {
            currentAccessory = accessory;
            currentArea = null;

            bool isKitchenAccessory = accessory != null && accessory is KitchenAccessoryVisual;

            if (selectionPanel != null)
            {
                if (isKitchenAccessory) UIPopAnimator.Show(selectionPanel);
                else UIPopAnimator.Hide(selectionPanel);
            }
            if (upgradeButton != null) upgradeButton.gameObject.SetActive(false);
            if (cookButton != null) cookButton.gameObject.SetActive(false);
            if (flipButton != null) flipButton.gameObject.SetActive(isKitchenAccessory);
            if (upgradeConfirmationPanel != null) UIPopAnimator.Hide(upgradeConfirmationPanel);
        }

        /// <summary>
        /// Called from Build Mode when an Area is selected. Just shows the selection
        /// highlight - no Upgrade button, since upgrading is a Play Mode-only action.
        /// </summary>
        public void ShowBuildModeAreaPanel(AreaVisual area)
        {
            currentAccessory = null;
            currentArea = area;

            if (selectionPanel != null) UIPopAnimator.Hide(selectionPanel);
            if (upgradeButton != null) upgradeButton.gameObject.SetActive(false);
            if (cookButton != null) cookButton.gameObject.SetActive(false);
            if (flipButton != null) flipButton.gameObject.SetActive(false);
            if (upgradeConfirmationPanel != null) UIPopAnimator.Hide(upgradeConfirmationPanel);
        }

        public void HidePanel()
        {
            currentAccessory = null;
            currentArea = null;
            if (selectionPanel != null) UIPopAnimator.Hide(selectionPanel);
            if (upgradeButton != null) upgradeButton.gameObject.SetActive(false);
            if (cookButton != null) cookButton.gameObject.SetActive(false);
            if (flipButton != null) flipButton.gameObject.SetActive(false);
            if (upgradeConfirmationPanel != null) UIPopAnimator.Hide(upgradeConfirmationPanel);
        }

        private void OnUpgradeClicked()
        {
            if (upgradeConfirmationPanel != null)
            {
                UIPopAnimator.Show(upgradeConfirmationPanel);

                string targetName = "Object";
                int currentLevel = 1;
                int upgradeCost = 0;
                Sprite targetSprite = null;

                if (currentAccessory != null && currentAccessory.LinkedAccessory != null)
                {
                    targetName = currentAccessory.LinkedAccessory.AccessoryName;
                    if (string.IsNullOrEmpty(targetName)) targetName = currentAccessory.LinkedAccessory.GetType().Name;
                    currentLevel = currentAccessory.LinkedAccessory.Level;
                    upgradeCost = currentAccessory.LinkedAccessory.CostToUpgrade;
                    
                    // Try to get the NEXT variation sprite (currentLevel corresponds to index for next level since level starts at 1)
                    targetSprite = currentAccessory.GetVariationSprite(currentLevel);
                    
                    // Fallback to current sprite if there is no next variation setup
                    if (targetSprite == null)
                    {
                        var spriteRenderer = currentAccessory.GetComponentInChildren<SpriteRenderer>();
                        if (spriteRenderer != null) targetSprite = spriteRenderer.sprite;
                    }
                }
                else if (currentArea != null && currentArea.LinkedArea != null)
                {
                    targetName = currentArea.LinkedArea.GetType().Name;
                    currentLevel = currentArea.LinkedArea.Level;
                    upgradeCost = currentArea.LinkedArea.CostToUpgrade;

                    // Preview the NEXT level's floor art - each level is its own prefab, so peek at
                    // that prefab asset's sprite directly without instantiating it.
                    if (MapInitializer.Instance != null)
                    {
                        GameObject nextPrefab = MapInitializer.Instance.GetAreaPrefab(currentArea.LinkedArea.Type, currentLevel + 1);
                        if (nextPrefab != null)
                        {
                            var nextRenderer = nextPrefab.GetComponentInChildren<SpriteRenderer>(true);
                            if (nextRenderer != null) targetSprite = nextRenderer.sprite;
                        }
                    }

                    // Fallback to current sprite if there is no next level art configured.
                    if (targetSprite == null)
                    {
                        var spriteRenderer = currentArea.GetComponentInChildren<SpriteRenderer>();
                        if (spriteRenderer != null) targetSprite = spriteRenderer.sprite;
                    }
                }

                if (upgradeTargetText != null)
                {
                    upgradeTargetText.text = $"Upgrade {targetName}\nNext Level: {currentLevel + 1}";
                }
                
                if (upgradeIconImage != null)
                {
                    if (targetSprite != null)
                    {
                        upgradeIconImage.sprite = targetSprite;
                        upgradeIconImage.preserveAspect = true;
                        upgradeIconImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        upgradeIconImage.gameObject.SetActive(false);
                    }
                }
                
                if (upgradePriceText != null)
                {
                    upgradePriceText.text = upgradeCost.ToString();
                }
            }
        }

        private void OnCookClicked()
        {
            if (currentAccessory is KitchenAccessoryVisual kitchenAcc)
            {
                if (CookingUIManager.Instance != null)
                {
                    CookingUIManager.Instance.OpenPanel(kitchenAcc);
                }
            }
        }

        private void OnFlipClicked()
        {
            if (currentAccessory != null && currentAccessory.LinkedAccessory != null)
            {
                currentAccessory.LinkedAccessory.ToggleFlip();
                Debug.Log($"[SelectionUIManager] Flipped {currentAccessory.LinkedAccessory.GetType().Name} - now {(currentAccessory.LinkedAccessory.IsFlipped ? "180" : "0")} degrees.");
            }
        }

        private void ConfirmUpgrade()
        {
            int cost = 0;
            if (currentAccessory != null && currentAccessory.LinkedAccessory != null) cost = currentAccessory.LinkedAccessory.CostToUpgrade;
            else if (currentArea != null && currentArea.LinkedArea != null) cost = currentArea.LinkedArea.CostToUpgrade;
            
            // Spend coins if PlayerData is available
            if (PlayerData.Instance != null && !PlayerData.Instance.SpendCoins(cost))
            {
                Debug.LogWarning("[SelectionUIManager] Not enough coins to upgrade!");
                // Shake the price rather than closing silently, so the refusal is legible.
                if (upgradePriceText != null) Juice.Shake(upgradePriceText.transform, 12f, 0.35f, unscaled: true);
                return;
            }

            if (currentAccessory != null && currentAccessory.LinkedAccessory != null)
            {
                currentAccessory.LinkedAccessory.Upgrade();
                Debug.Log($"[SelectionUIManager] Upgraded {currentAccessory.LinkedAccessory.GetType().Name} to Level {currentAccessory.LinkedAccessory.Level}");
            }
            else if (currentArea != null && currentArea.LinkedArea != null)
            {
                currentArea.LinkedArea.Upgrade();
                Debug.Log($"[SelectionUIManager] Upgraded {currentArea.LinkedArea.GetType().Name} to Level {currentArea.LinkedArea.Level}");
            }
            
            CloseUpgradePanel();
            HidePanel(); // Optionally hide the whole thing after upgrade
        }

        private void CloseUpgradePanel()
        {
            if (upgradeConfirmationPanel != null) UIPopAnimator.Hide(upgradeConfirmationPanel);
        }
    }
}
