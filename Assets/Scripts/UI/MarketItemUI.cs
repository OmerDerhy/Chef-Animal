using UnityEngine;
using UnityEngine.UI;

namespace MergesChef.UI
{
    [System.Serializable]
    public struct MarketItemData
    {
        public string itemName;
        public Sprite itemIcon;
        public int price;
        public GameObject prefabToPlace; // What to spawn when bought
        public bool isMysteryGift; // If true, purchasing opens the Mystery Gift mini-game instead of placing prefabToPlace
    }

    public class MarketItemUI : MonoBehaviour
    {
        public Image iconImage;
        public TMPro.TextMeshProUGUI nameText; 
        public TMPro.TextMeshProUGUI priceText;
        [Tooltip("The Button component on this Market Block itself")]
        public Button mainButton;

        private MarketUIManager manager;

        public void Setup(MarketItemData itemData, MarketUIManager managerRef)
        {
            manager = managerRef;

            if (iconImage != null && itemData.itemIcon != null)
            {
                iconImage.sprite = itemData.itemIcon;
                iconImage.preserveAspect = true;
            }

            if (nameText != null)
                nameText.text = itemData.itemName;

            if (priceText != null)
                priceText.text = itemData.price.ToString();

            if (mainButton != null)
            {
                mainButton.onClick.RemoveAllListeners();
                mainButton.onClick.AddListener(() => OnBlockClicked(itemData));
            }
        }

        private void OnBlockClicked(MarketItemData itemData)
        {
            if (manager != null)
            {
                manager.ShowConfirmationPanel(itemData);
            }
        }
    }
}
