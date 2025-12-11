using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections; // Required for Coroutines

public class CookieBehavior : MonoBehaviour
{
    private float fallSpeed;
    private float rotateSpeed;
    private Food realCookiePrefab;
    
    private BlockManager blockManager;
    private Camera mainCam;
    private float bottomLimit;
    private bool isShrinking = false; // Flag to prevent interactions/movement

    public void Initialize(float speed, float rot, Food realPrefab)
    {
        fallSpeed = speed;
        rotateSpeed = rot;
        realCookiePrefab = realPrefab;
    }

    void Start()
    {
        blockManager = Object.FindFirstObjectByType<BlockManager>();
        mainCam = Camera.main;
        
        // Calculate bottom limit to destroy object if missed
        bottomLimit = mainCam.ViewportToWorldPoint(new Vector3(0, -0.2f, 0)).y;
        
        // Ensure we have a collider for clicking
        if (GetComponent<Collider2D>() == null)
        {
            gameObject.AddComponent<CircleCollider2D>();
        }
    }

    void Update()
    {
        if (isShrinking) return; // Stop logic if shrinking

        // 1. Fall Down
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime, Space.World);

        // 2. Rotate
        transform.Rotate(Vector3.forward, rotateSpeed * Time.deltaTime);

        // 3. Check Bounds
        if (transform.position.y < bottomLimit)
        {
            Destroy(gameObject);
        }

        // 4. Handle Input (Clicking the falling cookie)
        CheckInput();
    }

    private void CheckInput()
    {
        bool clicked = false;

        // Check Mouse
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            if (GetComponent<Collider2D>().OverlapPoint(mousePos)) clicked = true;
        }

        // Check Touch
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    Vector2 touchPos = mainCam.ScreenToWorldPoint(touch.position.ReadValue());
                    if (GetComponent<Collider2D>().OverlapPoint(touchPos)) clicked = true;
                }
            }
        }

        if (clicked)
        {
            AddToFreeBlock();
        }
    }

    private void AddToFreeBlock()
    {
        if (blockManager == null) return;

        // 1. Find an empty block
        int blockIndex = blockManager.FillBlock();

        if (blockIndex != -1)
        {
            Block targetBlock = blockManager.GetBlocks()[blockIndex];

            // 2. Reserve it immediately so no one else takes it
            targetBlock.ReserveBlock();

            // 3. Instantiate the REAL game piece at the block's position
            Food newCookie = Instantiate(realCookiePrefab, targetBlock.transform.position, Quaternion.identity);

            // 4. Setup the new cookie
            newCookie.AssignToBlock(targetBlock);
            targetBlock.SetCurrentFood(newCookie);
            
            // Trigger the "Appear" animation on the new cookie
            newCookie.Appear(); 

            // 5. Destroy this falling cookie
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("No free blocks for cookie!");
            // Optional: Shrink instead of instant destroy if clicked but full
            Shrink(); 
        }
    }

    // --- NEW: Shrink Logic ---
    public void Shrink()
    {
        if (isShrinking) return;
        StartCoroutine(ShrinkRoutine());
    }

    private IEnumerator ShrinkRoutine()
    {
        isShrinking = true;
        // Disable collider so it can't be clicked while disappearing
        if (GetComponent<Collider2D>()) GetComponent<Collider2D>().enabled = false;

        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;

        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}