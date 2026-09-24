using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.Collections.Generic;

public class RecipeManager : MonoBehaviour
{
    public static RecipeManager Instance;

    [Header("UI References")]
    public GameObject recipePanel;
    
    // Individual containers for manual placement
    public Transform iconContainer1;
    public Transform iconContainer2;
    public Transform iconContainer3;
    public Transform iconContainer4;
    public Transform iconContainer5;

    [Header("Background References")]
    [Tooltip("Background object for Icon 1. Will be hidden if no food exists for this slot.")]
    public GameObject background1;
    public GameObject background2;
    public GameObject background3;
    public GameObject background4;
    public GameObject background5;

    public Button closeButton;
    public TextMeshProUGUI titleText; 

    [Header("Assets")]
    public GameObject iconPrefab; 
    
    [Tooltip("Scale of the icons in the recipe panel. Increase this number to make them bigger.")]
    public float iconScale = 2.5f; // Updated default size to be bigger

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (recipePanel != null) recipePanel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
    }

    public void ShowRecipe(GameObject foodPrefab)
    {
        if (FoodManager.Instance == null) return;

        // 1. Get the chain
        Food[] chain = FoodManager.Instance.GetEvolutionChain(foodPrefab);

        if (chain != null && chain.Length > 0)
        {
            GenerateIcons(chain);
            if (recipePanel != null) recipePanel.SetActive(true);
            if (titleText != null) titleText.text = "Evolution Recipe";
        }
        else
        {
            Debug.Log("No recipe found for this food.");
        }
    }

    private void GenerateIcons(Food[] foods)
    {
        // 1. Clear all containers
        ClearContainer(iconContainer1);
        ClearContainer(iconContainer2);
        ClearContainer(iconContainer3);
        ClearContainer(iconContainer4);
        ClearContainer(iconContainer5);

        // 2. Hide all backgrounds initially
        SetBackgroundActive(background1, false);
        SetBackgroundActive(background2, false);
        SetBackgroundActive(background3, false);
        SetBackgroundActive(background4, false);
        SetBackgroundActive(background5, false);

        if (foods == null) return;

        // 3. Filter valid foods
        List<Food> validFoods = new List<Food>();
        foreach (var f in foods) if (f != null) validFoods.Add(f);

        // 4. Assign sequentially and enable backgrounds for active slots
        if (validFoods.Count > 0)
        {
            SpawnIcon(validFoods[0], iconContainer1);
            SetBackgroundActive(background1, true);
        }
        if (validFoods.Count > 1)
        {
            SpawnIcon(validFoods[1], iconContainer2);
            SetBackgroundActive(background2, true);
        }
        if (validFoods.Count > 2)
        {
            SpawnIcon(validFoods[2], iconContainer3);
            SetBackgroundActive(background3, true);
        }
        if (validFoods.Count > 3)
        {
            SpawnIcon(validFoods[3], iconContainer4);
            SetBackgroundActive(background4, true);
        }
        if (validFoods.Count > 4)
        {
            SpawnIcon(validFoods[4], iconContainer5);
            SetBackgroundActive(background5, true);
        }
    }

    private void SetBackgroundActive(GameObject bg, bool isActive)
    {
        if (bg != null) bg.SetActive(isActive);
    }

    private void ClearContainer(Transform container)
    {
        if (container == null) return;
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Transform child = container.GetChild(i);
            if (child != null) Destroy(child.gameObject);
        }
    }

    private void SpawnIcon(Food foodItem, Transform container)
    {
        if (container == null || foodItem == null) return;

        GameObject newIcon = Instantiate(iconPrefab, container);
        
        // --- FIX: Normalize the bounding box size ---
        // This ensures every icon starts at a standard 100x100 size before scaling,
        // preventing some sprites from looking huge and others tiny.
        RectTransform rt = newIcon.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(100, 100); 
        }

        // Reset transforms and apply the larger scale
        newIcon.transform.localPosition = Vector3.zero; 
        newIcon.transform.localScale = Vector3.one * iconScale; 

        // Find Sprite
        Sprite foodSprite = null;
        SpriteRenderer sr = foodItem.GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null) foodSprite = sr.sprite;
        else
        {
            Image img = foodItem.GetComponentInChildren<Image>(true);
            if (img != null) foodSprite = img.sprite;
        }

        // Assign Sprite
        Image iconImage = newIcon.GetComponent<Image>();
        if (iconImage != null && foodSprite != null)
        {
            iconImage.sprite = foodSprite;
            iconImage.color = Color.white;
            iconImage.preserveAspect = true; // This keeps the sprite from stretching inside the 100x100 box
        }
    }

    public void ClosePanel()
    {
        if (recipePanel != null) recipePanel.SetActive(false);
    }
}