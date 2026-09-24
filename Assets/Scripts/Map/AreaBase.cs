using UnityEngine;
using System;

namespace MergesChef.Map
{
    public enum AreaType
    {
        Restaurant,
        Kitchen
    }

    [Serializable]
    public abstract class AreaBase : IPlaceable
    {
        [SerializeField] protected Vector2Int position;
        [SerializeField] protected int level;
        [SerializeField] protected int costToUpgrade;
        [SerializeField] protected AreaType areaType = AreaType.Restaurant;

        public int Level => level;
        public int CostToUpgrade => costToUpgrade;
        public AreaType Type => areaType;

        // Accessory storage lives on Map now (a single map-wide grid), keyed by a global sub-grid
        // coordinate. These methods keep their original local (0..2) signature and just translate
        // to/from that global coordinate, so every existing caller keeps working unchanged.
        public bool TryAddAccessory(AccessoryBase accessory, Vector2Int subGridPos)
        {
            if (subGridPos.x < 0 || subGridPos.x > 2 || subGridPos.y < 0 || subGridPos.y > 2) return false;
            if (Map.Instance == null) return false;

            Vector2Int global = Map.ToGlobalSubGrid(position, subGridPos);
            if (Map.Instance.TryPlaceAccessoryAtGlobal(accessory, global))
            {
                accessory.SetSubGridPosition(subGridPos);
                return true;
            }
            return false;
        }

        public void RemoveAccessory(AccessoryBase accessory)
        {
            if (Map.Instance == null) return;

            Vector2Int pos = accessory.SubGridPosition;
            if (pos.x >= 0 && pos.x <= 2 && pos.y >= 0 && pos.y <= 2)
            {
                Vector2Int global = Map.ToGlobalSubGrid(position, pos);
                if (Map.Instance.GetAccessoryAtGlobal(global) == accessory)
                {
                    Map.Instance.RemoveAccessoryAtGlobal(global);
                }
            }
        }

        public bool HasAccessoryAt(Vector2Int subGridPos)
        {
            if (subGridPos.x < 0 || subGridPos.x > 2 || subGridPos.y < 0 || subGridPos.y > 2) return false;
            if (Map.Instance == null) return false;
            return Map.Instance.HasAccessoryAtGlobal(Map.ToGlobalSubGrid(position, subGridPos));
        }

        public void UpdateAccessoryVisualStates()
        {
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    AccessoryBase acc = GetAccessoryAt(new Vector2Int(x, y));
                    if (acc != null)
                    {
                        acc.UpdateVisualState(this);
                    }
                }
            }
        }

        public AccessoryBase GetAccessoryAt(Vector2Int subGridPos)
        {
            if (subGridPos.x < 0 || subGridPos.x > 2 || subGridPos.y < 0 || subGridPos.y > 2) return null;
            if (Map.Instance == null) return null;
            return Map.Instance.GetAccessoryAtGlobal(Map.ToGlobalSubGrid(position, subGridPos));
        }

        // GameObject is not easily serializable for cross-session saves (like JSON),
        // so we exclude it. We can add a prefab ID or rely on the game manager to bind this.
        [NonSerialized] protected GameObject visualModel;

        public Vector2Int Position => position;

        public AreaBase(Vector2Int initialPosition, int startLevel, int startCost, AreaType type)
        {
            position = initialPosition;
            level = startLevel;
            costToUpgrade = startCost;
            areaType = type;
        }

        public void InitializeFromSave(int savedLevel)
        {
            if (savedLevel <= 1) return;
            level = savedLevel;
            costToUpgrade = 200; // Base cost for areas
            for (int i = 1; i < level; i++)
            {
                costToUpgrade = Mathf.RoundToInt(costToUpgrade * 1.5f);
            }
        }

        public virtual void BindVisual(GameObject visual)
        {
            visualModel = visual;

            // Automatically add the bridge script so the InputHandler knows what logic this visual belongs to
            AreaVisual areaVisual = visual.GetComponent<AreaVisual>();
            if (areaVisual == null)
            {
                areaVisual = visual.AddComponent<AreaVisual>();
            }
            areaVisual.LinkedArea = this;
        }

        public virtual void Move(Vector2Int newPosition, Vector3 worldPosition)
        {
            position = newPosition;
            if (visualModel != null)
            {
                visualModel.transform.position = worldPosition;
            }
        }

        public virtual void Upgrade()
        {
            level++;
            // costToUpgrade logic can be implemented here (e.g., doubling the cost)
            ApplyUpgradeEffect();
        }

        public virtual void UpdateWallState(WallState state)
        {
            if (visualModel != null)
            {
                AreaVisual areaVisual = visualModel.GetComponent<AreaVisual>();
                if (areaVisual != null)
                {
                    areaVisual.SetWallState(state);
                }
            }
        }

        protected abstract void ApplyUpgradeEffect();
    }
}
