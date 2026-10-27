using UnityEngine;
using DG.Tweening;

namespace MergesChef.Feel
{
    /// <summary>
    /// Pop-in / pop-out for panels. Call the static <see cref="Show"/> / <see cref="Hide"/> in place of
    /// SetActive(true/false) — the component is created on demand, so no prefab setup is required and
    /// panels that were never touched keep behaving exactly as before.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIPopAnimator : MonoBehaviour
    {
        private Vector3 restScale = Vector3.one;
        private bool restCaptured;
        private CanvasGroup group;
        private Sequence current;

        public static void Show(GameObject panel)
        {
            if (panel == null) return;
            Get(panel).PlayShow();
        }

        public static void Hide(GameObject panel)
        {
            if (panel == null) return;

            // Never animate something that is already hidden (Start() closes every panel this way).
            if (!panel.activeSelf)
            {
                panel.SetActive(false);
                return;
            }
            Get(panel).PlayHide();
        }

        private static UIPopAnimator Get(GameObject panel)
        {
            UIPopAnimator animator = panel.GetComponent<UIPopAnimator>();
            if (animator == null) animator = panel.AddComponent<UIPopAnimator>();
            return animator;
        }

        /// <summary>Captured before the first animation runs, so the prefab's authored scale is the rest pose.</summary>
        private void CaptureRest()
        {
            if (restCaptured) return;
            restScale = transform.localScale;
            restCaptured = true;
        }

        private CanvasGroup Group()
        {
            if (group == null)
            {
                group = GetComponent<CanvasGroup>();
                if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            }
            return group;
        }

        public void PlayShow()
        {
            CaptureRest();
            current?.Kill();

            gameObject.SetActive(true);

            CanvasGroup cg = Group();
            transform.localScale = restScale * Juice.PopInFromScale;
            cg.alpha = 0f;

            current = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            current.Append(transform.DOScale(restScale, Juice.PopInDuration).SetEase(Juice.PopEase));
            current.Join(cg.DOFade(1f, Juice.FadeInDuration).SetEase(Juice.SettleEase));
        }

        public void PlayHide()
        {
            CaptureRest();
            current?.Kill();

            CanvasGroup cg = Group();

            current = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            current.Append(transform.DOScale(restScale * Juice.PopOutToScale, Juice.PopOutDuration).SetEase(Ease.InQuad));
            current.Join(cg.DOFade(0f, Juice.PopOutDuration).SetEase(Ease.InQuad));
            current.OnComplete(() =>
            {
                // Cleared first: SetActive(false) fires OnDisable, which would otherwise try to Kill
                // this very sequence from inside its own completion callback.
                current = null;

                gameObject.SetActive(false);
                // Leave the panel in its rest pose so a plain SetActive(true) elsewhere still looks right.
                transform.localScale = restScale;
                cg.alpha = 1f;
            });
        }

        private void OnDisable()
        {
            current?.Kill();
            if (restCaptured) transform.localScale = restScale;
            if (group != null) group.alpha = 1f;
        }
    }
}
