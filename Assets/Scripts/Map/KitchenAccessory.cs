using UnityEngine;
using System;

namespace MergesChef.Map
{
    [Serializable]
    public abstract class KitchenAccessory : AccessoryBase, IKitchenAction
    {
        [SerializeField] protected Recipe[] meals;
        [SerializeField] protected int timeToCook;

        public abstract void Cook();

        public override bool IsPlacementValid(AreaBase targetArea, Vector2Int targetSubGridPos)
        {
            return targetArea != null && targetArea.Type == AreaType.Kitchen;
        }
    }
}
