using UnityEngine;
using System.Collections;

public class HintManager : MonoBehaviour
{
    public static HintManager Instance;

    [Header("Settings")]
    public float timeBeforeHint = 4.0f; // 4 Seconds of inactivity
    
    private float currentTimer = 0f;
    private BlockManager blockManager;
    private bool isHinting = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        blockManager = Object.FindFirstObjectByType<BlockManager>();
        ResetTimer();
    }

    void Update()
    {
        // Don't count time if we are currently playing a hint animation
        if (isHinting) return;

        currentTimer += Time.deltaTime;

        if (currentTimer >= timeBeforeHint)
        {
            TryShowHint();
            // Reset timer so it waits another 4 seconds before trying again
            currentTimer = 0f; 
        }
    }

    // Called by Food.cs whenever the player touches something
    public void ResetTimer()
    {
        currentTimer = 0f;
    }

    private void TryShowHint()
    {
        if (blockManager == null) return;

        Block[] blocks = blockManager.GetBlocks();
        if (blocks == null) return;

        // Search for any pair of foods with same tag
        for (int i = 0; i < blocks.Length; i++)
        {
            if (blocks[i].HasCurrentFood())
            {
                Food foodA = blocks[i].GetCurrentFood();
                
                // Skip invalid or busy foods
                if (foodA == null || foodA.IsAnimating()) continue;

                for (int j = i + 1; j < blocks.Length; j++)
                {
                    if (blocks[j].HasCurrentFood())
                    {
                        Food foodB = blocks[j].GetCurrentFood();
                        
                        if (foodB == null || foodB.IsAnimating()) continue;

                        if (foodA.CompareTag(foodB.tag))
                        {
                            // Found a matching pair! Animate them towards each other.
                            StartCoroutine(PlayHintRoutine(foodA, foodB));
                            return; // Only show one hint at a time
                        }
                    }
                }
            }
        }
    }

    private IEnumerator PlayHintRoutine(Food a, Food b)
    {
        isHinting = true;

        // Tell both foods to nudge towards the other's position
        a.PlayHintAnimation(b.transform.position);
        b.PlayHintAnimation(a.transform.position);

        // Wait for animation to finish (approx 0.6s) + a little buffer
        yield return new WaitForSeconds(1.0f);

        isHinting = false;
    }
}