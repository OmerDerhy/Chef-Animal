using UnityEngine;
using System.Collections.Generic;

public class FoodManager : MonoBehaviour
{
    public static FoodManager Instance; 

    [Header("Level Configuration")]
    [Tooltip("Set this to 1 or 2 in the Inspector for each scene")]
    public int LevelID = 1;

    [Header("Generic Food Chains (Max 7)")]
    // Reuse these arrays for different foods in different levels
    // Index 0 must be the Basic Food (the one that spawns from the bucket)
    public Food[] Chain1; 
    public Food[] Chain2;
    public Food[] Chain3;
    public Food[] Chain4;
    public Food[] Chain5;
    public Food[] Chain6;
    public Food[] Chain7;

    [Header("Specials")]
    public Food[] CookieArr; // Always Cookie Time

    [Header("Global Assets")]
    public GameObject successIconPrefab;

    [Header("Coin & Gem Popups")]
    public GameObject plus50;
    public GameObject plus100;
    public GameObject plus200;
    public GameObject plus10Gems; // <--- NEW

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public Food SetCookieFood()
    {
        // Safe check
        if (CookieArr != null && CookieArr.Length > 0) return CookieArr[0];
        return null;
    }

    // --- EVOLUTION LOGIC ---
    // Returns the NEXT evolution step (Index + 1)
    public Food Evolve(string tag)
    {

        

        // Level-Specific Logic
        if (LevelID == 1)
        {
            if (tag == "cookie1") return GetSafe(CookieArr, 1);
            if (tag == "cookie2") return GetSafe(CookieArr, 2);
            if (tag == "cookie3") return GetSafe(CookieArr, 3);
            // --- LEVEL 1: Fruits, Ice Cream, Bakery ---
            
            // Chain1 = Apple
            if (tag == "apple") return GetSafe(Chain1, 1);
            if (tag == "SlicedApple") return GetSafe(Chain1, 2);
            if (tag == "sweetapple") return GetSafe(Chain1, 3);

            // Chain2 = Orange
            if (tag == "orange") return GetSafe(Chain2, 1);
            if (tag == "orangejuice") return GetSafe(Chain2, 2);
            if (tag == "orangecupcake") return GetSafe(Chain2, 3);

            // Chain3 = Banana
            if (tag == "banana") return GetSafe(Chain3, 1);
            if (tag == "bananagroup") return GetSafe(Chain3, 2);
            if (tag == "bananapancake") return GetSafe(Chain3, 3);

            // Chain4 = IcePop
            if (tag == "icepop") return GetSafe(Chain4, 1);
            if (tag == "rainbowice") return GetSafe(Chain4, 2);
            if (tag == "icecone") return GetSafe(Chain4, 3);

            // Chain5 = IceCream
            if (tag == "icecream") return GetSafe(Chain5, 1);
            if (tag == "mixicecream") return GetSafe(Chain5, 2);
            if (tag == "icecream3") return GetSafe(Chain5, 3);

            // Chain6 = Flour/Bakery
            if (tag == "flour") return GetSafe(Chain6, 1);
            if (tag == "bread") return GetSafe(Chain6, 2);
            if (tag == "toast") return GetSafe(Chain6, 3);
            if (tag =="clubsandwich") return GetSafe(Chain6, 4);
        }
        else if (LevelID == 2)
        {
            if (tag == "cookie1") return GetSafe(CookieArr, 1);
            if (tag == "cookie2") return GetSafe(CookieArr, 2);
            if (tag == "cookie3") return GetSafe(CookieArr, 3);
            
            // --- LEVEL 2: Coffee, Croissant, Waffle, Cupcake ---

            // Chain1 = Croissant
            if (tag == "croissant") return GetSafe(Chain1, 1);
            if (tag == "chocolatecroissant") return GetSafe(Chain1, 2);
            if (tag == "pinkcroissant") return GetSafe(Chain1, 3);

            // Chain2 = Waffle
            if (tag == "waffle") return GetSafe(Chain2, 1);
            if (tag == "butterwaffle") return GetSafe(Chain2, 2);
            if (tag == "bluewaffle") return GetSafe(Chain2, 3);

            // Chain3 = Cupcake
            if (tag == "cupcake") return GetSafe(Chain3, 1);
            if (tag == "pinkcupcake") return GetSafe(Chain3, 2);
            if (tag == "chocolatecupcake") return GetSafe(Chain3, 3);

            // Chain4 = Coffee
            if (tag == "cup") return GetSafe(Chain4, 1);
            if (tag == "blackcoffee") return GetSafe(Chain4, 2);
            if (tag == "latte") return GetSafe(Chain4, 3);
            if (tag == "icecoffee") return GetSafe(Chain4, 4);

            // Chain5 = Donut
            if (tag == "donut") return GetSafe(Chain5, 1);
            if (tag == "pinkdonut") return GetSafe(Chain5, 2);
            if (tag == "donutbox") return GetSafe(Chain5, 3);

            // Chain6 = Chocolate Cake
            if (tag == "cake") return GetSafe(Chain6, 1);
            if (tag == "whitecake") return GetSafe(Chain6, 2);
            if (tag == "creamcake") return GetSafe(Chain6, 3);

            // Chain7 = Cheese cake
            if (tag == "cheesecake") return GetSafe(Chain7, 1);
            if (tag == "cheesecake2") return GetSafe(Chain7, 2);
            if (tag == "redcheesecake") return GetSafe(Chain7, 3);
        }
        else if (LevelID == 3)
        {
            if (tag == "cookie2") return GetSafe(CookieArr, 1);
            if (tag == "cookie3") return GetSafe(CookieArr, 2);
            if (tag == "cookie4") return GetSafe(CookieArr, 3);
            
            // --- LEVEL 2: Espresso, 

            // Chain1 = Espresso
            if (tag == "espresso1") return GetSafe(Chain1, 1);
            if (tag == "espresso2") return GetSafe(Chain1, 2);
            if (tag == "espresso3") return GetSafe(Chain1, 3);
            if (tag == "espresso4") return GetSafe(Chain1, 4);

            // Chain2 = Spagetti
            if (tag == "spagetti1") return GetSafe(Chain2, 1);
            if (tag == "spagetti2") return GetSafe(Chain2, 2);
            if (tag == "spagetti3") return GetSafe(Chain2, 3);

            // Chain3 = Ravioli
            if (tag == "ravioli1") return GetSafe(Chain3, 1);
            if (tag == "ravioli2") return GetSafe(Chain3, 2);
            if (tag == "ravioli3") return GetSafe(Chain3, 3);

            // Chain4 = Lasagna
            if (tag == "lasagna1") return GetSafe(Chain4, 1);
            if (tag == "lasagna2") return GetSafe(Chain4, 2);
            if (tag == "lasagna3") return GetSafe(Chain4, 3);

            // Chain5 = Calzone
            if (tag == "calzone1") return GetSafe(Chain5, 1);
            if (tag == "calzone2") return GetSafe(Chain5, 2);
            if (tag == "calzone3") return GetSafe(Chain5, 3);

            // Chain6 = Lettuce
            if (tag == "lettuce1") return GetSafe(Chain6, 1);
            if (tag == "lettuce2") return GetSafe(Chain6, 2);
            if (tag == "lettuce3") return GetSafe(Chain6, 3);
            if (tag == "lettuce4") return GetSafe(Chain6, 4);

            // Chain7 = Tomato
            if (tag == "Tomato1") return GetSafe(Chain7, 1);
            if (tag == "Tomato2") return GetSafe(Chain7, 2);
            if (tag == "Tomato3") return GetSafe(Chain7, 3);
            if (tag == "Tomato4") return GetSafe(Chain7, 4);
            

        }
        else if (LevelID == 4)
        {
            if (tag == "sushi1") return GetSafe(CookieArr, 1);
            if (tag == "sushi2") return GetSafe(CookieArr, 2);
            if (tag == "sushi3") return GetSafe(CookieArr, 3);

            if (tag == "rice1") return GetSafe(Chain1, 1);
            if (tag == "rice2") return GetSafe(Chain1, 2);
            if (tag == "rice3") return GetSafe(Chain1, 3);
            if (tag == "rice4") return GetSafe(Chain1, 4);

            if (tag == "noodles1") return GetSafe(Chain2, 1);
            if (tag == "noodles2") return GetSafe(Chain2, 2);
            if (tag == "noodles3") return GetSafe(Chain2, 3);
            if (tag == "noodles4") return GetSafe(Chain2, 4);

            if(tag == "tea1") return GetSafe(Chain3, 1);
            if(tag == "tea2") return GetSafe(Chain3, 2);
            if(tag == "tea3") return GetSafe(Chain3, 3);
            if(tag == "tea4") return GetSafe(Chain3, 4);

            if(tag == "matcha1") return GetSafe(Chain4, 1);
            if(tag == "matcha2") return GetSafe(Chain4, 2); 
            if(tag == "matcha3") return GetSafe(Chain4, 3);
            if(tag == "matcha4") return GetSafe(Chain4, 4);

            if(tag == "nigiri1") return GetSafe(Chain5, 1);
            if(tag == "nigiri2") return GetSafe(Chain5, 2);
            if(tag == "nigiri3") return GetSafe(Chain5, 3);
            if(tag == "nigiri4") return GetSafe(Chain5, 4);

            if(tag == "onigiri1") return GetSafe(Chain6, 1);
            if(tag == "onigiri2") return GetSafe(Chain6, 2);
            if(tag == "onigiri3") return GetSafe(Chain6, 3);
            if(tag == "onigiri4") return GetSafe(Chain6, 4);
        }



        return null;
    }

