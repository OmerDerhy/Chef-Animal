using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class HomeFoodItem : MonoBehaviour
{
    private float fallSpeed;
    private float rotateSpeed;
    
    private Camera mainCam;
    private float bottomLimit;
    private bool isShrinking = false; // Flag to prevent multiple interactions

    [Header("Effects")]
    [Tooltip("Sound to play when the item is popped. (Defaults to 'HappyVoice' if none given)")]
    private AudioClip popSound;

    public void Initialize(float speed, float rot)
    {
        fallSpeed = speed;
        rotateSpeed = rot;
    }

    void Start()
    {
        mainCam = Camera.main;
        
        // Calculate bottom limit to destroy object if missed (-0.2 is below bottom edge)
        bottomLimit = mainCam.ViewportToWorldPoint(new Vector3(0, -0.2f, 0)).y;
        
        // Ensure we have a collider for clicking
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            // Add a CircleCollider with a decent size so it's easy to swipe
            CircleCollider2D circle = gameObject.AddComponent<CircleCollider2D>();
            circle.radius = 0.5f; 
        }

        // Preload an energetic sound from resources
        popSound = Resources.Load<AudioClip>("FoodClick");
    }

    void Update()
    {
        if (isShrinking) return; 

        // 1. Fall Down
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime, Space.World);

        // 2. Rotate
        transform.Rotate(Vector3.forward, rotateSpeed * Time.deltaTime);

        // 3. Check Bounds
        if (transform.position.y < bottomLimit)
        {
            Destroy(gameObject);
            return;
        }

        // 4. Handle Input (Clicking or swiping the falling food)
        CheckInput();
    }

    private void CheckInput()
    {
        bool clicked = false;

        // Check Mouse: allows holding down and "swipe catching"
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            Vector2 mousePos = mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            if (GetComponent<Collider2D>().OverlapPoint(mousePos)) clicked = true;
        }

        // Check Touch: Allow both Began and Moved to catch foods when swiping
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                var phase = touch.phase.ReadValue();
                if (phase == UnityEngine.InputSystem.TouchPhase.Began || phase == UnityEngine.InputSystem.TouchPhase.Moved)
                {
                    Vector2 touchPos = mainCam.ScreenToWorldPoint(touch.position.ReadValue());
                    if (GetComponent<Collider2D>().OverlapPoint(touchPos)) clicked = true;
                }
            }
        }

        if (clicked)
        {
            PopTarget();
        }
    }

    private void PopTarget()
    {
        if (isShrinking) return; // Prevent double firing
        
        isShrinking = true;
        
        // Play Pop Sound
        if (popSound != null)
        {
            Vector3 playPos = mainCam != null ? mainCam.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(popSound, playPos, 0.7f);
        }

        // Shrink and disappear
        StartCoroutine(ShrinkRoutine());
    }

    private IEnumerator ShrinkRoutine()
    {
        // Disable collider so it can't be interacted with again
        if (GetComponent<Collider2D>()) GetComponent<Collider2D>().enabled = false;

        float duration = 0.2f; // Quick pop animation
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        
        // Small expand before shrink for satisfying feedback
        Vector3 popScale = startScale * 1.3f;

        // Expand
        while (elapsed < 0.05f)
        {
            transform.localScale = Vector3.Lerp(startScale, popScale, elapsed / 0.05f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Shrink to zero
        elapsed = 0f;
        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(popScale, Vector3.zero, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}
