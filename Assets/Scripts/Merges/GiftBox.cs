using UnityEngine;
using UnityEngine.UI; // For Button & Image
using System.Collections;

public class GiftBox : MonoBehaviour
{
    [Header("Configuration")]
    public int clicksToOpen = 5;
    public int coinReward = 200;
    public int gemReward = 20;

    [Header("Visuals")]
    [Tooltip("The sprite to show when the gift is damaged (at 3 clicks)")]
    public Sprite brokenGiftSprite; 
    [Tooltip("The sprite to show when the gift explodes/opens (at 5 clicks)")]
    public Sprite openGiftSprite; 

    public GameObject openEffect; // Explosion particle/effect
    public GameObject coinIconPrefab;
    public GameObject gemIconPrefab;
    
    [Tooltip("The Cookie Icon that flies to the button after opening")]
    public GameObject cookieIconPrefab; // <--- NEW

    [Header("UI Targets")]
    [Tooltip("Drag your Coin Bar UI object here")]
    public Transform coinTarget;
    [Tooltip("Drag your Gem Bar UI object here")]
    public Transform gemTarget;
    
    [Tooltip("Drag the Cookie Time Button here")]
    public Transform cookieTarget; // <--- NEW

    private int currentClicks = 0;
    private bool isShaking = false;
    private bool isExploding = false; // Prevent double clicks during explosion
    private Vector3 initialScale;
    private Sprite originalSprite;
    
    // Components
    private Image uiImage;
    private SpriteRenderer worldSprite;
    private Button myButton;

    void Awake()
    {
        initialScale = transform.localScale;
        
        // Cache visual components for sprite swapping
        uiImage = GetComponent<Image>();
        worldSprite = GetComponent<SpriteRenderer>();

        if (uiImage != null) originalSprite = uiImage.sprite;
        else if (worldSprite != null) originalSprite = worldSprite.sprite;
        
        // If this object has a Button component, add listener
        myButton = GetComponent<Button>();
        if (myButton != null) myButton.onClick.AddListener(HandleClick);
    }

    public void ResetGift()
    {
        currentClicks = 0;
        transform.localScale = initialScale;
        isShaking = false;
        isExploding = false;
        
        // Restore original sprite & Visibility
        if (uiImage != null) 
        {
            uiImage.sprite = originalSprite;
            uiImage.enabled = true;
        }
        else if (worldSprite != null) 
        {
            worldSprite.sprite = originalSprite;
            worldSprite.enabled = true;
        }

        // Re-enable button interaction
        if (myButton != null) myButton.enabled = true;
    }

    public void HandleClick()
    {
        if (isShaking || isExploding) return;

        currentClicks++;

        // 3rd Click: Change to Broken Sprite
        if (currentClicks == 3)
        {
            if (brokenGiftSprite != null)
            {
                if (uiImage != null) uiImage.sprite = brokenGiftSprite;
                else if (worldSprite != null) worldSprite.sprite = brokenGiftSprite;
            }
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

    // Support for 3D/2D object clicks (non-UI)
    void OnMouseDown()
    {
        HandleClick();
    }

    private IEnumerator ShakeRoutine()
    {
        isShaking = true;
        float duration = 0.15f;
        float elapsed = 0f;
        
        Quaternion originalRot = transform.localRotation;
        
        while (elapsed < duration)
        {
            // Fix: Use unscaledDeltaTime to animate even if game is paused
            float dt = Time.unscaledDeltaTime;
            elapsed += dt;

            float z = Mathf.Sin(elapsed * 50f) * 10f; // Shake angle
            transform.localRotation = Quaternion.Euler(0, 0, z);
            
            // Pulse scale
            transform.localScale = initialScale * (1f + Mathf.Sin(elapsed * 20f) * 0.1f);

            yield return null;
        }

        transform.localRotation = originalRot;
        transform.localScale = initialScale;
        isShaking = false;
    }

    private IEnumerator ExplodeSequence()
    {
        isExploding = true;

        // 1. Change Sprite to Open Gift
        if (openGiftSprite != null)
        {
            if (uiImage != null) uiImage.sprite = openGiftSprite;
            else if (worldSprite != null) worldSprite.sprite = openGiftSprite;
        }

        // 2. Give Rewards
        if (PlayerData.Instance != null)
        {
            PlayerData.Instance.AddCoins(coinReward);
            PlayerData.Instance.AddGems(gemReward);
            PlayerData.Instance.UnlockTutorialGift(); // <--- NEW: Flag for Daily Gift Box
        }

        // 3. Play Effect
        if (openEffect != null) 
        {
            Instantiate(openEffect, transform.position, Quaternion.identity);
        }

        // 4. Pop Animation (Scale Up)
        float duration = 0.2f;
        float timer = 0f;
        Vector3 targetScale = initialScale * 1.3f; // Grow 30%

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime; // Fix: Unscaled time
            transform.localScale = Vector3.Lerp(initialScale, targetScale, timer / duration);
            yield return null;
        }

        // 5. Spawn Flying Icons (Coins & Gems)
        Canvas uiCanvas = null;
        if (coinTarget != null) uiCanvas = coinTarget.GetComponentInParent<Canvas>();
        if (uiCanvas == null) uiCanvas = FindObjectOfType<Canvas>();

        if (uiCanvas != null)
        {
            if (coinIconPrefab != null && coinTarget != null)
            {
                SpawnFlyingIcons(coinIconPrefab, coinTarget, 5, uiCanvas.transform);
            }
            if (gemIconPrefab != null && gemTarget != null)
            {
                SpawnFlyingIcons(gemIconPrefab, gemTarget, 5, uiCanvas.transform);
            }
        }

        // 6. Wait 1.3s (Total time since explosion start ~1.5s)
        yield return new WaitForSecondsRealtime(1.3f);

        // 7. Notify TutorialManager (Opens the Cookie Time Tab and Pauses Game)
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnGiftOpened();
        }

        // 8. Spawn Flying Cookie Icon (After tab opens)
        if (cookieIconPrefab != null && cookieTarget != null && uiCanvas != null)
        {
            // Small delay to ensure the tab is visible first
            yield return new WaitForSecondsRealtime(0.5f);
            
            // Spawn 1 Cookie Icon flying to the button
            SpawnFlyingIcons(cookieIconPrefab, cookieTarget, 1, uiCanvas.transform);
        }

        // 9. Hide Visuals (BUT KEEP OBJECT ACTIVE so flying coroutines finish)
        if (uiImage != null) uiImage.enabled = false;
        if (worldSprite != null) worldSprite.enabled = false;
        if (myButton != null) myButton.enabled = false;

        // 10. Wait a bit more for any remaining flying animation frames
        yield return new WaitForSecondsRealtime(1.5f);

        // 11. Now deactivate completely
        gameObject.SetActive(false);
    }

