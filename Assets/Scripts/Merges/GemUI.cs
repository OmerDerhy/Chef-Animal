using UnityEngine;
using UnityEngine.UI; 
// Note: If you use TextMeshPro, change "Text" to "TMPro.TextMeshProUGUI" and the field type below

public class GemUI : MonoBehaviour
{
    [Tooltip("Drag your UI Text object here")]
    public TMPro.TextMeshProUGUI gemText; 

    void Start()
    {
        if (PlayerData.Instance != null)
        {
            // Subscribe to the event so we update immediately when gems change
            PlayerData.Instance.OnGemsChanged += UpdateGemDisplay;
            
            // Set initial value
            UpdateGemDisplay(PlayerData.Instance.GetGems());
        }
    }

    void OnDestroy()
    {
        // Clean up event subscription to prevent errors
        if (PlayerData.Instance != null)
        {
            PlayerData.Instance.OnGemsChanged -= UpdateGemDisplay;
        }
    }

    private void UpdateGemDisplay(int amount)
    {
        if (gemText != null)
        {
            gemText.text = amount.ToString();
        }
    }
}