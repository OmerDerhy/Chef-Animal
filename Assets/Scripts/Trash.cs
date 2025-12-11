using UnityEngine;

public class Trash : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void SetOpenState(bool isOpen)
    {
        if (animator != null)
        {
            animator.SetBool("Open", isOpen);
        }
    }

    public void PlayDeleteEffect()
    {
        // Close the lid when we eat the item
        SetOpenState(false);
        Debug.Log("Item Trashed!");
        
        // Optional: Add a "Chomp" trigger here if you have one
        // if (animator) animator.SetTrigger("Eat");
    }
}