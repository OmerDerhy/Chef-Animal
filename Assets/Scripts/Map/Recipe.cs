using UnityEngine;

namespace MergesChef.Map
{
    [CreateAssetMenu(fileName = "New Recipe", menuName = "Merges Chef/Recipe")]
    public class Recipe : ScriptableObject
    {
        public string recipeName;
        public int tier;
        public Sprite icon;
        // Add more recipe data here as needed
    }
}
