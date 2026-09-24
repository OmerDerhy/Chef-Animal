using UnityEngine;
using UnityEngine.UI;

namespace MergesChef.Map
{
    public class MapUIManager : MonoBehaviour
    {
        [Header("References")]
        public Map mapManager;
        
        [Header("UI Elements")]
        public Button editModeButton;
        [Tooltip("A dedicated button to confirm editing and return to Play Mode")]
        public Button confirmEditButton;
        [Tooltip("A dedicated button to cancel editing and revert to the old map state")]
        public Button cancelEditButton;
        
        [Tooltip("Add all the other buttons here that should be disabled when in Build Mode")]
        public Button[] otherButtonsToDisable;
        
        private string preEditStateJson = "";

        private void Start()
        {
            if (mapManager != null)
            {
                mapManager.OnMapValidityChanged += HandleValidityChange;
            }

            if (editModeButton != null)
            {
                // Ensure the button calls our toggle method when clicked
                editModeButton.onClick.AddListener(ToggleEditMode);
            }

            if (confirmEditButton != null)
            {
                // Confirming edit just toggles back to play mode
                confirmEditButton.onClick.AddListener(ToggleEditMode);
            }

            if (cancelEditButton != null)
            {
                cancelEditButton.onClick.AddListener(CancelEdit);
            }
            
            // Set initial state based on whatever mode the map starts in
            UpdateButtonStates();
            
            if (mapManager != null)
            {
                HandleValidityChange(mapManager.IsGridValid);
            }
        }

        private void OnDestroy()
        {
            if (mapManager != null)
            {
                mapManager.OnMapValidityChanged -= HandleValidityChange;
            }
        }

        private void HandleValidityChange(bool isValid)
        {
            if (confirmEditButton != null)
            {
                confirmEditButton.interactable = isValid;
            }
        }

        [ContextMenu("Toggle Edit Mode")]
        public void ToggleEditMode()
        {
            if (mapManager == null) return;

            if (mapManager.GetCurrentMode() == GameMode.PlayMode)
            {
                // Entering Build Mode: Take a snapshot of the current map
                preEditStateJson = mapManager.GetMapJson();
                mapManager.SetMode(GameMode.BuildMode);
                UI.SelectionUIManager.Instance?.HidePanel();
            }
            else
            {
                // Leaving Build Mode: Normal confirm
                MapInputHandler handler = mapManager.GetComponent<MapInputHandler>();
                if (handler != null) handler.ClearDraggingState(false);
                
                mapManager.SetMode(GameMode.PlayMode);
                // Ensure the final state is saved
                mapManager.SaveMap();
            }
            
            UpdateButtonStates();
        }

        public void HandlePurchase(MergesChef.UI.MarketItemData item)
        {
            if (mapManager == null) return;
            
            // 1. Tell map to spawn and place it on cursor (does not add to grid yet)
            mapManager.SpawnAndPlacePurchasedItem(item);
            
            // 2. Enter build mode (takes a snapshot BEFORE item is fully committed to grid)
            if (mapManager.GetCurrentMode() == GameMode.PlayMode)
            {
                preEditStateJson = mapManager.GetMapJson();
                mapManager.SetMode(GameMode.BuildMode);
                UI.SelectionUIManager.Instance?.HidePanel();
                UpdateButtonStates();
            }
        }

        public void CancelEdit()
        {
            if (mapManager == null) return;

            MapInputHandler handler = mapManager.GetComponent<MapInputHandler>();
            if (handler != null) handler.ClearDraggingState(true);

            // 1. Clear the map
            mapManager.ClearMap();

            // 2. Rebuild from snapshot
            MapInitializer initializer = mapManager.GetComponent<MapInitializer>();
            if (initializer != null && !string.IsNullOrEmpty(preEditStateJson))
            {
                initializer.LoadMapFromJson(preEditStateJson);
            }
            else
            {
                Debug.LogWarning("[MapUIManager] Could not find MapInitializer or snapshot was empty.");
            }

            // 3. Ensure the save file matches the reverted state
            mapManager.SaveMap();

            // 4. Return to play mode
            mapManager.SetMode(GameMode.PlayMode);
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            if (mapManager == null) return;

            bool isBuildMode = (mapManager.GetCurrentMode() == GameMode.BuildMode);

            // Change visual text/color of the edit button so the user knows what mode they are in
            if (editModeButton != null)
            {
                Text buttonText = editModeButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = isBuildMode ? "Finish Editing" : "Edit Mode";
                }
                else
                {
                    // Check for TextMeshPro instead
                    var tmpText = editModeButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (tmpText != null)
                    {
                        tmpText.text = isBuildMode ? "Finish Editing" : "Edit Mode";
                    }
                }
            }

            // Show or hide the Confirm/Cancel buttons based on mode
            if (confirmEditButton != null)
            {
                confirmEditButton.gameObject.SetActive(isBuildMode);
            }
            if (cancelEditButton != null)
            {
                cancelEditButton.gameObject.SetActive(isBuildMode);
            }

            // Disable or enable all other buttons
            foreach (Button btn in otherButtonsToDisable)
            {
                if (btn != null)
                {
                    btn.interactable = !isBuildMode;
                }
            }
        }
    }
}
