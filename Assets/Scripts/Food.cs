using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Food : MonoBehaviour
{
    private Block block;
    
    private bool isDragging = false;
    private int draggingFingerId = -1;
    private bool isAnimating = false; // Important flag

    private Camera mainCamera;
    public AudioClip clickSound;
    private AudioSource audioSource;

    private Rigidbody2D rb2d;
    private Collider2D col2d;

    private Animator animator;

    // --- INTERACTION VARIABLES ---
    private Trash currentHoveredTrash;
    private Face currentHoveredFace;

    [Header("Movement Settings")]
    public float returnSpeed = 15f; 
    public float snapDistance = 0.05f;
    
    private Vector3 targetPosition; 

    void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
        col2d = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        mainCamera = Camera.main;

        if (rb2d) {
            rb2d.bodyType = RigidbodyType2D.Kinematic;
            rb2d.freezeRotation = true; 
        }

        audioSource = gameObject.AddComponent<AudioSource>();
        clickSound = Resources.Load<AudioClip>("FoodClick");
        audioSource.playOnAwake = false;
        
        targetPosition = transform.position;

        if (animator != null) animator.Play("FoodPop"); 

        // FIX: Only run auto-logic if we aren't already animating
        // (This allows CookieTime to call Appear() manually without Start() interfering)
        if (!isAnimating) 
        {
            FindInitialBlock();
            
            // Default behavior: Grow when spawned naturally
            StartCoroutine(GrowAndAppear());
        }
    }
    void FindInitialBlock()
    {
        Collider2D hit = Physics2D.OverlapPoint(transform.position);
        if (hit != null)
        {
            Block foundBlock = hit.GetComponent<Block>();
            if (foundBlock != null) AssignToBlock(foundBlock);
        }
    }

    void Update()
    {
        if (isAnimating) return;

        HandleTouchInput();
        HandleMouseInput();

        if (isDragging)
        {
            // Follow Finger
            Vector3 mouseWorldPos = GetInputWorldPosition();
            transform.position = new Vector3(mouseWorldPos.x, mouseWorldPos.y, -2f); 
            
            // --- CHECK FOR INTERACTIONS (TRASH & FACE) ---
            CheckHoverInteractions();
        }
        else
        {
            // Smoothly fly to target
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * returnSpeed);
            if (Vector3.Distance(transform.position, targetPosition) < snapDistance)
            {
                transform.position = targetPosition;
            }
        }
    }

    private void CheckHoverInteractions()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.2f); 

        Trash trashFound = null;
        Face faceFound = null;

        foreach (var hit in hits)
        {
            if (trashFound == null)
            {
                Trash t = hit.GetComponent<Trash>();
                if (t == null) t = hit.GetComponentInParent<Trash>();
                if (t != null) trashFound = t;
            }

            if (faceFound == null)
            {
                Face f = hit.GetComponent<Face>();
                if (f == null) f = hit.GetComponentInParent<Face>();
                if (f != null) faceFound = f;
            }
        }

        // --- TRASH LOGIC ---
        if (trashFound != null)
        {
            if (currentHoveredTrash != trashFound)
            {
                if (currentHoveredTrash != null) currentHoveredTrash.SetOpenState(false);
                trashFound.SetOpenState(true);
                currentHoveredTrash = trashFound;
            }
        }
        else
        {
            if (currentHoveredTrash != null)
            {
                currentHoveredTrash.SetOpenState(false);
                currentHoveredTrash = null;
            }
        }

        // --- FACE LOGIC ---
        if (faceFound != null)
        {
            if (currentHoveredFace != faceFound)
            {
                if (currentHoveredFace != null) currentHoveredFace.CloseMouth();
                faceFound.OpenMouth();
                currentHoveredFace = faceFound;
            }
        }
        else
        {
            if (currentHoveredFace != null)
            {
                currentHoveredFace.CloseMouth();
                currentHoveredFace = null;
            }
        }
    }

    public void AssignToBlock(Block newBlock)
    {
        block = newBlock;
        targetPosition = block.transform.position;
        targetPosition.z = 0f; 
    }

    #region Input Handling

    private Vector3 GetInputWorldPosition()
    {
        Vector2 screenPos = Vector2.zero;
        if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
            screenPos = Touchscreen.current.touches[0].position.ReadValue();
        else if (Mouse.current != null)
            screenPos = Mouse.current.position.ReadValue();
            
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
        worldPos.z = 0;
        return worldPos;
    }

    private void HandleTouchInput()
    {
        if (Touchscreen.current == null) return;
        int layerMask = ~LayerMask.GetMask("Ignore Raycast"); 

        foreach (var touch in Touchscreen.current.touches)
        {
            if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
            {
                Vector2 touchPos = touch.position.ReadValue();
                Ray ray = mainCamera.ScreenPointToRay(touchPos);
                
                RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(ray, Mathf.Infinity, layerMask);

                foreach (var hit in hits)
                {
                    if (hit.collider.gameObject == gameObject)
                    {
                        StartDragging(touch.touchId.ReadValue());
                        break; 
                    }
                }
            }
            
            if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Ended || 
                touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                if (touch.touchId.ReadValue() == draggingFingerId)
                    StopDragging();
            }
        }
    }

    private void HandleMouseInput()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(mousePos);
            RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(ray);

            foreach (var hit in hits)
            {
                if (hit.collider.gameObject == gameObject)
                {
                    StartDragging(-1);
                    break; 
                }
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && isDragging)
        {
            StopDragging();
        }
    }

    private void StartDragging(int fingerId)
    {
        isDragging = true;
        draggingFingerId = fingerId;
        PlayClickSound();
    }

    private void StopDragging()
    {
        isDragging = false;
        draggingFingerId = -1;
        
        if (currentHoveredTrash != null)
        {
            currentHoveredTrash.SetOpenState(false);
            currentHoveredTrash = null;
        }
        if (currentHoveredFace != null)
        {
            currentHoveredFace.CloseMouth();
            currentHoveredFace = null;
        }

        DetectAndPlaceOnBlock();
    }
    #endregion

    private void DetectAndPlaceOnBlock()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.1f);
        
        // 1. CHECK FOR TRASH
        foreach (var hit in hits)
        {
            Trash trash = hit.GetComponent<Trash>();
            if (trash == null) trash = hit.GetComponentInParent<Trash>();
            
            if (trash != null)
            {
                trash.PlayDeleteEffect();
                if (this.block != null) this.block.ResetCurrentFood();
                Destroy(gameObject);
                return;
            }
        }

        // 2. CHECK FOR FACE (FEEDING)
        foreach (var hit in hits)
        {
            Face face = hit.GetComponent<Face>();
            if (face == null) face = hit.GetComponentInParent<Face>();

            if (face != null)
            {
                // Trigger the face's eating logic
                face.Taste(gameObject.tag);
                
                if (this.block != null) this.block.ResetCurrentFood();
                
                // NEW: Use Shrink animation instead of Destroy(gameObject)
                StartCoroutine(ShrinkAndDestroy());
                return;
            }
        }

        // 3. CHECK FOR BLOCKS
        Block targetBlock = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            Block b = hit.GetComponent<Block>();
            if (b != null)
            {
                float dist = Vector2.Distance(transform.position, b.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    targetBlock = b;
                }
            }
        }

        // 4. DECIDE BLOCK ACTION
        if (targetBlock != null)
        {
            Food existingFood = targetBlock.GetCurrentFood();
            Block previousBlock = this.block; 

            if (existingFood == null)
            {
                if (previousBlock != null) previousBlock.ResetCurrentFood();
                AssignToBlock(targetBlock);
                targetBlock.SetCurrentFood(this);
                targetBlock.ActivateImage();
            }
            else
            {
                // MERGE
                if (existingFood.CompareTag(this.tag))
                {
                    if (previousBlock != null) previousBlock.ResetCurrentFood();
                    targetBlock.SetCurrentFood(this); 
                }
                // SWAP
                else
                {
                    if (previousBlock != null)
                    {
                        existingFood.AssignToBlock(previousBlock);
                        previousBlock.SetCurrentFood(existingFood);
                    }
                    AssignToBlock(targetBlock);
                    targetBlock.SetCurrentFood(this);
                    targetBlock.ActivateImage();
                }
            }
        }
        else
        {
            Debug.Log("No valid block found, returning.");
        }

        targetPosition.z = 0;
    }

    public void AnimateMoveToBlock(Block targetBlock)
    {
        StartCoroutine(MoveRoutine(targetBlock));
    }

    private IEnumerator MoveRoutine(Block targetBlock)
    {
        isAnimating = true;
        if (col2d) col2d.enabled = false;

        Vector3 startPos = transform.position;
        Vector3 endPos = targetBlock.transform.position;
        
        startPos.z = -5f; 
        endPos.z = -5f;

        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;
        if (col2d) col2d.enabled = true;

        AssignToBlock(targetBlock);
        targetBlock.ActivateImage();
        targetBlock.SetCurrentFood(this);

        isAnimating = false;
    }

    private void PlayClickSound()
    {
        if (clickSound != null && audioSource != null)
            audioSource.PlayOneShot(clickSound);
    }

    // --- NEW: Shrink Animation ---
    public void Shrink()
    {
        // Stop any current movement/growing
        StopAllCoroutines(); 
        StartCoroutine(ShrinkAndDestroy());
    }

    // Public method to trigger Grow
    public void Appear()
    {
        // Stop any current movement
        StopAllCoroutines();
        StartCoroutine(GrowAndAppear());
    }

    private IEnumerator ShrinkAndDestroy()
    {
        isAnimating = true; // Lock interaction
        if (col2d) col2d.enabled = false;
        if (rb2d) rb2d.simulated = false;

        float duration = 0.3f;
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

    private IEnumerator GrowAndAppear()
    {
        isAnimating = true; // Lock interaction
        
        if (col2d) col2d.enabled = false;
        if (rb2d) rb2d.simulated = false;

        float duration = 0.2f;
        float elapsed = 0f;
        
        // Ensure we capture the scale correctly. If scale is 0, default to 1
        Vector3 targetScale = transform.localScale;
        if(targetScale == Vector3.zero) targetScale = Vector3.one;

        transform.localScale = Vector3.zero;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0, 1, t); 

            transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = targetScale;

        // Unlock
        if (col2d) col2d.enabled = true;
        if (rb2d) rb2d.simulated = true;
        
        isAnimating = false;
    }
}