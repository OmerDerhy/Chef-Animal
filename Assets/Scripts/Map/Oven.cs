using UnityEngine;
using System;

namespace MergesChef.Map
{
    [Serializable]
    public class Oven : KitchenAccessory
    {
        public Oven(string name)
        {
            this.accessoryName = name;
        }

        public override void Cook()
        {
            Debug.Log($"{accessoryName} is baking something hot!");
        }

        public override void Upgrade()
        {
            base.Upgrade();
            Debug.Log($"{accessoryName} upgraded! Cooks faster.");
            timeToCook = Mathf.Max(1, timeToCook - 1); // Example upgrade effect
        }
    }
}
