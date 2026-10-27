using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace MergesChef.Feel
{
    /// <summary>
    /// Squash on press, overshoot on release. Added via <see cref="Attach"/> from the managers that
    /// own the buttons, so existing prefabs and their onClick wiring are untouched.
    /// </summary>
    [DisallowMultipleComponent]
    public class JuicyButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private Vector3 restScale = Vector3.one;
        private bool restCaptured;
        private Tweener tween;

        public static void Attach(params Component[] targets)
        {
            if (targets == null) return;
            foreach (Component target in targets)
            {
                if (target == null) continue;
                if (target.GetComponent<JuicyButton>() == null)
                {
                    target.gameObject.AddComponent<JuicyButton>();
                }
            }
        }

        private void CaptureRest()
        {
            if (restCaptured) return;
            restScale = transform.localScale;
            restCaptured = true;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            CaptureRest();
            tween?.Kill();
            tween = Juice.ScaleTo(transform, restScale * Juice.PressScale,
                                  Juice.PressDuration, Juice.PressEase, unscaled: true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            CaptureRest();
            tween?.Kill();
            tween = Juice.ScaleTo(transform, restScale,
                                  Juice.ReleaseDuration, Juice.PopEase, unscaled: true);
        }

        private void OnDisable()
        {
            // A button hidden mid-press must not come back squashed.
            tween?.Kill();
            if (restCaptured) transform.localScale = restScale;
        }
    }
}
