using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Face : MonoBehaviour
{
    [SerializeField] private Animator animator;
    public Target target;
    bool check;
    
    public AudioClip clickSound;
    private AudioSource audioSource;
    public AudioClip clickSound2;
    private AudioSource audioSource2;
    
    public AudioClip clickSound3; 
    private AudioSource audioSource3;

    private bool isLeaving = false; 
    private bool isAngry = false;   

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        clickSound = Resources.Load<AudioClip>("HappyVoice");
        
        audioSource2 = gameObject.AddComponent<AudioSource>();
        clickSound2 = Resources.Load<AudioClip>("SadVoice");

        audioSource3 = gameObject.AddComponent<AudioSource>();
        clickSound3 = Resources.Load<AudioClip>("AngryVoice");
        
        animator = GetComponent<Animator>();
        
        if (target == null)
        {
            target = GetComponent<Target>();
            if (target == null) target = GetComponentInChildren<Target>();

            if (target == null)
            {
                Debug.LogError("No Target script found on this Face object or its children!", this);
            }
        }
    }

    void Start()
    {
        StartCoroutine(AngryTimerSequence());
    }

    private IEnumerator AngryTimerSequence()
    {
        // Wait for 30 seconds
        yield return new WaitForSeconds(30f);

        // If the face is still here and hasn't started the finish/leave sequence
        if (!isLeaving)
        {
            isAngry = true;
            if (animator != null)
            {
                // Play the Angry animation state
                // Using CrossFade ensures a smooth transition from whatever it's doing
                animator.CrossFade("Angry", 0.1f); 
            }
            
            // --- NEW: Play Angry Sound ---
            if (clickSound3 != null) audioSource3.PlayOneShot(clickSound3);

            Debug.Log("Face became Angry due to timeout!");

            // Wait 10 seconds while angry
            yield return new WaitForSeconds(10f);

            // After being angry for 10 seconds, trigger finish sequence if not already leaving
            if (!isLeaving)
            {
                TriggerFinishSequence();
            }
        }
    }

    public void Taste(string tagName)
    {
        if (isLeaving) return; 

        check = false;
        
        if (target == null || target.objectsArray == null) return;

        int len = target.objectsArray.Length;

        for (int i = 0; i < len; i++)
        {
            if (target.objectsArray[i] != null && target.objectsArray[i].CompareTag(tagName))
            {
                // Correct Food!
                target.addpoint();
                target.RemoveFoodImage(i);
                target.RemoveFood(i);
                
                if(clickSound != null) audioSource.PlayOneShot(clickSound);
                
                StartCoroutine(PlayTasteSequence());
                
                check = true;
                break;
            }
        }

        if (!check)
        {
            isLeaving = true; 

            Invoke(nameof(PlaySadVoice), 0.5f);
            if(animator != null) animator.CrossFade("BadTaste", 0.05f, 0, 0f);

            Invoke(nameof(TriggerFinishSequence), 1.5f);
        }
    }

    private IEnumerator PlayTasteSequence()
    {
        if(animator != null) animator.CrossFade("TasteGood", 0.05f, 0, 0f);

        yield return new WaitForSeconds(2.5f);

        if (isAngry && !isLeaving)
        {
            if(animator != null) animator.CrossFade("Angry", 0.1f); 
        }
    }

    private void PlaySadVoice()
    {
        if(clickSound2 != null) audioSource2.PlayOneShot(clickSound2);
    }

    void OnMouseDown()
    {
        if (target != null && !isLeaving)
        {
            target.gameObject.SetActive(true);
            target.AnimationAgain();
        }
    }

    public void OpenMouth()
    {
        if (!isLeaving && animator != null) 
        {
            animator.SetInteger("Anim", 1);
        }
    }

    public void CloseMouth()
    {
        if(animator != null) animator.SetInteger("Anim", 0);
        
        if (isAngry && !isLeaving && animator != null)
        {
            animator.CrossFade("Angry", 0.1f);
        }
    }

    public void TriggerFinishSequence()
    {
        if (!isLeaving) isLeaving = true; 
        
        StopAllCoroutines(); 
        
        StartCoroutine(ShrinkAndRespawnSequence());
    }

    private IEnumerator ShrinkAndRespawnSequence()
    {
        float shrinkDuration = 0.5f;
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;

        while (elapsed < shrinkDuration)
        {
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / shrinkDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = Vector3.zero;

        // --- UPDATED: Wait Time Logic ---
        float waitTimeForSpawner = 7.0f - shrinkDuration; 

        // If CookieTime is active, reduce wait time drastically (e.g. to 1.5 seconds)
        if (CookieTime.IsCookieTimeActive)
        {
            waitTimeForSpawner = 1.5f;
        }

        if (target != null && target.parentSpawner != null)
        {
            target.parentSpawner.ScheduleRespawn(waitTimeForSpawner);
        }
        else
        {
            Debug.LogWarning("Face: Cannot schedule respawn - Target or ParentSpawner is missing.");
        }

        Destroy(gameObject);
    }
}