using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class Food : MonoBehaviour
{
    private Block block;
    
    private bool isDragging = false;
    private int draggingFingerId = -1;
    private bool isAnimating = false; 
    private static Food currentlyDraggedFood = null; 

    private Camera mainCamera;
    public AudioClip clickSound;
    private AudioClip[] evolutionSounds;
    private AudioSource audioSource;

    private Rigidbody2D rb2d;
    private Collider2D col2d;

    private Animator animator;

    // --- INTERACTION VARIABLES ---
    private Trash currentHoveredTrash;
    private Face currentHoveredFace;

    [Header("Movement Settings")]
    private float returnSpeed = 15f; 
    private float snapDistance = 0.01f;
    
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
        audioSource.loop = false;
        
        // Load dynamic evolution sounds
        evolutionSounds = new AudioClip[5];
        evolutionSounds[0] = Resources.Load<AudioClip>("Do");
        evolutionSounds[1] = Resources.Load<AudioClip>("Mi");
        evolutionSounds[2] = Resources.Load<AudioClip>("Fa");
        evolutionSounds[3] = Resources.Load<AudioClip>("La");
        evolutionSounds[4] = Resources.Load<AudioClip>("Do2");
        
        targetPosition = transform.position;

        if (animator != null) animator.Play("FoodPop"); 

        if (!isAnimating) 
        {
            FindInitialBlock();
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

        HandleUnifiedInput();

        if (isDragging)
        {
            Vector3 mouseWorldPos = GetInputWorldPosition();
            transform.position = new Vector3(mouseWorldPos.x, mouseWorldPos.y, -2f); 
            CheckHoverInteractions();
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * returnSpeed);
            if (Vector3.Distance(transform.position, targetPosition) < snapDistance)
            {
                transform.position = targetPosition;
            }
        }
    }

    // --- HINT SYSTEM METHODS ---
    public bool IsAnimating()
    {
        return isAnimating || isDragging;
    }

    public void PlayHintAnimation(Vector3 targetMatchPos)
    {
        if (isAnimating || isDragging) return;
        StartCoroutine(HintMoveRoutine(targetMatchPos));
    }

    private IEnumerator HintMoveRoutine(Vector3 targetMatchPos)
    {
        isAnimating = true; 
        Vector3 startPos = transform.position;
        Vector3 direction = (targetMatchPos - startPos).normalized;
        Vector3 nudgePos = startPos + (direction * 0.4f); 

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPos, nudgePos, Mathf.SmoothStep(0, 1, elapsed / duration));
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(nudgePos, startPos, Mathf.SmoothStep(0, 1, elapsed / duration));
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = startPos;
        isAnimating = false; 
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

    private void HandleUnifiedInput()
    {
        bool touchActive = false;

        if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
        {
            var touch = Touchscreen.current.touches[0];
            touchActive = true;
            int touchId = touch.touchId.ReadValue();

            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touchId))
                return; // UI Blocking

            // Allow picking up food if the user just pressed down OR if they are holding down/sliding over it
            if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began ||
                (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Moved && !isDragging && currentlyDraggedFood == null))
            {
                CheckRaycastAndDrag(touch.position.ReadValue(), touchId);
            }
            else if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Ended ||
                     touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                if (isDragging && draggingFingerId == touchId)
                    StopDragging();
            }
        }

        // Only process mouse if touch didn't handle it
        if (!touchActive && Mouse.current != null)
        {
            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return; // UI Blocking

            // Same for Mouse: isPressed allows the user to hold click and swipe into a food to pick it up
            if (Mouse.current.leftButton.isPressed)
            {
                if (!isDragging && currentlyDraggedFood == null)
                {
                    CheckRaycastAndDrag(Mouse.current.position.ReadValue(), -1);
                }
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame && isDragging && draggingFingerId == -1)
            {
                StopDragging();
            }
        }
    }

    private void CheckRaycastAndDrag(Vector2 screenPos, int fingerId)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        int layerMask = ~LayerMask.GetMask("Ignore Raycast");
        RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(ray, Mathf.Infinity, layerMask);

        // Sort hits by Z depth to always grab the top-most object if they overlap
        System.Array.Sort(hits, (a, b) => a.transform.position.z.CompareTo(b.transform.position.z));

        foreach (var hit in hits)
        {
            if (hit.collider.gameObject == gameObject)
            {
                StartDragging(fingerId);
                break; 
            }
        }
    }

    private void StartDragging(int fingerId)
    {
        if (currentlyDraggedFood != null && currentlyDraggedFood != this) return;
        currentlyDraggedFood = this;

        if (HintManager.Instance != null) HintManager.Instance.ResetTimer();
        
        if (!isDragging)
        {
            PlayClickSound();
            
            // --- NEW: Highlight block when food is pressed ---
            if (this.block != null)
            {
                this.block.ActivateImage();
            }
        }
        
        isDragging = true;
        draggingFingerId = fingerId;
    }

    private void StopDragging()
    {
        if (currentlyDraggedFood == this) currentlyDraggedFood = null;

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
        
        // Check Trash
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

        // Check Face
        foreach (var hit in hits)
        {
            Face face = hit.GetComponent<Face>();
            if (face == null) face = hit.GetComponentInParent<Face>();

            if (face != null)
            {
                face.Taste(gameObject.tag);
                if (this.block != null) this.block.ResetCurrentFood();
                StartCoroutine(ShrinkAndDestroy());
                return;
            }
        }

        // Check Blocks
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
                // FIX: Check if evolution is possible before merging
                bool canEvolve = false;
                if (FoodManager.Instance != null)
                {
                    canEvolve = (FoodManager.Instance.Evolve(this.tag) != null);
                }

                // If Tags match AND it can evolve (not max level), then Merge
                if (existingFood.CompareTag(this.tag) && canEvolve)
                {
                    if (previousBlock != null) previousBlock.ResetCurrentFood();
                    targetBlock.SetCurrentFood(this); 
                }
                else
                {
                    // SWAP: If tags don't match OR if they are both Max Level
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

    public void PlayEvolutionSound(int evolutionIndex)
    {
        // Start a coroutine to ensure Resources are loaded if this is called instantly
        StartCoroutine(PlayEvolutionSoundDelayed(evolutionIndex));
    }

    private IEnumerator PlayEvolutionSoundDelayed(int evolutionIndex)
    {
        // Map the evolution index (1-based from the chain array) to the 0-4 sound array
        int soundIndex = evolutionIndex - 1; 

        // Wait until Start has finished loading the clips
        yield return new WaitUntil(() => evolutionSounds != null);

        if (soundIndex >= 0 && soundIndex < evolutionSounds.Length)
        {
            AudioClip clipToPlay = evolutionSounds[soundIndex];
            if (clipToPlay != null)
            {
                // Create a temporary object to play pitched audio, since PlayClipAtPoint doesn't support pitch directly
                Vector3 playPosition = mainCamera != null ? mainCamera.transform.position : transform.position;
                GameObject tempAudioObj = new GameObject("TempAudio_Evolve");
                tempAudioObj.transform.position = playPosition;
                
                AudioSource tempSource = tempAudioObj.AddComponent<AudioSource>();
                tempSource.clip = clipToPlay;
                
                // Increase the pitch right here! (1.0 is normal, 1.2 is higher, etc)
                tempSource.pitch = 1.35f; 
                tempSource.volume = 1.0f;
                // Optional: make it 2D so it's always fully audible regardless of camera zoom
                tempSource.spatialBlend = 0f; 

                tempSource.Play();

                // Destroy the temporary invisible speaker as soon as the sound finishes playing
                Destroy(tempAudioObj, clipToPlay.length / tempSource.pitch);
            }
            else
            {
                Debug.LogWarning("Evolution sound at index " + soundIndex + " is missing in Resources folder. Keep in mind: The file MUST be named exactly 'Do', 'Mi', 'Fa', 'La', or 'Do2'.");
            }
        }
    }

    public void Shrink()
    {
        StopAllCoroutines(); 
        StartCoroutine(ShrinkAndDestroy());
    }

    public void Appear()
    {
        StopAllCoroutines();
        StartCoroutine(GrowAndAppear());
    }

    private IEnumerator ShrinkAndDestroy()
    {
        isAnimating = true; 
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
        isAnimating = true; 
        
        if (col2d) col2d.enabled = false;
        if (rb2d) rb2d.simulated = false;

        float duration = 0.2f;
        float elapsed = 0f;
        
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

        if (col2d) col2d.enabled = true;
        if (rb2d) rb2d.simulated = true;
        
        isAnimating = false;
    }
}