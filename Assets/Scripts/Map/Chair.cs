using UnityEngine;
using System;

namespace MergesChef.Map
{
    [Serializable]
    public class Chair : RestaurantAccessory
    {
        [NonSerialized] public bool IsOccupied = false;

        public Chair(string name)
        {
            this.accessoryName = name;
        }

        public override void Upgrade()
        {
            base.Upgrade();
            Debug.Log($"{accessoryName} upgraded! Cushions added.");
        }

        public override bool IsPlacementValid(AreaBase targetArea, Vector2Int targetSubGridPos)
        {
            // A Chair must be placed in the same row as a Table, directly adjacent (x-1 or x+1)
            // It can connect to a table in the same area, or a table in a neighboring area.
            bool hasTableOnLeft = CheckTableOnLeft(targetArea, targetSubGridPos);
            bool hasTableOnRight = CheckTableOnRight(targetArea, targetSubGridPos);

            if (hasTableOnLeft || hasTableOnRight)
            {
                return true;
            }

            Debug.LogWarning("Chairs must be placed directly next to a Table in the same row.");
            return false;
        }

        private bool CheckTableOnLeft(AreaBase currentArea, Vector2Int subGridPos)
        {
            if (currentArea == null) return false;

            // If not on the left edge, just check the local area
            if (subGridPos.x > 0)
            {
                return currentArea.GetAccessoryAt(new Vector2Int(subGridPos.x - 1, subGridPos.y)) is Table;
            }
            
            // If on the left edge, check the right edge of the area to our left
            Map map = GameObject.FindObjectOfType<Map>();
            if (map != null)
            {
                AreaBase leftArea = map.GetAreaAt(currentArea.Position + new Vector2Int(-1, 0));
                if (leftArea != null)
                {
                    return leftArea.GetAccessoryAt(new Vector2Int(2, subGridPos.y)) is Table;
                }
            }
            return false;
        }

        private bool CheckTableOnRight(AreaBase currentArea, Vector2Int subGridPos)
        {
            if (currentArea == null) return false;

            // If not on the right edge, just check the local area
            if (subGridPos.x < 2)
            {
                return currentArea.GetAccessoryAt(new Vector2Int(subGridPos.x + 1, subGridPos.y)) is Table;
            }
            
            // If on the right edge, check the left edge of the area to our right
            Map map = GameObject.FindObjectOfType<Map>();
            if (map != null)
            {
                AreaBase rightArea = map.GetAreaAt(currentArea.Position + new Vector2Int(1, 0));
                if (rightArea != null)
                {
                    return rightArea.GetAccessoryAt(new Vector2Int(0, subGridPos.y)) is Table;
                }
            }
            return false;
        }

        public bool IsFacingFront { get; private set; }

        /// <summary>
        /// The world position where the customer should sit.
        /// Reads from a child GameObject named "SeatPoint" on the chair prefab.
        /// Falls back to the visual model's own position if no SeatPoint is found.
        /// </summary>
        public Vector3 SeatPosition
        {
            get
            {
                if (visualModel == null) return Vector3.zero;
                Transform seatPoint = visualModel.transform.Find("SeatPoint");
                return seatPoint != null ? seatPoint.position : visualModel.transform.position;
            }
        }

        public override void UpdateVisualState(AreaBase currentArea)
        {
            if (visualModel == null) return;
            
            AccessoryVisual accVisual = visualModel.GetComponent<AccessoryVisual>();
            if (accVisual == null) return;

            // Check where the table is to determine which variation to use
            bool hasTableOnLeft = CheckTableOnLeft(currentArea, subGridPosition);
            bool hasTableOnRight = CheckTableOnRight(currentArea, subGridPosition);
            
            IsFacingFront = hasTableOnRight;
            
            Debug.Log($"Updating Chair Visuals! Pos: {subGridPosition}. Table on Left: {hasTableOnLeft}, Table on Right: {hasTableOnRight}");

            if (hasTableOnRight)
            {
                accVisual.SetVariation(0); // Variation 0: facing right towards the table
            }
            else if (hasTableOnLeft)
            {
                accVisual.SetVariation(1); // Variation 1: facing left towards the table
            }
        }
    }
}
