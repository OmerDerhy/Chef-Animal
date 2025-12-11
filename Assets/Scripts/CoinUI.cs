using UnityEngine;
using UnityEngine.UI; 
// Note: If you use TextMeshPro, change "Text" to "TMPro.TextMeshProUGUI"

public class CoinUI : MonoBehaviour
{
    [Tooltip("Drag your UI Text object here")]
    public TMPro.TextMeshProUGUI coinText; 

    void Start()
    {
        if (PlayerData.Instance != null)
        {
            // Subscribe to the event so we update immediately when coins change
            PlayerData.Instance.OnCoinsChanged += UpdateCoinDisplay;
            
            // Set initial value
            UpdateCoinDisplay(PlayerData.Instance.GetCoins());
        }
    }

    void OnDestroy()
    {
        // Clean up event subscription to prevent errors
        if (PlayerData.Instance != null)
        {
            PlayerData.Instance.OnCoinsChanged -= UpdateCoinDisplay;
        }
    }

    private void UpdateCoinDisplay(int amount)
    {
        if (coinText != null)
        {
            coinText.text = amount.ToString();
            // You can also do: coinText.text = "Coins: " + amount;
        }
    }
}