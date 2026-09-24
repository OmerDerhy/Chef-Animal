using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MergesChef.UI
{
    public enum GiftRarity { Common, Rare, Epic, Legendary }

    public class MysteryGiftManager : MonoBehaviour
    {
        public static MysteryGiftManager Instance;

        [Header("Panel")]
        public GameObject giftPanel;
        public Image backgroundImage;
        public TextMeshProUGUI infoText;
        public Button actionButton;
        public TextMeshProUGUI actionButtonLabel;

        [Header("Rarity Backgrounds")]
        public Sprite commonBackground;
        public Sprite rareBackground;
        public Sprite epicBackground;
        public Sprite legendaryBackground;

        [Header("Rarity Gift Visuals (Common/Rare/Epic/Legandary prefab instances)")]
        [Tooltip("Only one of these is active at a time, matching the current rarity.")]
        public GameObject commonVisual;
        public GameObject rareVisual;
        public GameObject epicVisual;
        public GameObject legendaryVisual;

        [Header("Odds (chance to upgrade to the next rarity, per tap)")]
        [Range(0f, 1f)] public float commonToRareChance = 0.35f;
        [Range(0f, 1f)] public float rareToEpicChance = 0.15f;
        [Range(0f, 1f)] public float epicToLegendaryChance = 0.05f;

        [Header("Audio")]
        [Tooltip("Optional dedicated AudioSource. If left empty, AudioSource.PlayClipAtPoint is used instead.")]
        public AudioSource audioSource;
        [Tooltip("Played on every regular tap that doesn't upgrade the rarity")]
        public AudioClip tapSound;
        [Tooltip("Played when a tap upgrades the gift to a better rarity, and on the final reveal tap")]
        public AudioClip rarityUpSound;

        [Header("Animation - Spin")]
        [Tooltip("How long one full 360 degree spin takes, in seconds")]
        public float spinDuration = 0.35f;

        [Header("Animation - Float (kept centered, only bobs vertically)")]
        public float floatAmplitude = 15f;
        public float floatSpeed = 1.2f;

        [Header("Animation - Background Breathing (slow, since it's a background element)")]
        public float breatheAmplitude = 0.03f;
        public float breatheSpeed = 0.5f;

        private const int MaxTapsPerRarity = 4;
        private static readonly string[] RarityNames = { "Common", "Rare", "Epic", "Legendary" };

        private GiftRarity currentRarity;
        private int tapsUsedAtThisRarity;
        private bool isRevealPhase;

        private RectTransform backgroundRect;
        private Vector3 backgroundBaseScale = Vector3.one;
        private Vector2 commonBasePos, rareBasePos, epicBasePos, legendaryBasePos;

        private Coroutine floatRoutine;
        private Coroutine breatheRoutine;
        private Coroutine spinRoutine;
        private float visualSpinY;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (backgroundImage != null)
            {
                backgroundRect = backgroundImage.rectTransform;
                backgroundBaseScale = backgroundRect.localScale;
            }

            CacheVisualBasePosition(commonVisual, out commonBasePos);
            CacheVisualBasePosition(rareVisual, out rareBasePos);
            CacheVisualBasePosition(epicVisual, out epicBasePos);
            CacheVisualBasePosition(legendaryVisual, out legendaryBasePos);

            if (giftPanel != null) giftPanel.SetActive(false);
            if (actionButton != null) actionButton.onClick.AddListener(OnActionButtonPressed);
        }

        private void CacheVisualBasePosition(GameObject go, out Vector2 basePos)
        {
            basePos = Vector2.zero;
            if (go == null) return;
            RectTransform rect = go.GetComponent<RectTransform>();
            if (rect != null) basePos = rect.anchoredPosition;
        }

        public void OpenMysteryGift()
        {
            currentRarity = GiftRarity.Common;
            tapsUsedAtThisRarity = 0;
            isRevealPhase = false;
            visualSpinY = 0f;

            ResetVisualRotations();
            UpdateVisualsForCurrentRarity();
            if (infoText != null) infoText.text = $"{RarityNames[(int)currentRarity]} Gift - {MaxTapsPerRarity - tapsUsedAtThisRarity} taps left";
            if (actionButtonLabel != null) actionButtonLabel.text = "Tap!";

            if (giftPanel != null) giftPanel.SetActive(true);

            StartFloating();
            StartBreathing();
        }

        private void OnActionButtonPressed()
        {
            if (isRevealPhase)
            {
                CloseMysteryGift();
                return;
            }

            if (currentRarity == GiftRarity.Legendary || tapsUsedAtThisRarity >= MaxTapsPerRarity)
            {
                PlaySound(rarityUpSound);
                SpinCurrentVisual(extraTurns: 2);
                RevealResult();
                return;
            }

            tapsUsedAtThisRarity++;

            float chance = GetUpgradeChance(currentRarity);
            bool success = Random.value < chance;

            if (success)
            {
                PlaySound(rarityUpSound);

                currentRarity++;
                tapsUsedAtThisRarity = 0;
                UpdateVisualsForCurrentRarity();

                if (currentRarity == GiftRarity.Legendary)
                {
                    if (infoText != null) infoText.text = $"Upgraded to {RarityNames[(int)currentRarity]}! Tap to reveal your prize!";
                }
                else if (infoText != null)
                {
                    infoText.text = $"Upgraded to {RarityNames[(int)currentRarity]}! {MaxTapsPerRarity - tapsUsedAtThisRarity} taps left.";
                }
            }
            else
            {
                PlaySound(tapSound);

                if (infoText != null)
                {
                    infoText.text = tapsUsedAtThisRarity >= MaxTapsPerRarity
                        ? "No luck this time... Tap to reveal your prize!"
                        : $"No luck... {MaxTapsPerRarity - tapsUsedAtThisRarity} taps left.";
                }
            }

            SpinCurrentVisual(extraTurns: 1);
        }

        private float GetUpgradeChance(GiftRarity rarity)
        {
            switch (rarity)
            {
                case GiftRarity.Common: return commonToRareChance;
                case GiftRarity.Rare: return rareToEpicChance;
                case GiftRarity.Epic: return epicToLegendaryChance;
                default: return 0f;
            }
        }

        private void RevealResult()
        {
            isRevealPhase = true;
            StopFloating();
            StopBreathing();

            if (infoText != null) infoText.text = $"You got a {RarityNames[(int)currentRarity]} Gift!";
            if (actionButtonLabel != null) actionButtonLabel.text = "Collect";
        }

        private void CloseMysteryGift()
        {
            StopFloating();
            StopBreathing();
            if (spinRoutine != null) { StopCoroutine(spinRoutine); spinRoutine = null; }

            if (giftPanel != null) giftPanel.SetActive(false);
        }

        private void UpdateVisualsForCurrentRarity()
        {
            if (backgroundImage != null) backgroundImage.sprite = GetBackgroundForRarity(currentRarity);

            if (commonVisual != null) commonVisual.SetActive(currentRarity == GiftRarity.Common);
            if (rareVisual != null) rareVisual.SetActive(currentRarity == GiftRarity.Rare);
            if (epicVisual != null) epicVisual.SetActive(currentRarity == GiftRarity.Epic);
            if (legendaryVisual != null) legendaryVisual.SetActive(currentRarity == GiftRarity.Legendary);
        }

        private Sprite GetBackgroundForRarity(GiftRarity rarity)
        {
            switch (rarity)
            {
                case GiftRarity.Common: return commonBackground;
                case GiftRarity.Rare: return rareBackground;
                case GiftRarity.Epic: return epicBackground;
                case GiftRarity.Legendary: return legendaryBackground;
                default: return commonBackground;
            }
        }

        // ── Audio ────────────────────────────────────────────────────────────
        private void PlaySound(AudioClip clip)
        {
            if (clip == null) return;
            if (audioSource != null) audioSource.PlayOneShot(clip);
            else AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : Vector3.zero);
        }

        // ── Active visual lookup ────────────────────────────────────────────
        private GameObject GetActiveVisualGameObject()
        {
            switch (currentRarity)
            {
                case GiftRarity.Common: return commonVisual;
                case GiftRarity.Rare: return rareVisual;
                case GiftRarity.Epic: return epicVisual;
                case GiftRarity.Legendary: return legendaryVisual;
                default: return null;
            }
        }

        private RectTransform GetActiveVisualRect()
        {
            GameObject go = GetActiveVisualGameObject();
            return go != null ? go.GetComponent<RectTransform>() : null;
        }

        private void ResetVisualRotations()
        {
            ResetRotation(commonVisual);
            ResetRotation(rareVisual);
            ResetRotation(epicVisual);
            ResetRotation(legendaryVisual);
        }

        private void ResetRotation(GameObject go)
        {
            if (go == null) return;
            RectTransform rect = go.GetComponent<RectTransform>();
            if (rect != null) rect.localEulerAngles = Vector3.zero;
        }

        // ── Spin — Brawl Stars style, each tap spins the gift a full turn
        // around the vertical (Y) axis, like a coin/turntable flip ─────────
        private void SpinCurrentVisual(int extraTurns)
        {
            RectTransform rect = GetActiveVisualRect();
            if (rect == null) return;

            if (spinRoutine != null) StopCoroutine(spinRoutine);

            float fromY = visualSpinY;
            float toY = visualSpinY + 360f * extraTurns;
            spinRoutine = StartCoroutine(SpinRoutine(rect, fromY, toY));
        }

        private IEnumerator SpinRoutine(RectTransform rect, float fromY, float toY)
        {
            float duration = spinDuration;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f); // ease-out cubic
                visualSpinY = Mathf.Lerp(fromY, toY, eased);
                if (rect != null) rect.localEulerAngles = new Vector3(0f, visualSpinY, 0f);
                yield return null;
            }

            visualSpinY = toY;
            if (rect != null) rect.localEulerAngles = new Vector3(0f, visualSpinY, 0f);
        }

        // ── Float — gentle vertical bob, horizontal position untouched so it
        // always stays centered ─────────────────────────────────────────────
        private void StartFloating()
        {
            StopFloating();
            floatRoutine = StartCoroutine(FloatRoutine());
        }

        private void StopFloating()
        {
            if (floatRoutine != null)
            {
                StopCoroutine(floatRoutine);
                floatRoutine = null;
            }
            ApplyFloatOffset(0f);
        }

        private IEnumerator FloatRoutine()
        {
            float t = 0f;
            while (true)
            {
                t += Time.unscaledDeltaTime * floatSpeed;
                float offsetY = Mathf.Sin(t) * floatAmplitude;
                ApplyFloatOffset(offsetY);
                yield return null;
            }
        }

        private void ApplyFloatOffset(float offsetY)
        {
            ApplyOffsetToVisual(commonVisual, commonBasePos, offsetY);
            ApplyOffsetToVisual(rareVisual, rareBasePos, offsetY);
            ApplyOffsetToVisual(epicVisual, epicBasePos, offsetY);
            ApplyOffsetToVisual(legendaryVisual, legendaryBasePos, offsetY);
        }

        private void ApplyOffsetToVisual(GameObject go, Vector2 basePos, float offsetY)
        {
            if (go == null || !go.activeInHierarchy) return;
            RectTransform rect = go.GetComponent<RectTransform>();
            if (rect != null) rect.anchoredPosition = basePos + new Vector2(0f, offsetY);
        }

        // ── Background breathing — slow scale pulse behind the gift ────────
        private void StartBreathing()
        {
            StopBreathing();
            if (backgroundRect != null) breatheRoutine = StartCoroutine(BreatheRoutine());
        }

        private void StopBreathing()
        {
            if (breatheRoutine != null)
            {
                StopCoroutine(breatheRoutine);
                breatheRoutine = null;
            }
            if (backgroundRect != null) backgroundRect.localScale = backgroundBaseScale;
        }

        private IEnumerator BreatheRoutine()
        {
            float t = 0f;
            while (true)
            {
                t += Time.unscaledDeltaTime * breatheSpeed;
                float scale = 1f + Mathf.Sin(t) * breatheAmplitude;
                backgroundRect.localScale = backgroundBaseScale * scale;
                yield return null;
            }
        }
    }
}
