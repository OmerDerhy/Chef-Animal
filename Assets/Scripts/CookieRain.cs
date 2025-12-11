using UnityEngine;
using System.Collections;

public class CookieRain : MonoBehaviour
{
    [Header("🍪 Prefabs")]
    [Tooltip("The object that falls from the sky (Must have CookieBehavior script)")]
    public GameObject fallingCookieVisual; 
    
    [Tooltip("The actual Food object that appears on the Block")]
    public Food realCookieGamePiece;

    [Header("Spawn Settings")]
    public float spawnInterval = 1.0f;   
    public float fallSpeed = 3f;        
    public float rotateSpeed = 100f;     
    public float rainDuration = 30f; 

    private Camera mainCam;
    private float screenTopY;
    private float screenLeftX;
    private float screenRightX;

     void Awake()
    {
        // Cache camera and calculations once at the beginning
        mainCam = Camera.main;
        CalculateScreenBounds();
    }

    // FIX: Using OnEnable ensures this logic runs EVERY TIME the object becomes active
    void OnEnable()
    {
        // 1. Ensure clean slate
        CancelInvoke(nameof(SpawnCookie));
        StopAllCoroutines();

        // 2. Start Spawning
        InvokeRepeating(nameof(SpawnCookie), 0f, spawnInterval);

        // 3. Start the timer to end the rain
        StartCoroutine(EndRainSequence());
    }

    void OnDisable()
    {
        // Safety cleanup
        CancelInvoke(nameof(SpawnCookie));
        StopAllCoroutines();
    }

    void CalculateScreenBounds()
    {
        if (mainCam == null) mainCam = Camera.main;

        // 0,0 is bottom left. 1,1 is top right.
        Vector3 bottomLeft = mainCam.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 topRight = mainCam.ViewportToWorldPoint(new Vector3(1, 1, 0));

        screenLeftX = bottomLeft.x + 0.5f; // Add buffer so it doesn't spawn half off-screen
        screenRightX = topRight.x - 0.5f;
        screenTopY = topRight.y + 1.0f;    // Spawn slightly above the screen
    }

    void SpawnCookie()
    {
        if (fallingCookieVisual == null || realCookieGamePiece == null) return;

        // 1. Calculate random X within screen bounds
        float randomX = Random.Range(screenLeftX, screenRightX);
        Vector3 spawnPos = new Vector3(randomX, screenTopY, 0f); // Z=0 for 2D

        // 2. Instantiate the falling visual
        GameObject cookieObj = Instantiate(fallingCookieVisual, spawnPos, Quaternion.identity);

        // 3. Setup the behavior
        CookieBehavior behavior = cookieObj.GetComponent<CookieBehavior>();
        if (behavior == null) behavior = cookieObj.AddComponent<CookieBehavior>();

        behavior.Initialize(fallSpeed, rotateSpeed, realCookieGamePiece);
    }

    private IEnumerator EndRainSequence()
    {
        yield return new WaitForSeconds(rainDuration);

        // 1. Find all falling cookies (visuals) and shrink them
        CookieBehavior[] allFallingCookies = FindObjectsOfType<CookieBehavior>();
        foreach (var cookie in allFallingCookies)
        {
            if (cookie != null) cookie.Shrink();
        }

        // 2. Find EVERY Food object in the scene (all foods on blocks)
        Food[] allFoods = FindObjectsOfType<Food>();
        
        foreach (Food food in allFoods)
        {
            if (food != null)
            {
                food.Shrink();
            }
        }

        // 3. Disable this object to stop the rain logic
        gameObject.SetActive(false);
    }
}