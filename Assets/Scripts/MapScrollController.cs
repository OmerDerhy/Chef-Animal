using UnityEngine;
using UnityEngine.UI;

public class MapScrollController : MonoBehaviour
{
    public ScrollRect scrollRect;
    public RectTransform contentPanel;
    
    // Example: Call this with the level index the user is currently on
    public void FocusOnLevel(int currentLevelIndex, int totalLevels)
    {
        // Calculate a value between 0 and 1 based on level progress
        float progress = (float)currentLevelIndex / (float)totalLevels;
        
        // Clamp ensures we don't go out of bounds
        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(progress);
    }
}