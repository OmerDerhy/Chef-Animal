using UnityEngine;
using MergesChef.Map;

namespace MergesChef.Map
{
    public class MapInitializer : MonoBehaviour
    {
        public Map mapManager; // Drag your MapManager GameObject here
        
        [Header("Visual Prefabs")]
        public GameObject restaurantPrefab;
        public GameObject kitchenPrefab;
        public GameObject tablePrefab;
        public GameObject chairPrefab;
        public GameObject ovenPrefab;
        public GameObject iceCreamMachinePrefab;

        private void Start()
        {
            // Clean up any manually placed testing objects in the scene so they don't duplicate the saved ones!
            var oldAreas = FindObjectsOfType<AreaVisual>();
            foreach (var a in oldAreas) Destroy(a.gameObject);
            
            var oldAccessories = FindObjectsOfType<AccessoryVisual>();
            foreach (var acc in oldAccessories) Destroy(acc.gameObject);

            string mapJson = "";
            if (PlayerData.Instance != null)
            {
                mapJson = PlayerData.Instance.LoadMapData();
            }

            if (!string.IsNullOrEmpty(mapJson))
            {
                LoadMapFromJson(mapJson);
            }
            else
            {
                // Create default map
                CreateRestaurantArea(new Vector2Int(2, 2), AreaType.Restaurant);
                CreateRestaurantArea(new Vector2Int(3, 2), AreaType.Restaurant);
                CreateRestaurantArea(new Vector2Int(2, 3), AreaType.Restaurant);
                CreateRestaurantArea(new Vector2Int(3, 3), AreaType.Kitchen);

                SpawnTableAccessory(new Vector2Int(2, 2));
                SpawnChairAccessory(new Vector2Int(2, 2));
                SpawnOvenAccessory(new Vector2Int(3, 3));
                SpawnIceCreamMachineAccessory(new Vector2Int(3, 3));
                
                // Save it so it's not empty next time
                if (mapManager != null) mapManager.SaveMap();
            }
        }

        public void LoadMapFromJson(string json)
        {
            MapSaveData data = JsonUtility.FromJson<MapSaveData>(json);
            if (data == null || data.areas == null) return;

            // PASS 1: Spawn all floor areas first so the grid is fully built
            foreach (var savedArea in data.areas)
            {
                if (savedArea.type == "RestaurantFloor")
                {
                    CreateRestaurantAreaForLoad(new Vector2Int(savedArea.x, savedArea.y), savedArea.level, (AreaType)savedArea.areaTypeEnum);
                }
            }

            // PASS 2: Now that all floors exist, spawn all accessories.
            // We bypass placement validation here because we trust the saved data.
            foreach (var savedArea in data.areas)
            {
                foreach (var savedAcc in savedArea.accessories)
                {
                    Vector2Int gridPos = new Vector2Int(savedArea.x, savedArea.y);
                    Vector2Int subGridPos = new Vector2Int(savedAcc.x, savedAcc.y);
                    
                    GameObject prefab = null;
                    if (UI.MarketUIManager.Instance != null && !string.IsNullOrEmpty(savedAcc.itemName))
                    {
                        var marketItemData = UI.MarketUIManager.Instance.GetItemDataByName(savedAcc.itemName);
                        if (marketItemData != null && marketItemData.Value.prefabToPlace != null)
                        {
                            prefab = marketItemData.Value.prefabToPlace;
                        }
                    }

                    if (prefab == null)
                    {
                        if (savedAcc.type == "Table") prefab = tablePrefab;
                        else if (savedAcc.type == "Chair") prefab = chairPrefab;
                        else if (savedAcc.type == "Oven") prefab = ovenPrefab;
                        else if (savedAcc.type == "IceCreamMachine") prefab = iceCreamMachinePrefab;
                    }

                    string actualName = !string.IsNullOrEmpty(savedAcc.itemName) ? savedAcc.itemName : savedAcc.type;

                    if (savedAcc.type == "Table") SpawnAccessoryForLoad(new Table(actualName), prefab, gridPos, subGridPos, savedAcc.level, savedAcc.isFlipped);
                    else if (savedAcc.type == "Chair") SpawnAccessoryForLoad(new Chair(actualName), prefab, gridPos, subGridPos, savedAcc.level, savedAcc.isFlipped);
                    else if (savedAcc.type == "Oven") SpawnAccessoryForLoad(new Oven(actualName), prefab, gridPos, subGridPos, savedAcc.level, savedAcc.isFlipped);
                    else if (savedAcc.type == "IceCreamMachine") SpawnAccessoryForLoad(new IceCreamMachine(actualName), prefab, gridPos, subGridPos, savedAcc.level, savedAcc.isFlipped);
                }
            }
        }

