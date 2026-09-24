using UnityEngine;
using UnityEngine.UI; 
using TMPro;

public class LightsUI : MonoBehaviour
{
    [Tooltip("Drag your UI Text object here")]
    public TextMeshProUGUI LightText; 

    void Start()
    {
        if (PlayerData.Instance != null)
        {
            // Subscribe to the correct event (OnLightsChanged)
            PlayerData.Instance.OnLightsChanged += UpdateLightsDisplay;
            
            // Set initial value
            UpdateLightsDisplay(PlayerData.Instance.GetLights());
        }
    }

    void OnDestroy()
    {
        // Clean up event subscription to prevent errors
        if (PlayerData.Instance != null)
        {
            PlayerData.Instance.OnLightsChanged -= UpdateLightsDisplay;
        }
    }

    private void UpdateLightsDisplay(int amount)
    {
        if (LightText != null)
        {
            // Format: Current / Max (e.g. "50/250")
            LightText.text = $"{amount}/{PlayerData.MAX_LIGHTS}";
        }
    }
}