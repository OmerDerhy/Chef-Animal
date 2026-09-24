using UnityEngine;

namespace MergesChef.Map
{
    public enum WallState
    {
        Regular,
        LeftWall,
        RightWall,
        Corner
    }

    public class AreaVisual : SortableVisual
    {
        // Reference to the logical data class
        public AreaBase LinkedArea { get; set; }

        [Header("Area Configuration")]
        public bool isKitchen;

        [Header("Visual Variations")]
        public GameObject regularVisual;
        public GameObject leftWallVisual;
        public GameObject rightWallVisual;
        public GameObject cornerVisual;
        
        public WallState CurrentState { get; private set; }

        private System.Collections.Generic.Dictionary<SpriteRenderer, int> originalSortingOrders;
        
        private SpriteRenderer[] renderers;
        private Color originalColor = Color.white;
        private Vector3 originalScale = Vector3.one;

        private void Awake()
        {
            originalScale = transform.localScale;
            
            // Auto-assign references if you forgot to drag them in the Inspector!
            if (regularVisual == null) regularVisual = FindChild("Regular");
            if (leftWallVisual == null) leftWallVisual = FindChild("LeftWall");
            if (rightWallVisual == null) rightWallVisual = FindChild("RightWall");
            if (cornerVisual == null) cornerVisual = FindChild("Corner");

            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            if (renderers.Length > 0)
            {
                originalColor = renderers[0].color;
            }

            originalSortingOrders = new System.Collections.Generic.Dictionary<SpriteRenderer, int>();
            foreach (var sr in renderers)
            {
                originalSortingOrders[sr] = sr.sortingOrder;
            }
        }

        public void SetSelected(bool isSelected)
        {
            // Scale up slightly relative to the original scale
            transform.localScale = isSelected ? originalScale * 1.05f : originalScale; // slightly less scale for area since it's big
            
            Color targetColor = isSelected ? new Color(1.2f, 1.2f, 1.2f, 1f) : originalColor;
            foreach (var sr in renderers)
            {
                sr.color = targetColor;
            }
        }

        private GameObject FindChild(string n)
        {
            foreach (Transform child in transform)
                if (child.name.Replace(" ", "").Contains(n)) return child.gameObject;
            return null;
        }

        public void SetWallState(WallState state)
        {
            CurrentState = state;
            
            // If you have exactly 4 separate models (Regular, Left, Right, Corner)
            if (regularVisual != null && cornerVisual != null)
            {
                regularVisual.SetActive(state == WallState.Regular);
                leftWallVisual.SetActive(state == WallState.LeftWall);
                rightWallVisual.SetActive(state == WallState.RightWall);
                cornerVisual.SetActive(state == WallState.Corner);
            }
            // If your prefab just has a "LeftWall" piece and a "RightWall" piece that toggle on/off independently
            else
            {
                if (regularVisual != null) regularVisual.SetActive(true); // Base floor
                if (leftWallVisual != null) leftWallVisual.SetActive(state == WallState.LeftWall || state == WallState.Corner);
                if (rightWallVisual != null) rightWallVisual.SetActive(state == WallState.RightWall || state == WallState.Corner);
            }
            
            UpdateSortingOrder();
        }

        public override void UpdateSortingOrder()
        {
            int gridX = 0;
            int gridY = 0;
            if (LinkedArea != null)
            {
                gridX = LinkedArea.Position.x;
                gridY = LinkedArea.Position.y;
            }

            int typeOffset = 15000;
            switch (CurrentState)
            {
                case WallState.Regular: typeOffset = 17000; break;
                case WallState.LeftWall: typeOffset = 16000; break;
                case WallState.RightWall: typeOffset = 16000; break;
                case WallState.Corner: typeOffset = 15000; break;
            }

            ApplySortingOrder(gridX, gridY, typeOffset);
        }
    }
}
