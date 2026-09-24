using UnityEngine;
using System;

namespace MergesChef.Map
{
    [Serializable]
    public class Table : RestaurantAccessory
    {
        public Table(string name)
        {
            this.accessoryName = name;
        }

        public override void Upgrade()
        {
            base.Upgrade();
            Debug.Log($"{accessoryName} upgraded! Fancier tablecloth.");
        }
    }
}
