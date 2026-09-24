using UnityEngine;
using System.Collections.Generic;

namespace MergesChef.Map
{
    public class Map : MonoBehaviour
    {
        public static Map Instance { get; private set; }

        public const int SubGridSize = 3;

        [Header("Grid Setup")]
        public Grid layoutGrid; // Reference to Unity's Grid component

        [SerializeField] private GameMode currentMode = GameMode.PlayMode;

        // Using a Dictionary allows the grid to be infinitely large in any direction
        // (including negative coordinates) instead of being constrained to a fixed Width/Height.
        private Dictionary<Vector2Int, AreaBase> grid = new Dictionary<Vector2Int, AreaBase>();

        // Map-wide accessory storage, keyed by a GLOBAL sub-grid coordinate (area cell * SubGridSize + local 0..2 offset).
        // This is intentionally independent of whether an AreaBase exists at that cell, so a future accessory type
        // can be placed outside any Area without changing this storage layer.
        private Dictionary<Vector2Int, AccessoryBase> accessoryGrid = new Dictionary<Vector2Int, AccessoryBase>();

        private GridLinesVisualizer gridVisualizer;
        public event System.Action<bool> OnMapValidityChanged;
        public bool IsGridValid { get; private set; } = true;

        private void Awake()
        {
            Instance = this;
        }

        public static Vector2Int ToGlobalSubGrid(Vector2Int areaPos, Vector2Int localSubPos)
        {
            return areaPos * SubGridSize + localSubPos;
        }

        public AccessoryBase GetAccessoryAtGlobal(Vector2Int global)
        {
            return accessoryGrid.TryGetValue(global, out AccessoryBase acc) ? acc : null;
        }

        public bool HasAccessoryAtGlobal(Vector2Int global)
        {
            return accessoryGrid.ContainsKey(global);
        }

        public bool TryPlaceAccessoryAtGlobal(AccessoryBase accessory, Vector2Int global)
        {
            if (accessoryGrid.TryGetValue(global, out AccessoryBase occupant) && occupant != accessory)
            {
                return false;
            }
            accessoryGrid[global] = accessory;
            return true;
        }

        public void RemoveAccessoryAtGlobal(Vector2Int global)
        {
            accessoryGrid.Remove(global);
        }

        private void Start()
        {
            gridVisualizer = gameObject.AddComponent<GridLinesVisualizer>();
            SetMode(currentMode); // Properly initialize whatever mode is set in the Inspector
        }

        public void SetMode(GameMode mode)
        {
            currentMode = mode;
            
            if (gridVisualizer != null)
            {
                if (mode == GameMode.BuildMode)
                {
                    IsGridValid = ValidateMapContiguity() && ValidateAccessoryPlacements();
                    OnMapValidityChanged?.Invoke(IsGridValid);
                    gridVisualizer.ShowGrid(grid, layoutGrid, IsGridValid);
                }
                else
                {
                    gridVisualizer.HideGrid();
                }
            }

            // Enable/disable all animals
            var animals = FindObjectsOfType<Characters.Animal>(true);
            foreach (var animal in animals)
            {
                animal.gameObject.SetActive(mode == GameMode.PlayMode);
            }
        }

        public GameMode GetCurrentMode() => currentMode;

        public Vector3 GridToWorldPosition(Vector2Int gridPos)
        {
            if (layoutGrid != null)
            {
                return layoutGrid.GetCellCenterWorld(new Vector3Int(gridPos.x, gridPos.y, 0));
            }
            return new Vector3(gridPos.x, gridPos.y, 0); // Fallback
        }

