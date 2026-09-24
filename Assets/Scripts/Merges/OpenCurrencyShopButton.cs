using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class OpenCurrencyShopButton : MonoBehaviour
{
    private Button myButton;

    void Start()
    {
        myButton = GetComponent<Button>();
        myButton.onClick.AddListener(OnClicked);
    }

    void OnClicked()
    {
        if (CurrencyShop.Instance != null)
        {
            CurrencyShop.Instance.OpenShop();
        }
        else
        {
            Debug.LogError("CurrencyShop Instance not found! Make sure CurrencyShop.cs is in the scene.");
        }
    }
}