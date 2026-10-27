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

        // Areas are large, so they swell less than accessories when selected.
        protected override float SelectScaleFactor => MergesChef.Feel.Juice.AreaSelectScale;

        protected override void Awake()
        {
            base.Awake();

            // Auto-assign references if you forgot to drag them in the Inspector!
            if (regularVisual == null) regularVisual = FindChild("Regular");
            if (leftWallVisual == null) leftWallVisual = FindChild("LeftWall");
            if (rightWallVisual == null) rightWallVisual = FindChild("RightWall");
            if (cornerVisual == null) cornerVisual = FindChild("Corner");

            originalSortingOrders = new System.Collections.Generic.Dictionary<SpriteRenderer, int>();
            foreach (var sr in cachedRenderers)
            {
                originalSortingOrders[sr] = sr.sortingOrder;
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

        // Base sorting band for areas. Real world-Y (via ApplySortingOrder) dominates area-vs-area
        // ordering; these nudges only settle an exact Y tie (anti-diagonal neighbors in this skewed
        // grid), so they must stay far smaller than one grid row's real depth difference (~42 units).
        private const int AreaBand = 16000;

        public override void UpdateSortingOrder()
        {
            int gridX = 0;
            int gridY = 0;
            if (LinkedArea != null)
            {
                gridX = LinkedArea.Position.x;
                gridY = LinkedArea.Position.y;
            }

            int typeOffset = AreaBand;
            switch (CurrentState)
            {
                case WallState.Regular: typeOffset = AreaBand + 0; break;
                case WallState.LeftWall: typeOffset = AreaBand + 1; break;
                case WallState.RightWall: typeOffset = AreaBand + 1; break;
                case WallState.Corner: typeOffset = AreaBand + 2; break;
            }

            ApplySortingOrder(gridX, gridY, typeOffset);
        }
    }
}
