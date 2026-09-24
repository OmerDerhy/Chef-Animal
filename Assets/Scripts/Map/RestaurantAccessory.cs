using UnityEngine;
using System;

namespace MergesChef.Map
{
    [Serializable]
    public abstract class RestaurantAccessory : AccessoryBase
    {
        // Add shared fields for restaurant accessories like tables/chairs here
        
        public override bool IsPlacementValid(AreaBase targetArea, Vector2Int targetSubGridPos)
        {
            return targetArea != null && targetArea.Type == AreaType.Restaurant;
        }
    }
}