    private void SpawnFlyingIcons(GameObject prefab, Transform target, int count, Transform parent)
    {
        for (int i = 0; i < count; i++)
        {
            StartCoroutine(FlyRoutine(prefab, target, parent, i * 0.1f));
        }
    }

    private IEnumerator FlyRoutine(GameObject prefab, Transform target, Transform parent, float delay)
    {
        yield return new WaitForSecondsRealtime(delay); // Fix: Realtime wait

        // Instantiate
        GameObject icon = Instantiate(prefab, parent);
        
        // --- 1. Fix Layer & Sorting (Crucial for visibility over Tutorial Panel) ---
        icon.layer = parent.gameObject.layer;
        
        // Add Canvas to force it on top of everything (Tutorial Overlay is 29000, Tab is 30000)
        Canvas iconCanvas = icon.GetComponent<Canvas>();
        if (iconCanvas == null) iconCanvas = icon.AddComponent<Canvas>();
        iconCanvas.overrideSorting = true;
        iconCanvas.sortingOrder = 30005; 
        // -------------------------------------------------------------------------

        // --- 2. Setup Transform ---
        icon.transform.localScale = Vector3.one;
        icon.transform.rotation = Quaternion.identity;

        // --- 3. Position Logic (Simplified for robustness) ---
        if (GetComponent<RectTransform>() != null)
        {
            // Gift is UI: Start at Gift's exact position
            icon.transform.position = transform.position;
        }
        else
        {
            // Gift is World Object: Convert World Point to Screen/UI Point
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            
            Canvas c = parent.GetComponentInParent<Canvas>();
            if (c != null && c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                icon.transform.position = screenPos;
            }
            else
            {
                // For Screen Space Camera or World Space UI
                Vector3 worldPoint;
                RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    parent as RectTransform, 
                    screenPos, 
                    c != null ? c.worldCamera : Camera.main, 
                    out worldPoint
                );
                icon.transform.position = worldPoint;
            }
        }

        // Ensure Z is zeroed relative to parent plane
        Vector3 pos = icon.transform.localPosition;
        pos.z = 0f;
        icon.transform.localPosition = pos;

        // --- 4. Animation ---
        float flyDuration = 0.8f;
        float elapsed = 0f;
        Vector3 startPos = icon.transform.position;

        while (elapsed < flyDuration)
        {
            if (target == null) break;
            
            elapsed += Time.unscaledDeltaTime; // Fix: Unscaled time
            float t = elapsed / flyDuration;
            t = t * t * (3f - 2f * t); // Smoothstep ease
            
            // Lerp in World Space to ensure it reaches the target regardless of hierarchy
            icon.transform.position = Vector3.Lerp(startPos, target.position, t);
            
            yield return null;
        }

        Destroy(icon);
    }
}