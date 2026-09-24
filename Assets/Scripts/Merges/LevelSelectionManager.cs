using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LevelSelectionManager : MonoBehaviour
{
    [Header("Pop-up UI")]
    public GameObject buyPanel;
    [Tooltip("The description text, e.g. 'Unlock Level 2?'")]
    public TextMeshProUGUI buyText;
    [Tooltip("The text inside your coin bar that shows ONLY the cost number (400, 600, etc)")]
    public TextMeshProUGUI unlockCostText;
    public Button confirmButton;
    public Button closeButton;

    private LevelSelectButton selectedLevel;

    void Start()
    {
        // --- FIX 1: Auto-create PlayerData if missing on Game Start ---
        if (PlayerData.Instance == null)
        {
            GameObject go = new GameObject("DataManager");
            go.AddComponent<PlayerData>();
            // PlayerData.Awake() runs here, loading the save files
        }

        if (buyPanel != null) buyPanel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
        if (confirmButton != null) confirmButton.onClick.AddListener(BuySelectedLevel);

        // --- FIX 2: Force all buttons to update visuals now that Data exists ---
        RefreshAllButtons();
    }



    private void RefreshAllButtons()
    {
        LevelSelectButton[] allButtons = FindObjectsOfType<LevelSelectButton>();
        foreach (var btn in allButtons)
        {
            btn.RefreshState();
        }
    }

    public void HandleLevelClick(LevelSelectButton levelBtn)
    {
        if (PlayerData.Instance == null) return;

        bool isUnlocked = PlayerData.Instance.IsLevelUnlocked(levelBtn.levelIndex);
        Debug.Log($"[LevelManager] Clicked Level {levelBtn.levelIndex}. Unlocked status: {isUnlocked}");

        if (isUnlocked)
        {
            if (string.IsNullOrEmpty(levelBtn.sceneName)) return;

            if (Application.CanStreamedLevelBeLoaded(levelBtn.sceneName))
            {
                if (CurrencyShop.Instance != null)
                {
                    // Show an ad before loading the scene
                    CurrencyShop.Instance.ShowInterstitialAd(() => {
                        SceneManager.LoadScene(levelBtn.sceneName);
                    });
                }
                else
                {
                    // Fallback if shop is missing
                    SceneManager.LoadScene(levelBtn.sceneName);
                }
            }
            else
            {
                Debug.LogError($"[LevelManager] Scene '{levelBtn.sceneName}' not found in Build Settings.");
            }
        }
        else
        {
            // Sequential Check
            if (levelBtn.levelIndex > 1 && !PlayerData.Instance.IsLevelUnlocked(levelBtn.levelIndex - 1))
            {
                Debug.Log("Complete previous level first!");
                return; 
            }

            OpenBuyPanel(levelBtn);
        }
    }

    private void OpenBuyPanel(LevelSelectButton levelBtn)
    {
        selectedLevel = levelBtn;
        
        if (buyPanel != null)
        {
            if (buyText != null) 
                buyText.text = $"Unlock Level {levelBtn.levelIndex}?"; // Description in a few words
                
            if (unlockCostText != null) 
                unlockCostText.text = levelBtn.GetCalculatedCost().ToString(); // Only the number (e.g. 400)
                
            buyPanel.SetActive(true);
        }
    }

    private void BuySelectedLevel()
    {
        if (selectedLevel == null) return;

        if (PlayerData.Instance.SpendCoins(selectedLevel.GetCalculatedCost()))
        {
            PlayerData.Instance.UnlockLevel(selectedLevel.levelIndex);
            PlayPremiumPurchaseSound();
            RefreshAllButtons(); // Refresh visuals immediately after buying
            ClosePanel();
        }
        else
        {
            Debug.Log("Not enough Coins!");
        }
    }

    public void ClosePanel()
    {
        if (buyPanel != null) buyPanel.SetActive(false);
        selectedLevel = null;
    }

    private void PlayPremiumPurchaseSound()
    {
        AudioClip sound = Resources.Load<AudioClip>("Magic Score 9");
        if (sound != null)
        {
            Vector3 playPos = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(sound, playPos, 1.0f);
        }
    }
}