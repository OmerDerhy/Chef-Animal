using UnityEngine;
using UnityEngine.Rendering;

namespace MergesChef.Map
{
    public abstract class SortableVisual : MonoBehaviour
    {
        public abstract void UpdateSortingOrder();

        protected void ApplySortingOrder(int gridX, int gridY, int typeOffset, int subGridX = 0, int subGridY = 0)
        {
            // The absolute best way to sort in 2D Isometric is by the actual world Y position!
            // Items visually lower on the screen have a smaller Y coordinate.
            // By multiplying by -100, smaller Y coordinates get HIGHER sorting orders (drawing in front).
            int positionOffset = -(int)(transform.position.y * 100);
            
            int baseOrder = typeOffset + positionOffset;
            
            // Apply atomic sorting using Unity's SortingGroup
            SortingGroup sg = GetComponent<SortingGroup>();
            if (sg == null) sg = gameObject.AddComponent<SortingGroup>();
            
            sg.sortingOrder = baseOrder;
        }
    }
}
