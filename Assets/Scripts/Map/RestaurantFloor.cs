using UnityEngine;
using System;

namespace MergesChef.Map
{
    [Serializable]
    public class RestaurantFloor : AreaBase
    {
        [SerializeField] private int maxCapacity;
        [SerializeField] private float eatingTimeModifier;

        public RestaurantFloor(Vector2Int initialPosition, int startLevel, int startCost, int capacity, float timeModifier, AreaType type) 
            : base(initialPosition, startLevel, startCost, type)
        {
            maxCapacity = capacity;
            eatingTimeModifier = timeModifier;
        }

        protected override void ApplyUpgradeEffect()
        {
            // Manipulate customer flow metrics
            maxCapacity += 2; // Example increment
            eatingTimeModifier *= 0.9f; // Decrease eating time by 10%
            
            Debug.Log($"RestaurantFloor Upgraded! New Capacity: {maxCapacity}, Eating Time Mod: {eatingTimeModifier}");
        }
    }
}
