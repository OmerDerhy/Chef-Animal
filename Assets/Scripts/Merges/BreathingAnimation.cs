using UnityEngine;

public class BreathingAnimation : MonoBehaviour
{
    [Header("Breathing Settings")]
    [Tooltip("How much larger the image gets at the peak of the breath. (e.g., 1.1 = 10% larger)")]
    public float maxScaleMultiplier = 1.05f;

    [Tooltip("How quickly the image breathes in and out.")]
    public float breathingSpeed = 2f;

    private Vector3 originalScale;

    void Start()
    {
        // Remember the original size of the image/object
        originalScale = transform.localScale;
    }

    void Update()
    {
        // Mathf.Sin smoothly animates between -1 and 1 over time.
        // We adjust it so it animates between 0 and 1 instead.
        float breatheAmount = (Mathf.Sin(Time.time * breathingSpeed) + 1f) / 2f;

        // Smoothly transition between normal scale (1x) and max scale based on the speed
        float currentScale = Mathf.Lerp(1f, maxScaleMultiplier, breatheAmount);

        // Apply the new scale to the image
        transform.localScale = originalScale * currentScale;
    }
}
