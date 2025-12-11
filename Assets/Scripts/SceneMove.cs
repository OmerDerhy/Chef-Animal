using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class SceneMove : MonoBehaviour
{
    public int sceneIndex;

    void Update()
    {
        // טיפול בטאץ' (מסך מגע)
        foreach (Touch touch in Input.touches)
        {
            if (touch.phase == TouchPhase.Began)
            {
                Vector3 touchPos = Camera.main.ScreenToWorldPoint(touch.position);
                RaycastHit2D hit = Physics2D.Raycast(touchPos, Vector2.zero);
                
                if (hit.collider != null && hit.collider.gameObject == gameObject)
                {
                    SceneManager.LoadScene(sceneIndex);
                }
            }
        }
    }

    void OnMouseDown()
    {
        // טיפול בלחיצות עכבר (כולל סימולטורים)
        SceneManager.LoadScene(sceneIndex);
    }
}