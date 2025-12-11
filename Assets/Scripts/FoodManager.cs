using UnityEngine;

public class FoodManager : MonoBehaviour
{
    public Food[] AppleArr;
    public Food[] OrangeArr;
    public Food[] BananaArr;
    public Food[] IcePopArr;
    public Food[] IceCreamArr;
    public Food[] FlourArr;
    public Food[] CookieArr;

    [Header("Global Assets")]
    public GameObject successIconPrefab;

    [Header("Coin Popups")]
    public GameObject plus50;
    public GameObject plus100;
    public GameObject plus200;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public Food SetCookieFood()
    {
        return CookieArr[0];
    }
    public Food Evolve(string tag)
    {
        if (tag == "apple")
        {
            return AppleArr[0];
        }
        if(tag == "SlicedApple")
        {
            return AppleArr[1];
        }
        if(tag == "sweetapple")
        {
            return AppleArr[2];
        }
        if(tag == "applepie")
        {
            return AppleArr[3];
        }
        if(tag == "orange")
        {
            return OrangeArr[0];
        }
        if(tag == "orangejuice")
        {
            return OrangeArr[1];
        }
        if(tag == "orangeicecream")
        {
            return OrangeArr[2];
        }
        if(tag == "orangecupcake")
        {
            return OrangeArr[3];
        }
        if(tag == "banana")
        {
            return BananaArr[0];
        }
        if(tag == "bananagroup")
        {
            return BananaArr[1];
        }
        if(tag == "bananapancake")
        {
            return BananaArr[2];
        }
        if(tag == "icepop")
        {
            return IcePopArr[0];
        }
        if(tag == "rainbowice")
        {
            return IcePopArr[1];
        }
        if(tag == "icecone")
        {
            return IcePopArr[2];
        }
        if(tag == "icecream")
        {
            return IceCreamArr[0];
        }
        if(tag == "mixicecream")
        {
            return IceCreamArr[1];
        }
        if(tag == "icecream3")
        {
            return IceCreamArr[2];
        }
        if(tag == "flour")
        {
            return FlourArr[0];
        }
        if(tag == "bread")
        {
            return FlourArr[1];
        }
        if(tag == "toast")
        {
            return FlourArr[2];
        }
        if(tag =="clubsandwich")
        {
            return FlourArr[3];
        }
        if(tag == "cookie")
        {
            return CookieArr[1];
        }
        if(tag == "chocolatestack")
        {
            return CookieArr[2];
        }
        if(tag == "smores")
        {
            return CookieArr[3];
        }
        if(tag == "cup")
        {
            return FlourArr[0];
        }
        if(tag == "blackcoffee")
        {
            return FlourArr[1];
        }
        if(tag == "latte")
        {
            return FlourArr[2];
        }
        if(tag == "icecoffee")
        {
            return FlourArr[3];
        }

        return null;
    }
}