        // Used only during loading — skips placement validation since the data was already validated when saved.
        private void SpawnAccessoryForLoad(AccessoryBase logic, GameObject prefab, Vector2Int gridPos, Vector2Int subGridPos, int savedLevel, bool savedFlipped)
        {
            if (prefab == null) return;

            AreaBase targetArea = mapManager.GetAreaAt(gridPos);
            if (targetArea == null)
            {
                Debug.LogWarning($"Load: No area at {gridPos} for accessory {logic.GetType().Name}. Skipping.");
                return;
            }

            if (targetArea.HasAccessoryAt(subGridPos))
            {
                Debug.LogWarning($"Load: Slot {subGridPos} at {gridPos} already occupied. Skipping duplicate.");
                return;
            }

            GameObject visualInstance = Instantiate(prefab);
            logic.InitializeFromSave(savedLevel);
            logic.SetFlipped(savedFlipped);
            logic.BindVisual(visualInstance);
            // Bypass validation: directly place into the area and move it into position
            targetArea.TryAddAccessory(logic, subGridPos);
            logic.Move(gridPos, mapManager.GetAccessoryWorldPosition(gridPos, subGridPos));
            targetArea.UpdateAccessoryVisualStates();
        }

        private void SpawnAccessoryAt(AccessoryBase logic, GameObject prefab, Vector2Int gridPos, Vector2Int subGridPos)
        {
            if (prefab == null) return;
            
            AreaBase targetArea = mapManager.GetAreaAt(gridPos);
            if (targetArea != null && targetArea.HasAccessoryAt(subGridPos))
            {
                Debug.LogWarning($"Slot {subGridPos} at {gridPos} is already occupied. Skipping duplicate.");
                return;
            }

            GameObject visualInstance = Instantiate(prefab);
            logic.BindVisual(visualInstance);
            bool success = mapManager.TryMoveAccessory(logic, gridPos, subGridPos);
            
            if (!success)
            {
                Destroy(visualInstance);
            }
        }

        private void SpawnTableAccessory(Vector2Int gridPosition)
        {
            if (tablePrefab == null) return;
            Table tableLogic = new Table("Starter Table");
            SpawnAccessoryAt(tableLogic, tablePrefab, gridPosition, new Vector2Int(1, 1));
        }

        private void SpawnChairAccessory(Vector2Int gridPosition)
        {
            if (chairPrefab == null) return;
            Chair chairLogic = new Chair("Starter Chair");
            SpawnAccessoryAt(chairLogic, chairPrefab, gridPosition, new Vector2Int(0, 1));
        }

        private void SpawnOvenAccessory(Vector2Int gridPosition)
        {
            if (ovenPrefab == null) return;
            Oven ovenLogic = new Oven("Starter Oven");
            SpawnAccessoryAt(ovenLogic, ovenPrefab, gridPosition, new Vector2Int(1, 1)); // center
        }

        private void SpawnIceCreamMachineAccessory(Vector2Int gridPosition)
        {
            if (iceCreamMachinePrefab == null) return;
            IceCreamMachine iceCreamLogic = new IceCreamMachine("Starter Ice Cream Machine");
            SpawnAccessoryAt(iceCreamLogic, iceCreamMachinePrefab, gridPosition, new Vector2Int(1, 1)); // center
        }


        private void CreateRestaurantArea(Vector2Int gridPosition, AreaType areaType)
        {
            GameObject prefabToUse = (areaType == AreaType.Kitchen && kitchenPrefab != null) ? kitchenPrefab : restaurantPrefab;
            if (prefabToUse == null)
            {
                Debug.LogWarning("Required Area Prefab is not assigned in MapInitializer.");
                return;
            }

            if (mapManager.GetAreaAt(gridPosition) != null)
            {
                Debug.LogWarning($"Area at {gridPosition} already exists. Skipping duplicate area.");
                return;
            }

            RestaurantFloor restaurantLogic = new RestaurantFloor(gridPosition, 1, 200, 10, 1.0f, areaType);
            GameObject visualInstance = Instantiate(prefabToUse);
            restaurantLogic.BindVisual(visualInstance);

            mapManager.AddAreaToGrid(restaurantLogic, gridPosition);
        }

        private void CreateRestaurantAreaForLoad(Vector2Int gridPosition, int savedLevel, AreaType areaType)
        {
            GameObject prefabToUse = (areaType == AreaType.Kitchen && kitchenPrefab != null) ? kitchenPrefab : restaurantPrefab;
            if (prefabToUse == null)
            {
                Debug.LogWarning("Required Area Prefab is not assigned in MapInitializer.");
                return;
            }

            if (mapManager.GetAreaAt(gridPosition) != null)
            {
                Debug.LogWarning($"Area at {gridPosition} already exists. Skipping duplicate area.");
                return;
            }

            RestaurantFloor restaurantLogic = new RestaurantFloor(gridPosition, 1, 200, 10, 1.0f, areaType);
            restaurantLogic.InitializeFromSave(savedLevel);
            
            GameObject visualInstance = Instantiate(prefabToUse);
            restaurantLogic.BindVisual(visualInstance);

            mapManager.AddAreaToGrid(restaurantLogic, gridPosition);
        }
    }
}
