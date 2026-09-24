using UnityEngine;

namespace MergesChef.Map
{
    [CreateAssetMenu(fileName = "New Dish", menuName = "Merges Chef/Dish")]
    public class Dish : ScriptableObject
    {
        [Header("Basic Info")]
        public string dishName;
        public Sprite icon;

        [Header("Cooking Variables")]
        [Tooltip("Lights required to start cooking")]
        public int cost;
        
        [Tooltip("Coins earned when the dish is served")]
        public int revenuePerDish;
        
        [Tooltip("Time in seconds required to cook")]
        public int timeToCook;
        
        [Tooltip("How many servings this recipe yields")]
        public int amountOfDishes;

        [Header("Unlock Requirements")]
        [Tooltip("The machine must be at least this level to cook this dish")]
        public int requiredLevel = 1;
    }
}
