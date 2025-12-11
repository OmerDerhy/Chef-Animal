using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Required to reload the scene

public class RemoveDataButton : MonoBehaviour
{
    void Start()
    {
        // Automatically look for a Button component on this object
        Button btn = GetComponent<Button>();
        
        if (btn != null)
        {
            btn.onClick.AddListener(OnResetClicked);
        }
        else
        {
            Debug.LogWarning("RemoveDataButton script is attached to an object without a Button component!", this);
        }
    }

    public void OnResetClicked()
    {
        if (PlayerData.Instance != null)
        {
            // 1. Call the reset function in PlayerData
            PlayerData.Instance.ResetData();
            
            Debug.Log("Save Data Deleted. Reloading Scene...");

            // 2. Reload the current scene so all FoodSources lock themselves again and coins reset visually
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else
        {
            Debug.LogError("PlayerData not found! Make sure you have a DataManager object in the scene.");
        }
    }
}