    // Helper to avoid IndexOutOfRange errors
    private Food GetSafe(Food[] arr, int index)
    {
        if (arr != null && index >= 0 && index < arr.Length) return arr[index];
        return null;
    }

    // --- RECIPE HELPER ---
    public Food[] GetEvolutionChain(GameObject foodPrefab)
    {
        if (foodPrefab == null) return null;
        string tag = foodPrefab.tag;

        // Check generic chains (works for any level automatically if tag matches)
        if (IsInArray(Chain1, tag)) return Chain1;
        if (IsInArray(Chain2, tag)) return Chain2;
        if (IsInArray(Chain3, tag)) return Chain3;
        if (IsInArray(Chain4, tag)) return Chain4;
        if (IsInArray(Chain5, tag)) return Chain5;
        if (IsInArray(Chain6, tag)) return Chain6;
        if (IsInArray(Chain7, tag)) return Chain7;
        
        if (IsInArray(CookieArr, tag)) return CookieArr;

        return null;
    }

    private bool IsInArray(Food[] arr, string tag)
    {
        if (arr == null) return false;
        foreach (Food f in arr)
        {
            if (f != null && f.gameObject.tag == tag) return true;
        }
        return false;
    }

    // --- SOURCE MAPPING FOR TARGET.CS ---
    // Returns a list of all Food Arrays belonging to a specific Source ID for the current level
    public List<Food[]> GetFoodsForSource(int sourceID)
    {
        List<Food[]> validChains = new List<Food[]>();

        if (LevelID == 1)
        {
            // ID 0 = Bakery (Flour)
            if (sourceID == 0) validChains.Add(Chain6);
            
            // ID 1 = Ice Cream (IcePop, IceCream)
            if (sourceID == 1) { validChains.Add(Chain4); validChains.Add(Chain5); }
            
            // ID 2 = Fruits (Apple, Orange, Banana)
            if (sourceID == 2) { validChains.Add(Chain1); validChains.Add(Chain2); validChains.Add(Chain3); }
        }
        else if (LevelID == 2)
        {
            // ID 0 = Coffee (Chain 4 reused)
            if (sourceID == 0) validChains.Add(Chain4);

            // ID 1 = Pastries (Croissant Chain 1, Waffle Chain 2)
            if (sourceID == 1) { validChains.Add(Chain1); validChains.Add(Chain2); }

            // ID 2 = Cupcake (Chain 3 Donut)
            if (sourceID == 2) {validChains.Add(Chain3); validChains.Add(Chain5); }
            // ID 3 = Cakes (Chocolate Cake Chain 6, Cheesecake Chain 7)
            if (sourceID == 3) { validChains.Add(Chain6); validChains.Add(Chain7); }
        }
        else if (LevelID == 3)
        {
            // ID 0 = Espresso (Chain 1)
            if (sourceID == 0) validChains.Add(Chain1);

            // ID 1= Spaggeti (Chain 2) and Ravioli (Chain 3)
            if (sourceID == 1) {validChains.Add(Chain2); validChains.Add(Chain3);}
            // ID 2 = Lettuce (Chain 6)
            if (sourceID == 2) { validChains.Add(Chain6); validChains.Add(Chain7); }

            // ID 3 = Lasagna (Chain 4) and Calzone (Chain 5)
            if (sourceID == 3) { validChains.Add(Chain4); validChains.Add(Chain5); }
        }
        else if( LevelID == 4)
        {
            // ID 0 = Rice (Chain 1) , Noodles (Chain 2)
            if (sourceID == 0) {validChains.Add(Chain1); validChains.Add(Chain2); }

            // ID 1 = Tea (Chain 3), Matcha (Chain 4)
            if (sourceID == 1) {validChains.Add(Chain3); validChains.Add(Chain4); }

            // ID 2 = Nigiri (Chain 5)
            if (sourceID == 2) { validChains.Add(Chain5); validChains.Add(Chain6); }
        }

        return validChains;
    }
}