        public Vector2Int WorldToGridPosition(Vector3 worldPos)
        {
            if (layoutGrid != null)
            {
                Vector3Int cellPos = layoutGrid.WorldToCell(worldPos);
                return new Vector2Int(cellPos.x, cellPos.y);
            }
            return new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y)); // Fallback
        }

        public bool TryMoveArea(AreaBase area, Vector2Int newPos)
        {


            // Check if the new position is already occupied
            if (grid.ContainsKey(newPos))
            {
                Debug.LogWarning("Position is already occupied.");
                return false;
            }

            // Remove from old position in the dictionary
            Vector2Int oldPos = area.Position;
            if (grid.ContainsKey(oldPos) && grid[oldPos] == area)
            {
                grid.Remove(oldPos);
            }

            // Set new position in the dictionary
            grid[newPos] = area;
            area.Move(newPos, GridToWorldPosition(newPos));

            // Move accessories properly
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    AccessoryBase acc = area.GetAccessoryAt(new Vector2Int(x, y));
                    if (acc != null)
                    {
                        // Update the accessory's logical position to the new area position
                        acc.Move(newPos, GetAccessoryWorldPosition(newPos, new Vector2Int(x, y)));
                    }
                }
            }

            // Update visuals for the moved area and its new/old neighbors
            UpdateMapVisuals();
            
            SaveMap();

            return true;
        }

        public AreaBase GetAreaAt(Vector2Int pos)
        {
            if (grid.TryGetValue(pos, out AreaBase area))
            {
                return area;
            }
            return null;
        }

        public Vector3 GetAccessoryWorldPosition(Vector2Int gridPos, Vector2Int subGridPos)
        {
            Vector3 center = GridToWorldPosition(gridPos);
            
            if (layoutGrid != null)
            {
                Vector3 rightVec = layoutGrid.GetCellCenterWorld(new Vector3Int(1, 0, 0)) - layoutGrid.GetCellCenterWorld(Vector3Int.zero);
                Vector3 upVec = layoutGrid.GetCellCenterWorld(new Vector3Int(0, 1, 0)) - layoutGrid.GetCellCenterWorld(Vector3Int.zero);

                // subGridPos (1,1) is center.
                float offsetX = (subGridPos.x - 1) / 3f;
                float offsetY = (subGridPos.y - 1) / 3f;

                return center + (rightVec * offsetX) + (upVec * offsetY);
            }
            
            return center + new Vector3((subGridPos.x - 1) * 0.333f, (subGridPos.y - 1) * 0.333f, 0);
        }

        public Vector2Int GetSubGridPosFromWorld(Vector3 worldPos, Vector2Int gridPos)
        {
            Vector3 center = GridToWorldPosition(gridPos);
            Vector3 diff = worldPos - center;
            
            if (layoutGrid != null)
            {
                Vector3 rightVec = layoutGrid.GetCellCenterWorld(new Vector3Int(1, 0, 0)) - layoutGrid.GetCellCenterWorld(Vector3Int.zero);
                Vector3 upVec = layoutGrid.GetCellCenterWorld(new Vector3Int(0, 1, 0)) - layoutGrid.GetCellCenterWorld(Vector3Int.zero);
                
                // Solve for projection multipliers u and v: diff = rightVec * u + upVec * v
                float det = rightVec.x * upVec.y - rightVec.y * upVec.x;
                if (Mathf.Abs(det) > 0.0001f)
                {
                    float u = (diff.x * upVec.y - diff.y * upVec.x) / det;
                    float v = (rightVec.x * diff.y - rightVec.y * diff.x) / det;

                    // Convert u, v back to subGridPos indices
                    int x = Mathf.RoundToInt(u * 3f + 1f);
                    int y = Mathf.RoundToInt(v * 3f + 1f);

                    return new Vector2Int(Mathf.Clamp(x, 0, 2), Mathf.Clamp(y, 0, 2));
                }
            }
            
            int fallbackX = Mathf.RoundToInt(diff.x * 3f + 1f);
            int fallbackY = Mathf.RoundToInt(diff.y * 3f + 1f);
            return new Vector2Int(Mathf.Clamp(fallbackX, 0, 2), Mathf.Clamp(fallbackY, 0, 2));
        }

        public bool TryMoveAccessory(AccessoryBase accessory, Vector2Int newPos, Vector2Int subGridPos)
        {
            if (accessory == null)
            {
                Debug.LogError("TryMoveAccessory failed: accessory is null. If you placed this prefab manually in the scene, it won't work because it lacks a data model. Please let the MapInitializer spawn it!");
                return false;
            }

            // targetArea may legitimately be null (a map cell with no Area placed on it yet).
            // Whether that's acceptable is up to the accessory's own IsPlacementValid override.
            AreaBase targetArea = GetAreaAt(newPos);

            // Check accessory-specific placement rules
            if (!accessory.IsPlacementValid(targetArea, subGridPos))
            {
                Debug.LogWarning($"Placement rejected by {accessory.GetType().Name} specific rules.");
                return false;
            }

            Vector2Int globalNew = ToGlobalSubGrid(newPos, subGridPos);
            AccessoryBase occupant = GetAccessoryAtGlobal(globalNew);
            if (occupant != null && occupant != accessory)
            {
                Debug.LogWarning("SubGrid position is already occupied.");
                return false;
            }

            // Remove from its old global slot (and notify the old area, if any)
            AreaBase oldArea = GetAreaAt(accessory.Position);
            Vector2Int globalOld = ToGlobalSubGrid(accessory.Position, accessory.SubGridPosition);
            RemoveAccessoryAtGlobal(globalOld);

            TryPlaceAccessoryAtGlobal(accessory, globalNew);
            accessory.SetSubGridPosition(subGridPos);
            accessory.Move(newPos, GetAccessoryWorldPosition(newPos, subGridPos));
            accessory.UpdateVisualState(targetArea);

            // We should also update visuals of other accessories on the old and new areas,
            // in case their state depended on this accessory moving.
            if (oldArea != null && oldArea != targetArea) oldArea.UpdateAccessoryVisualStates();
            if (targetArea != null) targetArea.UpdateAccessoryVisualStates();

            // Moving a Table (or anything else) can orphan a neighboring Chair that relied
            // on it. Refresh grid validity so the Confirm button reflects that immediately.
            UpdateMapVisuals();

            SaveMap();

            return true;
        }

        public AreaBase[] GetNeighbors(Vector2Int pos)
        {
            List<AreaBase> neighbors = new List<AreaBase>();

            Vector2Int[] directions = {
                Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
            };

            foreach (var dir in directions)
            {
                Vector2Int neighborPos = pos + dir;
                if (grid.TryGetValue(neighborPos, out AreaBase neighbor))
                {
                    neighbors.Add(neighbor);
                }
            }

            return neighbors.ToArray();
        }

        public IEnumerable<Vector2Int> GetAllAreaPositions() => grid.Keys;

        public Vector2Int GetClosestAreaPosition(Vector3 worldPos)
        {
            if (grid.Count == 0) return Vector2Int.zero;

            Vector2Int closest = Vector2Int.zero;
            float minDistance = float.MaxValue;
            
            foreach (var kvp in grid)
            {
                Vector3 areaWorldPos = GridToWorldPosition(kvp.Key);
                float dist = Vector2.Distance(worldPos, areaWorldPos);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = kvp.Key;
                }
            }
            return closest;
        }

        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int target)
        {
            if (!grid.ContainsKey(start)) start = GetClosestAreaPosition(GridToWorldPosition(start));
            if (!grid.ContainsKey(target)) return null;

            List<Vector2Int> openSet = new List<Vector2Int> { start };
            HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();
            
            Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            Dictionary<Vector2Int, float> gScore = new Dictionary<Vector2Int, float>();
            Dictionary<Vector2Int, float> fScore = new Dictionary<Vector2Int, float>();
            
            gScore[start] = 0;
            fScore[start] = Vector2.Distance(start, target);

            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            while (openSet.Count > 0)
            {
                // Find node with lowest fScore
                Vector2Int current = openSet[0];
                float lowestF = fScore.ContainsKey(current) ? fScore[current] : float.MaxValue;
                
                for (int i = 1; i < openSet.Count; i++)
                {
                    Vector2Int node = openSet[i];
                    float f = fScore.ContainsKey(node) ? fScore[node] : float.MaxValue;
                    if (f < lowestF)
                    {
                        lowestF = f;
                        current = node;
                    }
                }

                if (current == target)
                {
                    return ReconstructPath(cameFrom, current);
                }

                openSet.Remove(current);
                closedSet.Add(current);

                foreach (var dir in directions)
                {
                    Vector2Int neighbor = current + dir;
                    
                    if (!grid.ContainsKey(neighbor)) continue; // Must be walkable
                    if (closedSet.Contains(neighbor)) continue;

                    float tentative_gScore = (gScore.ContainsKey(current) ? gScore[current] : float.MaxValue) + 1;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                    else if (tentative_gScore >= (gScore.ContainsKey(neighbor) ? gScore[neighbor] : float.MaxValue))
                        continue;

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentative_gScore;
                    fScore[neighbor] = tentative_gScore + Vector2.Distance(neighbor, target);
                }
            }
            
            return null; // No path found
        }

        private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            List<Vector2Int> path = new List<Vector2Int> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Add(current);
            }
            path.Reverse();
            return path;
        }

        public Vector2Int WorldToGlobalSubGrid(Vector3 worldPos)
        {
            Vector2Int areaPos = WorldToGridPosition(worldPos);
            Vector2Int subGridPos = GetSubGridPosFromWorld(worldPos, areaPos);
            return new Vector2Int(areaPos.x * 3 + subGridPos.x, areaPos.y * 3 + subGridPos.y);
        }

        public Vector3 GlobalSubGridToWorld(Vector2Int globalSubGrid)
        {
            int areaX = Mathf.FloorToInt(globalSubGrid.x / 3f);
            int areaY = Mathf.FloorToInt(globalSubGrid.y / 3f);
            int subX = globalSubGrid.x - areaX * 3;
            int subY = globalSubGrid.y - areaY * 3;
            return GetAccessoryWorldPosition(new Vector2Int(areaX, areaY), new Vector2Int(subX, subY));
        }

        public bool IsSubGridWalkable(Vector2Int globalSubGrid)
        {
            int areaX = Mathf.FloorToInt(globalSubGrid.x / 3f);
            int areaY = Mathf.FloorToInt(globalSubGrid.y / 3f);
            Vector2Int areaPos = new Vector2Int(areaX, areaY);
            
            AreaBase area = GetAreaAt(areaPos);
            if (area == null) return false;
            
            int subX = globalSubGrid.x - areaX * 3;
            int subY = globalSubGrid.y - areaY * 3;
            
            AccessoryBase acc = area.GetAccessoryAt(new Vector2Int(subX, subY));
            if (acc != null)
            {
                if (acc is Chair) return true;
                return false;
            }
            return true;
        }

        public List<Vector3> FindPathWorld(Vector3 startWorld, Vector3 endWorld)
        {
            Vector2Int start = WorldToGlobalSubGrid(startWorld);
            Vector2Int target = WorldToGlobalSubGrid(endWorld);

            if (!IsSubGridWalkable(start))
            {
                Vector2Int bestStart = start;
                float bestDist = float.MaxValue;
                foreach(var kvp in grid)
                {
                    Vector2Int areaPos = kvp.Key;
                    for (int sx = 0; sx < 3; sx++)
                    {
                        for (int sy = 0; sy < 3; sy++)
                        {
                            Vector2Int subPos = new Vector2Int(areaPos.x * 3 + sx, areaPos.y * 3 + sy);
                            if (IsSubGridWalkable(subPos))
                            {
                                float dist = Vector2.Distance(startWorld, GlobalSubGridToWorld(subPos));
                                if (dist < bestDist)
                                {
                                    bestDist = dist;
                                    bestStart = subPos;
                                }
                            }
                        }
                    }
                }
                start = bestStart;
            }

            if (!IsSubGridWalkable(target))
            {
                Vector2Int[] searchDirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
                                            new Vector2Int(1,1), new Vector2Int(1,-1), new Vector2Int(-1,1), new Vector2Int(-1,-1) };
                Vector2Int bestValid = target;
                float bestDist = float.MaxValue;
                foreach(var d in searchDirs)
                {
                    Vector2Int neighbor = target + d;
                    if (IsSubGridWalkable(neighbor))
                    {
                        float dist = Vector2.Distance(start, neighbor);
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestValid = neighbor;
                        }
                    }
                }
                target = bestValid;
            }

            List<Vector2Int> openSet = new List<Vector2Int> { start };
            HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();
            
            Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            Dictionary<Vector2Int, float> gScore = new Dictionary<Vector2Int, float>();
            Dictionary<Vector2Int, float> fScore = new Dictionary<Vector2Int, float>();
            
            gScore[start] = 0;
            fScore[start] = Vector2.Distance(start, target);

            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
                                        new Vector2Int(1,1), new Vector2Int(1,-1), new Vector2Int(-1,1), new Vector2Int(-1,-1) };

            Vector2Int closestNodeToTarget = start;
            float closestDistToTarget = Vector2.Distance(start, target);

            while (openSet.Count > 0)
            {
                Vector2Int current = openSet[0];
                float lowestF = fScore.ContainsKey(current) ? fScore[current] : float.MaxValue;
                
                for (int i = 1; i < openSet.Count; i++)
                {
                    Vector2Int node = openSet[i];
                    float f = fScore.ContainsKey(node) ? fScore[node] : float.MaxValue;
                    if (f < lowestF)
                    {
                        lowestF = f;
                        current = node;
                    }
                }

                if (current == target)
                {
                    return ConvertGlobalSubGridPathToWorld(ReconstructPath(cameFrom, current));
                }

                openSet.Remove(current);
                closedSet.Add(current);

                float distToTarget = Vector2.Distance(current, target);
                if (distToTarget < closestDistToTarget)
                {
                    closestDistToTarget = distToTarget;
                    closestNodeToTarget = current;
                }

                foreach (var dir in directions)
                {
                    Vector2Int neighbor = current + dir;
                    
                    if (!IsSubGridWalkable(neighbor)) continue;
                    if (closedSet.Contains(neighbor)) continue;

                    float tentative_gScore = (gScore.ContainsKey(current) ? gScore[current] : float.MaxValue) + 1;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                    else if (tentative_gScore >= (gScore.ContainsKey(neighbor) ? gScore[neighbor] : float.MaxValue))
                        continue;

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentative_gScore;
                    fScore[neighbor] = tentative_gScore + Vector2.Distance(neighbor, target);
                }
                
                if (closedSet.Count > 5000) break;
            }
            
            return ConvertGlobalSubGridPathToWorld(ReconstructPath(cameFrom, closestNodeToTarget));
        }

        private List<Vector3> ConvertGlobalSubGridPathToWorld(List<Vector2Int> path)
        {
            if (path == null) return null;
            List<Vector3> worldPath = new List<Vector3>();
            foreach(var p in path)
            {
                worldPath.Add(GlobalSubGridToWorld(p));
            }
            return worldPath;
        }

        public void SaveMapState()
        {
            Debug.Log("Saving Map State...");
        }

        public void AddAreaToGrid(AreaBase area, Vector2Int pos)
        {
             if (!grid.ContainsKey(pos))
             {
                 grid[pos] = area;
                 area.Move(pos, GridToWorldPosition(pos));
                 UpdateMapVisuals();
                 
                 SaveMap();
             }
        }

        public void UpdateMapVisuals()
        {
            foreach (var area in grid.Values)
            {
                Vector2Int pos = area.Position;
                bool hasTopNeighbor = grid.ContainsKey(pos + Vector2Int.up);
                bool hasRightNeighbor = grid.ContainsKey(pos + Vector2Int.right);
                
                WallState state = WallState.Regular;

                if (hasRightNeighbor && hasTopNeighbor) state = WallState.Regular;
                else if (!hasRightNeighbor && !hasTopNeighbor) state = WallState.Corner;
                else if (hasRightNeighbor) state = WallState.LeftWall;
                else if (hasTopNeighbor) state = WallState.RightWall;

                area.UpdateWallState(state);
            }

            if (currentMode == GameMode.BuildMode && gridVisualizer != null)
            {
                IsGridValid = ValidateMapContiguity() && ValidateAccessoryPlacements();
                OnMapValidityChanged?.Invoke(IsGridValid);
                gridVisualizer.ShowGrid(grid, layoutGrid, IsGridValid);
            }
        }

        /// <summary>
        /// Re-checks every placed accessory against its own IsPlacementValid rule using the
        /// CURRENT grid state. This is what catches a Chair left without an adjacent Table -
        /// e.g. after the Area (or a neighboring Area) holding that Table was moved away.
        /// </summary>
        public bool ValidateAccessoryPlacements()
        {
            foreach (var area in grid.Values)
            {
                for (int x = 0; x < SubGridSize; x++)
                {
                    for (int y = 0; y < SubGridSize; y++)
                    {
                        Vector2Int subGridPos = new Vector2Int(x, y);
                        AccessoryBase acc = area.GetAccessoryAt(subGridPos);
                        if (acc == null) continue;

                        if (!acc.IsPlacementValid(area, subGridPos))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public bool ValidateMapContiguity()
        {
            if (grid.Count <= 1) return true;

            System.Collections.Generic.HashSet<Vector2Int> visited = new System.Collections.Generic.HashSet<Vector2Int>();
            System.Collections.Generic.Queue<Vector2Int> queue = new System.Collections.Generic.Queue<Vector2Int>();
            
            // Get first key
            Vector2Int startNode = Vector2Int.zero;
            foreach (var key in grid.Keys)
            {
                startNode = key;
                break;
            }

            queue.Enqueue(startNode);
            visited.Add(startNode);

            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            while (queue.Count > 0)
            {
                Vector2Int curr = queue.Dequeue();
                foreach (var dir in directions)
                {
                    Vector2Int neighbor = curr + dir;
                    if (grid.ContainsKey(neighbor) && !visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return visited.Count == grid.Count;
        }

        public void SpawnAndPlacePurchasedItem(MergesChef.UI.MarketItemData item)
        {
            if (item.prefabToPlace == null) return;
            
            GameObject visualInstance = Instantiate(item.prefabToPlace);
            
            AreaVisual areaVisual = visualInstance.GetComponent<AreaVisual>();
            if (areaVisual != null)
            {
                // Defaulting new purchased areas to Restaurant for now
                RestaurantFloor logic = new RestaurantFloor(Vector2Int.zero, 1, 200, 10, 1.0f, AreaType.Restaurant);
                areaVisual.LinkedArea = logic;
                logic.BindVisual(visualInstance);
                
                // Find empty spot adjacent to existing grid
                bool placed = false;
                Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                foreach (var kvp in grid)
                {
                    foreach (var dir in dirs)
                    {
                        Vector2Int candidate = kvp.Key + dir;
                        if (!grid.ContainsKey(candidate))
                        {
                            logic.Move(candidate, GridToWorldPosition(candidate));
                            grid[candidate] = logic;
                            UpdateMapVisuals();
                            placed = true;
                            break;
                        }
                    }
                    if (placed) break;
                }

                if (!placed) // Fallback if grid is empty
                {
                    logic.Move(Vector2Int.zero, GridToWorldPosition(Vector2Int.zero));
                    grid[Vector2Int.zero] = logic;
                    UpdateMapVisuals();
                }
                SaveMap();
                return;
            }

            AccessoryVisual accVisual = visualInstance.GetComponent<AccessoryVisual>();
            if (accVisual != null)
            {
                AccessoryBase logic = null;
                if (item.itemName.Contains("Table")) logic = new Table(item.itemName);
                else if (item.itemName.Contains("Chair")) logic = new Chair(item.itemName);
                else if (item.itemName.Contains("Oven")) logic = new Oven(item.itemName);
                else if (item.itemName.Contains("Ice Cream")) logic = new IceCreamMachine(item.itemName);
                else logic = new Table(item.itemName); // Fallback
                
                accVisual.LinkedAccessory = logic;
                logic.BindVisual(visualInstance);
                
                // Find available subgrid spot
                bool placed = false;
                foreach (var kvp in grid)
                {
                    AreaBase area = kvp.Value;
                    for (int x = 0; x < 3; x++)
                    {
                        for (int y = 0; y < 3; y++)
                        {
                            Vector2Int subGridPos = new Vector2Int(x, y);
                            if (area.TryAddAccessory(logic, subGridPos))
                            {
                                logic.Move(area.Position, GetAccessoryWorldPosition(area.Position, subGridPos));
                                logic.UpdateVisualState(area);
                                area.UpdateAccessoryVisualStates();
                                placed = true;
                                break;
                            }
                        }
                        if (placed) break;
                    }
                    if (placed) break;
                }

                if (!placed)
                {
                    Debug.LogWarning("No space available for this accessory!");
                    Destroy(visualInstance);
                }
                else
                {
                    SaveMap();
                }
                return;
            }
        }

        public int GetTotalChairs()
        {
            int chairCount = 0;
            foreach (var kvp in grid)
            {
                AreaBase area = kvp.Value;
                for (int x = 0; x < 3; x++)
                {
                    for (int y = 0; y < 3; y++)
                    {
                        AccessoryBase acc = area.GetAccessoryAt(new Vector2Int(x, y));
                        if (acc != null && acc is Chair)
                        {
                            chairCount++;
                        }
                    }
                }
            }
            return chairCount;
        }

        public string GetMapJson()
        {
            MapSaveData data = new MapSaveData();
            foreach (var areaKV in grid)
            {
                SavedArea sa = new SavedArea();
                sa.type = areaKV.Value.GetType().Name;
                sa.x = areaKV.Key.x;
                sa.y = areaKV.Key.y;
                sa.level = areaKV.Value.Level; 
                sa.areaTypeEnum = (int)areaKV.Value.Type;
                
                for (int x = 0; x < 3; x++)
                {
                    for (int y = 0; y < 3; y++)
                    {
                        AccessoryBase acc = areaKV.Value.GetAccessoryAt(new Vector2Int(x, y));
                        if (acc != null)
                        {
                            SavedAccessory sacc = new SavedAccessory();
                            sacc.type = acc.GetType().Name;
                            sacc.itemName = acc.AccessoryName;
                            sacc.x = x;
                            sacc.y = y;
                            sacc.level = acc.Level;
                            sacc.isFlipped = acc.IsFlipped;
                            sa.accessories.Add(sacc);
                        }
                    }
                }
                data.areas.Add(sa);
            }
            
            return JsonUtility.ToJson(data);
        }

        public void SaveMap()
        {
            string json = GetMapJson();
            if (PlayerData.Instance != null)
            {
                PlayerData.Instance.SaveMapData(json);
            }
        }

        public void ClearMap()
        {
            grid.Clear();
            var oldAreas = FindObjectsOfType<AreaVisual>();
            foreach (var a in oldAreas) Destroy(a.gameObject);
            
            var oldAccessories = FindObjectsOfType<AccessoryVisual>();
            foreach (var acc in oldAccessories) Destroy(acc.gameObject);
        }

        private void OnApplicationQuit()
        {
            SaveMap();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveMap();
            }
        }
    }
}
