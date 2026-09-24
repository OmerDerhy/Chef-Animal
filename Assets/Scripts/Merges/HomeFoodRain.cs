using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HomeFoodRain : MonoBehaviour
{
    [Header("Falling Object Prefab")]
    [Tooltip("The base object that falls from the sky (Must have HomeFoodItem script attached)")]
    public GameObject fallingFoodPrefab; 

    [Header("Visuals")]
    [Tooltip("List of Sprites to randomly assign to the falling food")]
    public List<Sprite> possibleFoods = new List<Sprite>();

    [Tooltip("Size modifier for the falling food sprites")]
    public float foodScale = 0.5f;

    [Header("Spawn Settings")]
    public float spawnInterval = 0.8f;   
    public float fallSpeedMin = 1.5f;
    public float fallSpeedMax = 3.5f;        
    public float rotateSpeedMax = 100f;     
    
    // Bounds
    private Camera mainCam;
    private float screenTopY;
    private float screenLeftX;
    private float screenRightX;

    void Awake()
    {
        mainCam = Camera.main;
        CalculateScreenBounds();
    }

    void OnEnable()
    {
        // 1. Ensure clean slate
        CancelInvoke(nameof(SpawnFood));

        // 2. Start Spawning
        InvokeRepeating(nameof(SpawnFood), 0f, spawnInterval);
    }

    void OnDisable()
    {
        CancelInvoke(nameof(SpawnFood));
    }

    void CalculateScreenBounds()
    {
        if (mainCam == null) mainCam = Camera.main;

        // 0,0 is bottom left. 1,1 is top right.
        Vector3 bottomLeft = mainCam.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 topRight = mainCam.ViewportToWorldPoint(new Vector3(1, 1, 0));

        screenLeftX = bottomLeft.x + 0.5f; // Add buffer so it doesn't spawn half off-screen
        screenRightX = topRight.x - 0.5f;
        screenTopY = topRight.y + 1.5f;    // Spawn well above the screen so it falls in naturally
    }

    void SpawnFood()
    {
        if (fallingFoodPrefab == null || possibleFoods == null || possibleFoods.Count == 0) 
        {
            Debug.LogWarning("[HomeFoodRain] Missing prefab or empty sprite list. Cannot spawn.");
            return;
        }

        // --- NEW: Update bounds based on current camera position ---
        CalculateScreenBounds();

        // 1. Calculate random X within screen bounds
        float randomX = Random.Range(screenLeftX, screenRightX);
        Vector3 spawnPos = new Vector3(randomX, screenTopY, 0f);

        // 2. Instantiate the falling object
        GameObject foodObj = Instantiate(fallingFoodPrefab, spawnPos, Quaternion.identity, this.transform);
        
        // --- NEW: Apply custom scale and Layer ---
        foodObj.transform.localScale = new Vector3(foodScale, foodScale, 1f);
        foodObj.layer = 4;

        // 3. Assign a random sprite visual to the object
        Sprite randomSprite = possibleFoods[Random.Range(0, possibleFoods.Count)];
        
        SpriteRenderer sr = foodObj.GetComponent<SpriteRenderer>();
        if (sr == null) sr = foodObj.AddComponent<SpriteRenderer>();
        sr.sprite = randomSprite;

        // 4. Randomize sorting order so they overlap dynamically
        sr.sortingOrder = Random.Range(-10, 10);

        // 5. Setup the physics/behavior component
        HomeFoodItem behavior = foodObj.GetComponent<HomeFoodItem>();
        if (behavior == null) behavior = foodObj.AddComponent<HomeFoodItem>();

        // 6. Give it randomized speed and rotation for variety
        float speed = Random.Range(fallSpeedMin, fallSpeedMax);
        float rot = Random.Range(-rotateSpeedMax, rotateSpeedMax);
        behavior.Initialize(speed, rot);
    }
}
