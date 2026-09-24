using UnityEngine;
using UnityEngine.Rendering;

namespace MergesChef.Map
{
    public class AccessoryVisual : SortableVisual
    {
        public AccessoryBase LinkedAccessory { get; set; }
        
        [Header("Visual Variations")]
        public GameObject[] visualVariations;

        private SpriteRenderer[] renderers;
        private Color originalColor = Color.white;
        private Vector3 originalScale = Vector3.one;

        private void Awake()
        {
            originalScale = transform.localScale;

            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            if (renderers.Length > 0)
            {
                originalColor = renderers[0].color;
            }
        }

        public void SetSelected(bool isSelected)
        {
            // Scale up slightly relative to the original scale
            transform.localScale = isSelected ? originalScale * 1.1f : originalScale;
            
            Color targetColor = isSelected ? new Color(1.2f, 1.2f, 1.2f, 1f) : originalColor;
            foreach (var sr in renderers)
            {
                sr.color = targetColor;
            }
        }

        public void SetVariation(int index)
        {
            if (visualVariations == null || visualVariations.Length == 0) return;

            // Turn off the root SpriteRenderer so it doesn't overlap with our variations
            SpriteRenderer rootRenderer = GetComponent<SpriteRenderer>();
            if (rootRenderer != null)
            {
                rootRenderer.enabled = false;
            }

            for (int i = 0; i < visualVariations.Length; i++)
            {
                if (visualVariations[i] != null)
                {
                    visualVariations[i].SetActive(i == index);
                }
            }
        }

        public Sprite GetVariationSprite(int index)
        {
            if (visualVariations != null && index >= 0 && index < visualVariations.Length)
            {
                if (visualVariations[index] != null)
                {
                    SpriteRenderer sr = visualVariations[index].GetComponent<SpriteRenderer>();
                    if (sr != null) return sr.sprite;
                    
                    // Sometimes the SpriteRenderer is on a child object
                    sr = visualVariations[index].GetComponentInChildren<SpriteRenderer>();
                    if (sr != null) return sr.sprite;
                }
            }
            return null;
        }

        public override void UpdateSortingOrder()
        {
            int gridX = 0;
            int gridY = 0;
            int subX = 0;
            int subY = 0;

            if (LinkedAccessory != null)
            {
                gridX = LinkedAccessory.Position.x;
                gridY = LinkedAccessory.Position.y;
                subX = LinkedAccessory.SubGridPosition.x;
                subY = LinkedAccessory.SubGridPosition.y;
            }

            // High type offset (20000) so they always draw on top of their own floor tile (15000-17000)
            ApplySortingOrder(gridX, gridY, 20000, subX, subY);
        }
    }
}
