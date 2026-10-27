using UnityEngine;
using UnityEngine.Rendering;
using DG.Tweening;
using MergesChef.Feel;

namespace MergesChef.Map
{
    public abstract class SortableVisual : MonoBehaviour
    {
        /// <summary>While held, the visual floats above every other sorting band.</summary>
        public bool IsDragVisual { get; private set; }

        protected SpriteRenderer[] cachedRenderers;
        protected Color originalColor = Color.white;
        protected Vector3 originalScale = Vector3.one;

        // Typed as Tween, not Tweener, so a LandSquash Sequence can be tracked and cancelled here too.
        private Tween scaleTween;

        /// <summary>Per-type selection scale — areas are large, so they swell less than accessories.</summary>
        protected virtual float SelectScaleFactor => Juice.SelectScale;

        protected virtual void Awake()
        {
            CacheVisualState();
        }

        protected void CacheVisualState()
        {
            originalScale = transform.localScale;
            cachedRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            if (cachedRenderers.Length > 0)
            {
                originalColor = cachedRenderers[0].color;
            }
        }

        // Unity stores sortingOrder in a 16-bit field.
        private const int MinSortingOrder = -32768;
        private const int MaxSortingOrder = 32767;

        public abstract void UpdateSortingOrder();

        protected void ApplySortingOrder(int gridX, int gridY, int typeOffset, int subGridX = 0, int subGridY = 0)
        {
            // The absolute best way to sort in 2D Isometric is by the actual world Y position!
            // Items visually lower on the screen have a smaller Y coordinate.
            // By multiplying by -100, smaller Y coordinates get HIGHER sorting orders (drawing in front).
            int positionOffset = -(int)(transform.position.y * 100);

            // A held object swaps into the drag band entirely instead of adding to its own, so it stays
            // inside the 16-bit range Unity stores sortingOrder in.
            int band = IsDragVisual ? Juice.DragSortingBand : typeOffset;

            // Anything outside this range wraps (50000 reads back as -15536) and the sprite ends up
            // behind the world instead of in front of it.
            int baseOrder = Mathf.Clamp(band + positionOffset, MinSortingOrder, MaxSortingOrder);

            // Apply atomic sorting using Unity's SortingGroup
            SortingGroup sg = GetComponent<SortingGroup>();
            if (sg == null) sg = gameObject.AddComponent<SortingGroup>();

            sg.sortingOrder = baseOrder;
        }

        public virtual void SetSelected(bool isSelected)
        {
            // While the player is physically holding the object, the drag owns its scale.
            if (IsDragVisual) return;

            AnimateScale(isSelected ? originalScale * SelectScaleFactor : originalScale,
                         Juice.SelectDuration,
                         isSelected ? Juice.PopEase : Juice.SettleEase);

            Juice.TintRenderers(cachedRenderers,
                                isSelected ? new Color(1.2f, 1.2f, 1.2f, 1f) : originalColor,
                                Juice.SelectDuration);
        }

        /// <summary>Lifts the object on pickup and drops it back into the world on release.</summary>
        public void SetDragging(bool dragging)
        {
            IsDragVisual = dragging;

            if (dragging)
            {
                AnimateScale(originalScale * Juice.DragLiftScale, Juice.DragLiftDuration, Juice.PopEase);
            }
            UpdateSortingOrder();
        }

        /// <summary>Squash-and-stretch landing once a drop has been accepted.</summary>
        public void PlayDropSettle()
        {
            scaleTween?.Kill();
            scaleTween = Juice.LandSquash(transform, originalScale, Juice.DropSettleDuration);
            UpdateSortingOrder();
        }

        /// <summary>Shake + red flash when a drop is refused, instead of silently snapping back.</summary>
        public void PlayRejected()
        {
            Juice.Shake(transform, 0.12f, 0.3f);
            Juice.FlashRenderers(cachedRenderers, new Color(1f, 0.45f, 0.45f, 1f), originalColor, 0.35f);
        }

        /// <summary>Eases back to a position instead of teleporting there.</summary>
        public Tweener ReturnTo(Vector3 worldPosition)
        {
            return transform.DOMove(worldPosition, Juice.RejectReturnDuration)
                            .SetEase(Juice.SettleEase)
                            .SetLink(gameObject)
                            .OnUpdate(UpdateSortingOrder);
        }

        private void AnimateScale(Vector3 target, float duration, Ease ease)
        {
            scaleTween?.Kill();
            scaleTween = Juice.ScaleTo(transform, target, duration, ease);
        }
    }
}
