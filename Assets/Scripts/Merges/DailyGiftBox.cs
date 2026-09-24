using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

public class DailyGiftBox : MonoBehaviour
{
    [Header("Gift Settings")]
    public int clicksToOpen = 5;
    public float cooldownHours = 12f;

    [Header("Visuals")]
    public Sprite brokenGiftSprite; 
    public Sprite openGiftSprite; 
    public GameObject openEffect; 

    [Header("Audio")]
    [Tooltip("Sound to play when the gift pops open")]
    public AudioClip openGiftSound;

    [Tooltip("Sprite shown in the popup when Coins are won")]
    public Sprite coinRewardSprite;
    [Tooltip("Sprite shown in the popup when Gems are won")]
    public Sprite gemRewardSprite;
    [Tooltip("Sprite shown in the popup when the Jackpot is won")]
    public Sprite jackpotRewardSprite;

    [Header("Popup UI")]
    [Tooltip("The Panel that appears to show what you won")]
    public GameObject rewardPopupPanel;
    [Tooltip("Text element to display the reward description")]
    public TextMeshProUGUI rewardDescText;
    [Tooltip("Image element to display the reward icon")]
    public Image rewardIconImage;
    [Tooltip("Button to close the popup")]
    public Button closePopupButton;

    [Header("Focus State (First Click)")]
    [Tooltip("Drag the existing Dimmer GameObject here")]
    public GameObject dimmerObject;
    [Tooltip("Drag your pre-placed big center gift here!")]
    public GameObject prePlacedCenterGift;
    [Tooltip("Place an empty GameObject in the scene where you want the gift to move to and assign it here")]
    public Transform animationTargetSpot;

    [Header("UI State Overlays (Optional)")]
    [Tooltip("Text on top of the gift showing the cooldown timer")]
    public TextMeshProUGUI timerText;

    private int currentClicks = 0;
    private bool isShaking = false;
    private bool isExploding = false;
    private bool isFocused = false;
    private Vector3 initialScale;
    private Vector3 focusedBaseScale;
    private Vector3 originalPosition;
    private int originalSiblingIndex;
    private Transform originalParent; // Ensure parent is restored
    private Sprite originalSprite;

    private Image uiImage;
    private Button myButton;
    private RectTransform rectTransform;
    private Animator myAnimator; 
    
    // External components on the big gift
    private Image bigGiftImage;
    private RectTransform bigGiftRect;

    private bool hasInitialized = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        myAnimator = GetComponent<Animator>();
        initialScale = rectTransform.localScale;
        
        uiImage = GetComponent<Image>();
        myButton = GetComponent<Button>();

        if (uiImage != null) originalSprite = uiImage.sprite;
        if (myButton != null) myButton.onClick.AddListener(HandleClick);
        if (closePopupButton != null) closePopupButton.onClick.AddListener(ClosePopup);

        if (rewardPopupPanel != null) rewardPopupPanel.SetActive(false);
        if (dimmerObject != null) dimmerObject.SetActive(false);

