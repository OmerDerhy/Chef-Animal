using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelSelectButton : MonoBehaviour
{
    [Header("Configuration")]
    public int levelIndex = 1;      
    public string sceneName = "Level 1"; 

    [Header("UI References")]
    public GameObject lockIcon;     
    
    [Header("Cost UI Display")]
    [Tooltip("The parent 'bar' UI object that contains the coin graphic and text.")]
    public GameObject coinBar;
    [Tooltip("The Text object inside the coin bar that shows just the number.")]
    public TextMeshProUGUI costText;

    private Button myButton;
    private Image buttonImage;      
    private LevelSelectionManager manager;

    void Awake()
    {
        myButton = GetComponent<Button>();
        buttonImage = GetComponent<Image>(); 
        manager = FindObjectOfType<LevelSelectionManager>();

        if (myButton != null)
        {
            myButton.onClick.AddListener(OnClicked);
        }
    }

    // FIX: Use OnEnable to refresh every time the menu is shown
    void OnEnable()
    {
        RefreshState();
    }

    public void RefreshState()
    {
        // If PlayerData isn't ready yet, LevelSelectionManager will call this again later
        if (PlayerData.Instance == null) return;

        bool isUnlocked = PlayerData.Instance.IsLevelUnlocked(levelIndex);

        // 1. Handle Lock Icon
        if (lockIcon != null)
        {
            lockIcon.SetActive(!isUnlocked);
        }

        // 2. Handle the whole Coin Bar
        if (coinBar != null)
        {
            coinBar.SetActive(!isUnlocked);
        }

        // 3. Handle Text (only setting the number)
        if (costText != null)
        {
            costText.text = $"{GetCalculatedCost()}";
            // In case the user doesn't use 'coinBar' and directly uses 'costText' 
            costText.gameObject.SetActive(!isUnlocked);
        }

        // 3. Handle Darkening Effect
        if (buttonImage != null)
        {
            buttonImage.color = isUnlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
        }
    }

    public int GetCalculatedCost()
    {
        // Level 1 = 400, Level 2 = 600, Level 3 = 800
        return 200 + (levelIndex * 200);
    }

    void OnClicked()
    {
        if (manager != null)
        {
            manager.HandleLevelClick(this);
        }
    }
}