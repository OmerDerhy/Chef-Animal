using UnityEngine;
using System;

namespace MergesChef.Map
{
    [Serializable]
    public class IceCreamMachine : KitchenAccessory
    {
        public IceCreamMachine(string name)
        {
            this.accessoryName = name;
        }

        public override void Cook()
        {
            Debug.Log($"{accessoryName} is making ice cream!");
        }

        public override void Upgrade()
        {
            base.Upgrade();
            Debug.Log($"{accessoryName} upgraded! New flavors added.");
        }
    }
}
