using UnityEngine;

public class FrameRateManager : MonoBehaviour
{
    void Awake()
    {
        // 1. Disable VSync (required to set manual frame rate)
        QualitySettings.vSyncCount = 0;

        // 2. Set the target frame rate to 60
        Application.targetFrameRate = 60;
    }
}