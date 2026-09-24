using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class OpenLightsShopButton : MonoBehaviour
{
    private Button myButton;

    void Start()
    {
        myButton = GetComponent<Button>();
        myButton.onClick.AddListener(OnClicked);
    }

    void OnClicked()
    {
        // Opens the "Out of Lights" / "Buy Lights" panel manually
        if (CurrencyShop.Instance != null)
        {
            CurrencyShop.Instance.ShowOutOfLights();
        }
    }
}