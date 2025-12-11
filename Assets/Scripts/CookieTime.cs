using UnityEngine;
using UnityEngine.UI; 
using System.Collections; 

public class CookieTime : MonoBehaviour
{
    // STATIC FLAG: Allows Target.cs to know if we are in cookie mode
    public static bool IsCookieTimeActive = false;

    private BlockManager blocksmanager;
    public Food CookieFood; 
    
    [Header("🍪 Cookie Rain")]
    private CookieRain cookierain;

    [Header("Cooldown Settings")]
    public Button cookieTimeButton; 
    public float cooldownDuration = 120f; 
    private float currentCooldown = 0f;
    private bool isAvailable = true;

    [Header("Other Buttons Control")]
    public Button[] buttonsToDisable; 
    public float tempDisableDuration = 15f;

    void Awake()
    {
        blocksmanager = FindObjectOfType<BlockManager>();
        cookierain = FindObjectOfType<CookieRain>();
        if (cookierain != null)
        {
            cookierain.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        StartCooldown();
    }

    void Update()
    {
        if (!isAvailable)
        {
            currentCooldown -= Time.deltaTime;

            if (currentCooldown <= 0)
            {
                isAvailable = true;
                currentCooldown = 0;
                
                if (cookieTimeButton != null)
                {
                    cookieTimeButton.interactable = true;
                }
            }
        }
    }

    public void SetCookieTime()
    {
        if (!isAvailable) return;

        // 1. Trigger Block Logic (Shrink old food, add cookies)
        if (blocksmanager != null)
        {
            Block[] blocks = blocksmanager.GetBlocks();
            for(int i = 0; i < blocks.Length; i++)
            {
                Block block = blocks[i];
                if (block.HasCurrentFood())
                {
                    Food oldFood = block.GetCurrentFood();
                    if (oldFood != null) oldFood.Shrink();
                    
                    block.ResetCurrentFood();

                    Food newCookie = Instantiate(CookieFood, block.transform.position, Quaternion.identity);
                    newCookie.AssignToBlock(block);
                    block.SetCurrentFood(newCookie);
                    newCookie.Appear();
                }
            }
        }

        // 2. Activate Rain
        if (cookierain != null) cookierain.gameObject.SetActive(true);

        // 3. Disable Buttons
        if (buttonsToDisable != null && buttonsToDisable.Length > 0)
        {
            StartCoroutine(DisableButtonsRoutine());
        }

        // 4. Start Target Logic (Change all faces to want cookies)
        StartCoroutine(CookieTargetSequence());

        // 5. Start Cooldown
        StartCooldown();
    }

    private IEnumerator CookieTargetSequence()
    {
        // A. Turn ON Cookie Mode
        IsCookieTimeActive = true;
        RefreshAllTargets();

        // B. Wait for 30 seconds
        yield return new WaitForSeconds(30f);

        // C. Turn OFF Cookie Mode
        IsCookieTimeActive = false;
        RefreshAllTargets();
    }

    // Helper to find all faces and tell them to update their list
    private void RefreshAllTargets()
    {
        Target[] allTargets = FindObjectsOfType<Target>();
        foreach (Target t in allTargets)
        {
            t.RefreshTargetFoods();
        }
    }

    private void StartCooldown()
    {
        isAvailable = false;
        currentCooldown = cooldownDuration;
        if (cookieTimeButton != null) cookieTimeButton.interactable = false;
    }

    private IEnumerator DisableButtonsRoutine()
    {
        foreach (Button btn in buttonsToDisable) if (btn != null) btn.interactable = false;
        yield return new WaitForSeconds(tempDisableDuration);
        foreach (Button btn in buttonsToDisable) if (btn != null) btn.interactable = true;
    }
}