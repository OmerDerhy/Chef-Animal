using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasGroup))]
public class UIPanelDimmer : MonoBehaviour
{
    private static GameObject dimmerOverlay;
    private static List<UIPanelDimmer> activePanels = new List<UIPanelDimmer>();

    // You can customize the dim color in the inspector
    [Header("Dim Settings")]
    [Tooltip("The color and transparency of the dark background")]
    public Color dimColor = new Color(0, 0, 0, 0.7f);

    private void OnEnable()
    {
        // Clean up any dead references before adding ourselves
        activePanels.RemoveAll(p => p == null);
        
        if (dimmerOverlay == null)
            CreateDimmerOverlay();

        // Make sure this panel isn't already tracked
        if (!activePanels.Contains(this))
            activePanels.Add(this);

        UpdateDimmerOverlay();
    }

    private void OnDisable()
    {
        if (activePanels.Contains(this))
            activePanels.Remove(this);

        UpdateDimmerOverlay();
    }

    private void OnDestroy()
    {
        // Cleanup when panel is destroyed to prevent ghost entries
        if (activePanels.Contains(this))
            activePanels.Remove(this);

        if (activePanels.Count == 0 && dimmerOverlay != null)
        {
            Destroy(dimmerOverlay);
            dimmerOverlay = null;
        }
        else
        {
            UpdateDimmerOverlay();
        }
    }

    private void CreateDimmerOverlay()
    {
        if (dimmerOverlay != null) return; // double check

        // Find the topmost Canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) 
        {
            canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("UIPanelDimmer: No Canvas found in the scene to place the dark overlay.");
                return;
            }
        }

        dimmerOverlay = new GameObject("AutoDarkOverlay");
        
        // Critical Fix: Add exactly to the Canvas root so it stretches properly
        dimmerOverlay.transform.SetParent(canvas.transform, false);
        
        // Add full screen Image component
        Image img = dimmerOverlay.AddComponent<Image>();
        img.color = dimColor;
        img.raycastTarget = true; // This blocks clicks from passing through to the game
        
        // Make the overlay enormous to cover the entire screen space regardless of parent size
        RectTransform rect = dimmerOverlay.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(20000f, 20000f);
        rect.localPosition = Vector3.zero;
        
        dimmerOverlay.SetActive(false);
    }

    private static void UpdateDimmerOverlay()
    {
        // Clean up any accidentally destroyed panels
        activePanels.RemoveAll(p => p == null);

        // Check if the overlay itself was somehow destroyed (e.g. changing scenes)
        if (activePanels.Count > 0 && dimmerOverlay == null)
        {
            activePanels[0].CreateDimmerOverlay(); 
        }

        if (dimmerOverlay == null) return;

        if (activePanels.Count > 0)
        {
            dimmerOverlay.SetActive(true);
            
            // Get the topmost active panel (the one that opened last)
            UIPanelDimmer topmostPanel = activePanels[activePanels.Count - 1];
            
            // Critical Fix: Only move the overlay if the topmost panel really has a parent
            if (topmostPanel != null && topmostPanel.transform.parent != null)
            {
                if (dimmerOverlay.transform.parent != topmostPanel.transform.parent)
                {
                    dimmerOverlay.transform.SetParent(topmostPanel.transform.parent, false);
                    
                    // Make the overlay enormous to cover the entire screen space regardless of parent size
                    RectTransform rect = dimmerOverlay.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        rect.anchorMin = new Vector2(0.5f, 0.5f);
                        rect.anchorMax = new Vector2(0.5f, 0.5f);
                        rect.sizeDelta = new Vector2(20000f, 20000f);
                        rect.localPosition = Vector3.zero;
                    }
                }

                // Force the topmost panel to render completely on top of everything else
                topmostPanel.transform.SetAsLastSibling();
                
                // Then force the dark overlay to render precisely one step behind it
                dimmerOverlay.transform.SetAsLastSibling();
                topmostPanel.transform.SetAsLastSibling();
            }
            
            // Ensure the dimmer color updates
            Image img = dimmerOverlay.GetComponent<Image>();
            if (img != null && topmostPanel != null) img.color = topmostPanel.dimColor;
        }
        else
        {
            // No panels are open, just hide the background
            dimmerOverlay.SetActive(false);
        }
    }
}
