using UnityEngine;
using UnityEngine.UI; 
using System.Collections; 
using System.Collections.Generic;

public class CookieTime : MonoBehaviour
{
    // STATIC FLAG: Allows Target.cs to know if we are in cookie mode
    public static bool IsCookieTimeActive = false;

    private BlockManager blocksmanager;
    public Food CookieFood; 
    private TutorialManager tutorialManager;
    
    [Header("🍪 Cookie Rain")]
    private CookieRain cookierain;

    [Header("Visual Feedback")]
    [Tooltip("The logo that flashes for 1.5 seconds when Cookie Time starts")]
    public GameObject cookieTimeLogo;
    [Tooltip("The size the logo grows to (1 = normal, 2 = double size, etc.)")]
    public float logoTargetScale = 2.5f;

    [Header("Cooldown Settings")]
    public Button cookieTimeButton; 
    
    [Tooltip("Drag the Image component of the button here. Ensure Image Type is set to 'Filled' in Unity Inspector.")]
    public Image cooldownFillImage; 

    [Tooltip("Icon that appears when the button is ready to press (e.g. Finger pointing)")]
    public GameObject readyFingerIcon;

    [Header("Finger Animation")]
    private float animationSpeed = 5f;
    private float scaleAmount = 0.1f;
    private Vector3 initialFingerScale;

    private float cooldownDuration = 120f; 
    private float currentCooldown = 0f;
    private bool isAvailable = true;

    [Header("Other Buttons Control")]
    private float tempDisableDuration = 80f;

    void Awake()
    {
        blocksmanager = FindObjectOfType<BlockManager>();
        cookierain = FindObjectOfType<CookieRain>();
        tutorialManager = FindObjectOfType<TutorialManager>();
        if (cookierain != null)
        {
            cookierain.gameObject.SetActive(false);
        }

        if (readyFingerIcon != null)
        {
            initialFingerScale = readyFingerIcon.transform.localScale;
            readyFingerIcon.SetActive(false); 
        }

        if (cookieTimeLogo != null) cookieTimeLogo.SetActive(false);
    }

    void Start()
    {
        // --- UPDATED LOGIC ---
        // Always start with cooldown (temp disable duration), 
        // regardless of whether we unlocked it before or not.
        StartCooldown();

        // Double check: if completely locked (gift not opened yet), ensure button is non-interactable
        if (PlayerPrefs.GetInt("Tutorial_GiftOpened_Completed", 0) == 0)
        {
            if (cookieTimeButton != null) cookieTimeButton.interactable = false;
        }
    }

    void Update()
    {
        if (!isAvailable)
        {
            currentCooldown -= Time.deltaTime;

            if (cooldownFillImage != null)
            {
                cooldownFillImage.fillAmount = 1f - (currentCooldown / cooldownDuration);
            }

            if (currentCooldown <= 0)
            {
                MakeAvailable();
            }
        }
        else
        {
            // Only animate if the finger is actually active/visible
            if (readyFingerIcon != null && readyFingerIcon.activeSelf)
            {
                AnimateFinger();
                bool FirstTimeUnlocked = PlayerData.Instance.IsCookieTimeUnlocked();
                if(!FirstTimeUnlocked)
                {
                    PlayerData.Instance.UnlockCookieTime();
                    if (tutorialManager != null)
                    {
                        tutorialManager.TriggerCookieTimeUnlocked();
                    }
                }
            }
        }
    }

    // --- NEW: Called by TutorialManager when Gift Opens ---
    public void ForceReady()
    {
        MakeAvailable();
    }

    private void MakeAvailable()
    {
        isAvailable = true;
        currentCooldown = 0f;
        
        if (cookieTimeButton != null)
        {
            cookieTimeButton.interactable = true;
            Debug.Log("[CookieTime] Cookie Time is now available!");
        }

        if (cooldownFillImage != null)
        {
            cooldownFillImage.fillAmount = 1f;
        }

        if (readyFingerIcon != null) readyFingerIcon.SetActive(true);

        // Play sound when available
        AudioClip readySound = Resources.Load<AudioClip>("Magic Score 9");
        if (readySound != null)
        {
            Vector3 soundPos = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(readySound, soundPos, 1.0f);
        }
    }

