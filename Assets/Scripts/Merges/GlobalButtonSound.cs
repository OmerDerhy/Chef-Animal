using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class GlobalButtonSound : MonoBehaviour
{
    private AudioClip clickSound;
    private AudioSource audioSource;

    // This makes the script run automatically when the game starts, 
    // so you don't even need to place it in the scene!
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        GameObject go = new GameObject("GlobalButtonSoundManager");
        DontDestroyOnLoad(go);
        go.AddComponent<GlobalButtonSound>();
    }

    void Start()
    {
        clickSound = Resources.Load<AudioClip>("FoodClick");
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        bool isClickDown = Input.GetMouseButtonDown(0);
        bool isTouchDown = Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;

        if (isClickDown || isTouchDown)
        {
            if (EventSystem.current != null)
            {
                PointerEventData pointerData = new PointerEventData(EventSystem.current)
                {
                    position = isTouchDown ? (Vector2)Input.GetTouch(0).position : (Vector2)Input.mousePosition
                };

                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);

                if (results.Count > 0)
                {
                    // Check if the UI element we tapped on (or its parent) is a Button
                    Button btn = results[0].gameObject.GetComponentInParent<Button>();
                    
                    // If it is an interactable button, play the sound
                    if (btn != null && btn.interactable)
                    {
                        PlayClickSound();
                    }
                }
            }
        }
    }

    private void PlayClickSound()
    {
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }
}
