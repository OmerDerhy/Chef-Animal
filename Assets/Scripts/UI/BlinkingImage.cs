using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MergesChef.UI
{
    /// <summary>
    /// Cycles an Image between an "eyes open" and "eyes closed" sprite on a natural, irregular blink
    /// cadence - mostly open, with a quick closed flash - rather than a steady metronome swap.
    /// </summary>
    public class BlinkingImage : MonoBehaviour
    {
        public Image targetImage;
        public Sprite openSprite;
        public Sprite closedSprite;

        [Tooltip("Random range (seconds) between blinks while eyes are open.")]
        public Vector2 blinkIntervalRange = new Vector2(2f, 5f);
        [Tooltip("How long the eyes stay closed during a blink.")]
        public float blinkDuration = 0.12f;

        private void Start()
        {
            if (targetImage == null) targetImage = GetComponent<Image>();
            if (targetImage != null && openSprite != null) targetImage.sprite = openSprite;
            StartCoroutine(BlinkLoop());
        }

        private IEnumerator BlinkLoop()
        {
            while (true)
            {
                float wait = Random.Range(blinkIntervalRange.x, blinkIntervalRange.y);
                yield return new WaitForSecondsRealtime(wait);

                if (targetImage != null && closedSprite != null) targetImage.sprite = closedSprite;
                yield return new WaitForSecondsRealtime(blinkDuration);

                if (targetImage != null && openSprite != null) targetImage.sprite = openSprite;
            }
        }
    }
}
