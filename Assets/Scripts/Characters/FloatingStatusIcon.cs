using UnityEngine;
using DG.Tweening;

namespace MergesChef.Characters
{
    /// <summary>
    /// A small icon that floats above a character - the "eating" heart and the "finished eating" coin
    /// reward both use this. Deliberately NOT parented to the character's own transform: a Customer's
    /// localScale changes when it sits down (see Customer.OnReachedDestination), and coupling the icon
    /// to that would warp its animation. Instead the owner repositions it each frame via SetPosition.
    /// </summary>
    public class FloatingStatusIcon : MonoBehaviour
    {
        // Above characters (25000), drag-lift (30000) and below the kitchen dish-stock UI (32000) -
        // always visible over gameplay, never over build-mode or UI chrome.
        private const int IconSortingOrder = 31000;

        private SpriteRenderer spriteRenderer;
        private Sequence activeSequence;
        private Vector3 restScale;

        public static FloatingStatusIcon Create(Sprite sprite, Vector3 worldPosition, Vector3 restScale)
        {
            GameObject go = new GameObject(sprite != null ? sprite.name + " Icon" : "FloatingStatusIcon");
            go.transform.position = worldPosition;

            FloatingStatusIcon icon = go.AddComponent<FloatingStatusIcon>();
            icon.spriteRenderer = go.AddComponent<SpriteRenderer>();
            icon.spriteRenderer.sprite = sprite;
            icon.spriteRenderer.sortingOrder = IconSortingOrder;
            icon.restScale = restScale;
            go.transform.localScale = restScale;
            return icon;
        }

        public void SetPosition(Vector3 worldPosition)
        {
            transform.position = worldPosition;
        }

        /// <summary>Hides/shows the icon outright - used to pull it off-screen during Build Mode, since
        /// it lives on its own root GameObject and isn't deactivated along with its owning character.</summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        /// <summary>Continuous gentle scale pulse for an icon that's showing an ongoing state (the heart).</summary>
        public void PlayBreathing()
        {
            activeSequence?.Kill();
            transform.localScale = restScale;
            transform.localRotation = Quaternion.identity;

            activeSequence = DOTween.Sequence().SetLink(gameObject).SetLoops(-1, LoopType.Yoyo);
            activeSequence.Append(transform.DOScale(restScale * 1.2f, 0.45f).SetEase(Ease.InOutSine));
        }

        /// <summary>Breathing plus a slight rotational wiggle - a "twinkle" for a persistently-shown icon (the staff star).</summary>
        public void PlayTwinkle()
        {
            activeSequence?.Kill();
            transform.localScale = restScale;
            transform.localRotation = Quaternion.identity;

            activeSequence = DOTween.Sequence().SetLink(gameObject).SetLoops(-1, LoopType.Yoyo);
            activeSequence.Append(transform.DOScale(restScale * 1.15f, 0.5f).SetEase(Ease.InOutSine));
            activeSequence.Join(transform.DORotate(new Vector3(0f, 0f, 10f), 0.5f).SetEase(Ease.InOutSine));
        }

        /// <summary>Pops in, floats upward, fades out, then destroys itself - a one-shot reward cue (the coin).</summary>
        public void PlayRewardPopAndDestroy(float floatDistance = 0.35f, float duration = 1.1f)
        {
            activeSequence?.Kill();
            transform.localScale = Vector3.zero;

            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;

            Vector3 endPos = transform.position + Vector3.up * floatDistance;

            activeSequence = DOTween.Sequence().SetLink(gameObject);
            activeSequence.Append(transform.DOScale(restScale, 0.25f).SetEase(Ease.OutBack));
            activeSequence.AppendInterval(0.2f);
            activeSequence.Append(transform.DOMove(endPos, duration).SetEase(Ease.OutQuad));
            activeSequence.Join(spriteRenderer.DOFade(0f, duration * 0.6f).SetEase(Ease.InQuad).SetDelay(duration * 0.4f));
            activeSequence.OnComplete(() => Destroy(gameObject));
        }

        /// <summary>Shrinks out and destroys itself - used to cut the heart off if eating ends abruptly.</summary>
        public void StopAndDestroy(float duration = 0.15f)
        {
            activeSequence?.Kill();
            activeSequence = DOTween.Sequence().SetLink(gameObject);
            activeSequence.Append(transform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack));
            activeSequence.OnComplete(() => Destroy(gameObject));
        }

        private void OnDestroy()
        {
            activeSequence?.Kill();
        }
    }
}
