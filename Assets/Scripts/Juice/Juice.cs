using UnityEngine;
using DG.Tweening;

namespace MergesChef.Feel
{
    /// <summary>
    /// Single source of truth for how everything in the game animates. Tweak the constants
    /// here and the whole app re-tunes together instead of drifting apart per-screen.
    /// </summary>
    public static class Juice
    {
        // ── Press / tap (Brawl Stars buttons) ────────────────────────────────
        public const float PressScale = 0.92f;
        public const float PressDuration = 0.07f;
        public const float ReleaseDuration = 0.20f;

        // ── Panels ──────────────────────────────────────────────────────────
        public const float PopInFromScale = 0.86f;
        public const float PopInDuration = 0.26f;
        public const float PopOutToScale = 0.90f;
        public const float PopOutDuration = 0.13f;
        public const float FadeInDuration = 0.14f;

        // ── Market list ─────────────────────────────────────────────────────
        public const float ListStagger = 0.035f;
        public const float ListItemPopDuration = 0.24f;
        /// <summary>Ceiling on cascade delay so a long category doesn't take a second to finish appearing.</summary>
        public const float MaxStaggerDelay = 0.35f;

        public static float StaggerDelay(int index) => Mathf.Min(index * ListStagger, MaxStaggerDelay);

        // ── World objects (Clash of Clans drag) ─────────────────────────────
        public const float SelectScale = 1.10f;
        public const float AreaSelectScale = 1.05f;
        public const float SelectDuration = 0.16f;

        public const float DragLiftScale = 1.14f;
        public const float DragLiftDuration = 0.12f;
        /// <summary>SmoothDamp time for a dragged object chasing the snapped cell.</summary>
        public const float DragFollowSmoothTime = 0.055f;
        public const float DropSettleDuration = 0.26f;
        public const float RejectReturnDuration = 0.22f;

        public const Ease PopEase = Ease.OutBack;
        public const Ease SettleEase = Ease.OutQuad;
        public const Ease PressEase = Ease.OutQuad;

        /// <summary>
        /// Absolute sorting band a held object moves into, above the floor (~16000), grid lines
        /// (18000), accessories (20000) and characters (25000). It REPLACES the object's normal band
        /// rather than adding to it: Unity stores sortingOrder in 16 bits, so an additive offset here
        /// overflowed past 32767 and wrapped negative, hiding the object behind the world.
        /// </summary>
        public const int DragSortingBand = 30000;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            DOTween.Init(recycleAllByDefault: false, useSafeMode: true, logBehaviour: LogBehaviour.ErrorsOnly);
            DOTween.SetTweensCapacity(tweenersCapacity: 400, sequencesCapacity: 100);
        }

        // ── Scale ───────────────────────────────────────────────────────────

        public static Tweener ScaleTo(Transform t, Vector3 target, float duration, Ease ease, bool unscaled = false)
        {
            if (t == null) return null;
            return t.DOScale(target, duration).SetEase(ease).SetUpdate(unscaled).SetLink(t.gameObject);
        }

        public static Tweener PunchScale(Transform t, float strength, float duration, bool unscaled = false)
        {
            if (t == null) return null;
            return t.DOPunchScale(Vector3.one * strength, duration, vibrato: 6, elasticity: 0.5f)
                    .SetUpdate(unscaled).SetLink(t.gameObject);
        }

        /// <summary>Squash-and-stretch landing: flattens on impact, then springs back to <paramref name="restScale"/>.</summary>
        public static Sequence LandSquash(Transform t, Vector3 restScale, float duration, bool unscaled = false)
        {
            if (t == null) return null;

            Vector3 squashed = new Vector3(restScale.x * 1.16f, restScale.y * 0.84f, restScale.z);
            Sequence seq = DOTween.Sequence().SetUpdate(unscaled).SetLink(t.gameObject);
            seq.Append(t.DOScale(squashed, duration * 0.3f).SetEase(Ease.OutQuad));
            seq.Append(t.DOScale(restScale, duration * 0.7f).SetEase(Ease.OutBack));
            return seq;
        }

        public static Tweener Shake(Transform t, float strength, float duration, bool unscaled = false)
        {
            if (t == null) return null;
            return t.DOShakePosition(duration, strength, vibrato: 18, randomness: 90f, fadeOut: true)
                    .SetUpdate(unscaled).SetLink(t.gameObject);
        }

        /// <summary>Pops a freshly spawned list entry in. <paramref name="delay"/> staggers a whole list.</summary>
        public static void PopInListItem(GameObject go, float delay)
        {
            if (go == null) return;

            Transform t = go.transform;
            Vector3 rest = t.localScale;

            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();

            t.localScale = rest * 0.7f;
            cg.alpha = 0f;

            t.DOScale(rest, ListItemPopDuration).SetEase(PopEase).SetDelay(delay).SetUpdate(true).SetLink(go);
            cg.DOFade(1f, ListItemPopDuration * 0.7f).SetDelay(delay).SetUpdate(true).SetLink(go);
        }

        /// <summary>
        /// Slides a UI element to a new anchored position. <paramref name="id"/> must be an object that is
        /// never itself a tween target (the owning Button works well) — killing by the RectTransform would
        /// also kill the press-scale tween running on that same transform.
        /// </summary>
        public static void SlideAnchored(RectTransform rect, Vector2 target, float duration, object id)
        {
            if (rect == null || id == null) return;

            DOTween.Kill(id);
            rect.DOAnchorPos(target, duration).SetEase(PopEase).SetId(id).SetUpdate(true).SetLink(rect.gameObject);
        }

        // ── Color ───────────────────────────────────────────────────────────

        public static void TintRenderers(SpriteRenderer[] renderers, Color target, float duration, bool unscaled = false)
        {
            if (renderers == null) return;
            foreach (SpriteRenderer sr in renderers)
            {
                if (sr == null) continue;
                sr.DOKill();
                sr.DOColor(target, duration).SetEase(SettleEase).SetUpdate(unscaled).SetLink(sr.gameObject);
            }
        }

        /// <summary>Flashes every renderer to <paramref name="flash"/> and back — used for rejected placements.</summary>
        public static void FlashRenderers(SpriteRenderer[] renderers, Color flash, Color restore, float duration, bool unscaled = false)
        {
            if (renderers == null) return;
            foreach (SpriteRenderer sr in renderers)
            {
                if (sr == null) continue;
                sr.DOKill();
                Sequence seq = DOTween.Sequence().SetUpdate(unscaled).SetLink(sr.gameObject);
                seq.Append(sr.DOColor(flash, duration * 0.35f));
                seq.Append(sr.DOColor(restore, duration * 0.65f));
            }
        }
    }
}
