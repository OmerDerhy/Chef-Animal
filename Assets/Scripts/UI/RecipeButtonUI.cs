using UnityEngine;
using UnityEngine.UI;

namespace MergesChef.UI
{
    public class RecipeButtonUI : MonoBehaviour
    {
        [Header("UI References")]
        public Image dishIcon;
       
        public TMPro.TextMeshProUGUI dishNameTMP;
        
        public TMPro.TextMeshProUGUI costText;
        public TMPro.TextMeshProUGUI revenueText;
        public TMPro.TextMeshProUGUI timeText;
        public TMPro.TextMeshProUGUI yieldText;
        
        public Button cookButton;
    }
}