    private void AnimateFinger()
    {
        if (readyFingerIcon != null)
        {
            float scaleFactor = 1f + Mathf.Sin(Time.time * animationSpeed) * scaleAmount;
            readyFingerIcon.transform.localScale = initialFingerScale * scaleFactor;
        }
    }

    public void SetCookieTime()
    {
        if (!isAvailable) return;

        // --- 1. SHOW LOGO ---
        if (cookieTimeLogo != null)
        {
            StartCoroutine(ShowLogoRoutine());
        }

        // --- 2. DISABLE ALL BUTTONS ---
        StartCoroutine(DisableAllButtonsRoutine());

        // --- 3. ACTIVATE RAIN ---
        if (cookierain != null) 
        {
            cookierain.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[CookieTime] CookieRain script not found in scene!");
        }

        // --- 4. START TARGET LOGIC & MUSIC ---
        if (BackgroundMusic.Instance != null) 
        {
            BackgroundMusic.Instance.PlayCookieTimeMusic();
        }
        StartCoroutine(CookieTargetSequence());

        // --- 5. START COOLDOWN ---
        StartCooldown();

        // --- 6. TRIGGER BLOCK LOGIC ---
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
                }

                if (CookieFood != null)
                {
                    Food newCookie = Instantiate(CookieFood, block.transform.position, Quaternion.identity);
                    newCookie.AssignToBlock(block);
                    block.SetCurrentFood(newCookie);
                    newCookie.Appear();
                }
            }
        }
        else
        {
            Debug.LogError("[CookieTime] Critical: BlockManager not found in this level! Cookies won't spawn on blocks.");
        }
    }

    private IEnumerator ShowLogoRoutine()
    {
        if (cookieTimeLogo == null) yield break;

        cookieTimeLogo.SetActive(true);
        
        CanvasGroup cg = cookieTimeLogo.GetComponent<CanvasGroup>();
        if (cg == null) cg = cookieTimeLogo.AddComponent<CanvasGroup>();

        cookieTimeLogo.transform.localScale = Vector3.zero;
        cg.alpha = 1f;

        float growDuration = 0.3f;
        float timer = 0f;

        while (timer < growDuration)
        {
            timer += Time.deltaTime;
            float t = timer / growDuration;
            float scale = Mathf.SmoothStep(0f, 1f, t);
            cookieTimeLogo.transform.localScale = Vector3.one * scale * logoTargetScale;
            yield return null;
        }
        cookieTimeLogo.transform.localScale = Vector3.one * logoTargetScale;

        yield return new WaitForSeconds(0.7f);

        float fadeDuration = 0.5f;
        timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / fadeDuration;
            cg.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        cookieTimeLogo.SetActive(false);
        cg.alpha = 1f; 
    }

    private IEnumerator CookieTargetSequence()
    {
        IsCookieTimeActive = true;
        RefreshAllTargets();

        yield return new WaitForSeconds(tempDisableDuration);

        IsCookieTimeActive = false;
        RefreshAllTargets();
        
        if (BackgroundMusic.Instance != null) 
        {
            BackgroundMusic.Instance.PlayDefaultMusic();
        }
    }

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
        
        if (cooldownFillImage != null) cooldownFillImage.fillAmount = 0f;

        if (readyFingerIcon != null) readyFingerIcon.SetActive(false);
    }

    private IEnumerator DisableAllButtonsRoutine()
    {
        Button[] allButtons = FindObjectsOfType<Button>();
        List<Button> buttonsIDisabled = new List<Button>();

        // A. Turn OFF
        foreach (Button btn in allButtons) 
        {
            if (btn != null && btn != cookieTimeButton && btn.interactable) 
            {
                btn.interactable = false;
                buttonsIDisabled.Add(btn);
            }
        }

        // B. Wait
        yield return new WaitForSeconds(tempDisableDuration);

        // C. Turn ON
        foreach (Button btn in buttonsIDisabled) 
        {
            if (btn != null) 
            {
                btn.interactable = true;
            }
        }
    }
}