        if (prePlacedCenterGift != null)
        {
            prePlacedCenterGift.SetActive(false);
            bigGiftImage = prePlacedCenterGift.GetComponent<Image>();
            bigGiftRect = prePlacedCenterGift.GetComponent<RectTransform>();
            
            // Forward clicks from the big gift directly into this script's HandleClick
            Button bigBtn = prePlacedCenterGift.GetComponent<Button>();
            if (bigBtn != null) bigBtn.onClick.AddListener(HandleClick);
        }
    }

    void Start()
    {
        hasInitialized = false;
    }

    void Update()
    {
        // Try to initialize if PlayerData wasn't ready on frame 1
        if (!hasInitialized)
        {
            if (PlayerData.Instance != null)
            {
                CheckCooldownState();
                hasInitialized = true;
            }
            return; // Wait until initialized
        }

        // Update the timer if still on cooldown
        if (myButton != null && !myButton.interactable && timerText != null)
        {
            TimeSpan timePassed = PlayerData.Instance.GetTimeSinceLastDailyGift();
            TimeSpan cooldown = TimeSpan.FromHours(cooldownHours);
            
            if (timePassed < cooldown)
            {
                TimeSpan remaining = cooldown - timePassed;
                timerText.text = string.Format("{0:D2}:{1:D2}:{2:D2}", remaining.Hours, remaining.Minutes, remaining.Seconds);
            }
            else
            {
                CheckCooldownState();
            }
        }
    }

    private void CheckCooldownState()
    {
        if (PlayerData.Instance == null) return;

        // --- NEW: Hide until tutorial is complete ---
        if (!PlayerData.Instance.IsTutorialGiftOpened())
        {
            SetCooldownVisuals();
            if (timerText != null) timerText.text = "PLAY LEVEL 1!";
            return;
        }

        TimeSpan timePassed = PlayerData.Instance.GetTimeSinceLastDailyGift();

        if (timePassed.TotalHours >= cooldownHours)
        {
            // Ready to open!
            ResetGiftState();
        }
        else
        {
            // Still on cooldown
            SetCooldownVisuals();
        }
    }

    private void ResetGiftState()
    {
        currentClicks = 0;
        rectTransform.localScale = initialScale;
        isShaking = false;
        isExploding = false;
        
        if (uiImage != null) 
        {
            uiImage.sprite = originalSprite;
            uiImage.color = Color.white;
            uiImage.enabled = true; // Show small gift again
        }

        if (prePlacedCenterGift != null)
        {
            prePlacedCenterGift.SetActive(false);
            if (bigGiftImage != null) bigGiftImage.sprite = originalSprite;
        }

        if (myButton != null) myButton.interactable = true;
        if (timerText != null) timerText.text = "READY!";
    }

    private void ResetFocusState()
    {
        isFocused = false;
        
        if (dimmerObject != null) dimmerObject.SetActive(false);
        
        // Hide big gift, show small gift
        if (prePlacedCenterGift != null) prePlacedCenterGift.SetActive(false);
        if (uiImage != null) uiImage.enabled = true;

        // Restore original parent, position, and scale
        if (originalParent != null)
        {
            transform.SetParent(originalParent, true);
            transform.SetSiblingIndex(originalSiblingIndex);
        }
        rectTransform.anchoredPosition = originalPosition;
        rectTransform.localScale = initialScale;
    }

    private void SetCooldownVisuals()
    {
        if (myButton != null) myButton.interactable = false;
        if (uiImage != null)
        {
            // Make it look empty or unavailable (grayed out and open)
            if (openGiftSprite != null) uiImage.sprite = openGiftSprite;
            uiImage.color = new Color(0.7f, 0.7f, 0.7f, 1f); 
        }
    }

    public void HandleClick()
    {
        // Don't do anything if they haven't finished the tutorial
        if (PlayerData.Instance != null && !PlayerData.Instance.IsTutorialGiftOpened()) return;

        if (isShaking || isExploding) return;

        // --- FIRST CLICK FOCUS LOGIC ---
        if (!isFocused)
        {
            isFocused = true;
            
            // Activate Dimmer
            if (dimmerObject != null) 
            {
                dimmerObject.SetActive(true);
                dimmerObject.transform.SetAsLastSibling();
            }
            
            originalPosition = rectTransform.anchoredPosition;
            originalSiblingIndex = transform.GetSiblingIndex();
            originalParent = transform.parent;
            
            if (dimmerObject != null && dimmerObject.transform.parent != null)
            {
                transform.SetParent(dimmerObject.transform.parent, true);
            }
            transform.SetAsLastSibling();

            StartCoroutine(FocusAnimationRoutine());

            return; // Don't count this as one of the 5 "opening" clicks
        }

        currentClicks++;

        // 3rd Click: Change to Broken Sprite
        if (currentClicks == 3 && brokenGiftSprite != null)
        {
            if (bigGiftImage != null && prePlacedCenterGift != null && prePlacedCenterGift.activeInHierarchy) bigGiftImage.sprite = brokenGiftSprite;
            else if (uiImage != null) uiImage.sprite = brokenGiftSprite; // Fallback
        }

        // 5th Click: Explode Sequence
        if (currentClicks >= clicksToOpen)
        {
            StartCoroutine(ExplodeSequence());
        }
        else
        {
            StartCoroutine(ShakeRoutine());
        }
    }

    private IEnumerator FocusAnimationRoutine()
    {
        if (myButton != null) myButton.interactable = false;

        float duration = 0.3f;
        float elapsed = 0f;
        
        Vector3 startWorldPos = rectTransform.position;
        // Target the assigned spot, otherwise fallback to the dimmer center
        Vector3 targetWorldPos = animationTargetSpot != null ? animationTargetSpot.position : 
                                (dimmerObject != null ? dimmerObject.transform.position : startWorldPos);
        targetWorldPos.z = startWorldPos.z;
        
        // Use the scale after reparenting!
        Vector3 startScale = rectTransform.localScale;
        focusedBaseScale = startScale * 2.5f; // Enlarge
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            t = Mathf.Sin(t * Mathf.PI * 0.5f); // ease out

            rectTransform.position = Vector3.Lerp(startWorldPos, targetWorldPos, t);
            rectTransform.localScale = Vector3.Lerp(startScale, focusedBaseScale, t);
            yield return null;
        }

        rectTransform.position = targetWorldPos;
        rectTransform.localScale = focusedBaseScale;

        if (myButton != null) myButton.interactable = true;
    }
    private IEnumerator ShakeRoutine()
    {
        isShaking = true;
        float duration = 0.15f;
        float elapsed = 0f;
        
        RectTransform targetRect = (bigGiftRect != null && prePlacedCenterGift != null && prePlacedCenterGift.activeInHierarchy) ? bigGiftRect : rectTransform;
        Quaternion originalRot = targetRect.localRotation;
        Vector3 baseScale = isFocused ? focusedBaseScale : initialScale;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float z = Mathf.Sin(elapsed * 50f) * 10f; 
            targetRect.localRotation = Quaternion.Euler(0, 0, z);
            targetRect.localScale = baseScale * (1f + Mathf.Sin(elapsed * 20f) * 0.1f);
            yield return null;
        }

        targetRect.localRotation = originalRot;
        targetRect.localScale = baseScale;
        isShaking = false;
    }

    private IEnumerator ExplodeSequence()
    {
        isExploding = true;
        if (myButton != null) myButton.interactable = false; // Lock out further clicks

        // Save that we opened it!
        PlayerData.Instance.ClaimDailyGift();

        // --- NEW: Map the Daily Gift Notification ---
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ScheduleDailyGiftNotification(TimeSpan.FromHours(cooldownHours));
        }

        // 1. Change Sprite to Open
        if (openGiftSprite != null)
        {
            if (bigGiftImage != null && prePlacedCenterGift != null && prePlacedCenterGift.activeInHierarchy) bigGiftImage.sprite = openGiftSprite;
            else if (uiImage != null) uiImage.sprite = openGiftSprite; // Fallback
        }

        // 2. Play Effect at target position
        Transform targetTransform = (bigGiftRect != null && prePlacedCenterGift != null && prePlacedCenterGift.activeInHierarchy) ? bigGiftRect : transform;
        if (openEffect != null) 
        {
            Instantiate(openEffect, targetTransform.position, Quaternion.identity, targetTransform.parent);
        }

        // Audio pop
        AudioClip soundToPlay = openGiftSound != null ? openGiftSound : Resources.Load<AudioClip>("Harp Money 1");
        if (soundToPlay != null) AudioSource.PlayClipAtPoint(soundToPlay, Camera.main != null ? Camera.main.transform.position : targetTransform.position, 1.0f);

        // 3. Pop Animation
        float duration = 0.2f;
        float timer = 0f;
        Vector3 baseScale = isFocused ? focusedBaseScale : initialScale;
        Vector3 targetScale = baseScale * 1.3f; 

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime; 
            if (bigGiftRect != null && prePlacedCenterGift != null && prePlacedCenterGift.activeInHierarchy) bigGiftRect.localScale = Vector3.Lerp(baseScale, targetScale, timer / duration);
            else rectTransform.localScale = Vector3.Lerp(baseScale, targetScale, timer / duration);
            yield return null;
        }

        // Randomize Rewards!
        var rewardInfo = DetermineAndGiveRewards();

        // Wait a small moment before showing popup
        yield return new WaitForSecondsRealtime(0.5f);

        // 4. Show Reward Popup
        if (rewardDescText != null)
        {
            rewardDescText.text = rewardInfo.text;
        }

        if (rewardIconImage != null && rewardInfo.sprite != null)
        {
            rewardIconImage.sprite = rewardInfo.sprite;
            // Optionally set it to perfectly fit native size
            rewardIconImage.SetNativeSize();
            // Scale if native size is too big/small depending on your UI layout:
            // rewardIconImage.transform.localScale = Vector3.one * 0.5f; 
        }

        if (rewardPopupPanel != null) 
        {
            rewardPopupPanel.SetActive(true);
            rewardPopupPanel.transform.SetAsLastSibling();
        }

        // Return the gift back to its corner quietly behind the popup
        ResetFocusState();
        SetCooldownVisuals();
        
        isExploding = false;
        
        // Timer will naturally start ticking down via Update()
    }

    // Returns the descriptive text and the appropriate icon sprite
    private (string text, Sprite sprite) DetermineAndGiveRewards()
    {
        int roll = UnityEngine.Random.Range(1, 101); // 1 to 100

        if (roll <= 25)
        {
            // 25% for 25 Coins
            PlayerData.Instance.AddCoins(25);
            return ("You found 25 Coins!", coinRewardSprite);
        }
        else if (roll <= 50)
        {
            // 25% for 50 Coins
            PlayerData.Instance.AddCoins(50);
            return ("You found 50 Coins!", coinRewardSprite);
        }
        else if (roll <= 70)
        {
            // 20% for 10 Gems
            PlayerData.Instance.AddGems(10);
            return ("You found 10 Gems!", gemRewardSprite);
        }
        else if (roll <= 90)
        {
            // 20% for 20 Gems
            PlayerData.Instance.AddGems(20);
            return ("You found 20 Gems!", gemRewardSprite);
        }
        else
        {
            // 10% Special Reward! (Defaulting to 50 Gems and 500 Coins)
            PlayerData.Instance.AddGems(50);
            PlayerData.Instance.AddCoins(500);
            return ("JACKPOT!\nYou found 50 Gems & 500 Coins!", jackpotRewardSprite);
        }
    }

    public void ClosePopup()
    {
        if (rewardPopupPanel != null) rewardPopupPanel.SetActive(false);
    }
}
