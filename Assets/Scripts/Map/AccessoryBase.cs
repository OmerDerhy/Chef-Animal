using UnityEngine;
using System;

namespace MergesChef.Map
{
    [Serializable]
    public abstract class AccessoryBase : IPlaceable
    {
        [SerializeField] protected string accessoryName;
        [SerializeField] protected Vector2Int position;
        [SerializeField] protected Vector2Int subGridPosition;
        [SerializeField] protected int level = 1;
        [SerializeField] protected int costToUpgrade = 100;
        [SerializeField] protected bool isFlipped = false;

        public Vector2Int Position => position;
        public Vector2Int SubGridPosition => subGridPosition;
        public string AccessoryName => accessoryName;
        public int Level => level;
        public int CostToUpgrade => costToUpgrade;
        public bool IsFlipped => isFlipped;

        public void SetSubGridPosition(Vector2Int newSubGridPos)
        {
            subGridPosition = newSubGridPos;
        }

        public void InitializeFromSave(int savedLevel)
        {
            if (savedLevel <= 1) return;
            level = savedLevel;
            costToUpgrade = 100; // Base cost
            for (int i = 1; i < level; i++)
            {
                costToUpgrade = Mathf.RoundToInt(costToUpgrade * 1.5f);
            }
        }

        [NonSerialized] protected GameObject visualModel;
        public GameObject VisualModel => visualModel;

        public virtual void BindVisual(GameObject visual)
        {
            visualModel = visual;
            AccessoryVisual accessoryVisual = visual.GetComponent<AccessoryVisual>();
            if (accessoryVisual == null)
            {
                accessoryVisual = visual.AddComponent<AccessoryVisual>();
            }
            accessoryVisual.LinkedAccessory = this;

            // Set the initial visual state based on current level
            accessoryVisual.SetVariation(level - 1);
            ApplyFlipRotation();
        }

        public void ToggleFlip()
        {
            isFlipped = !isFlipped;
            ApplyFlipRotation();
        }

        public void SetFlipped(bool flipped)
        {
            isFlipped = flipped;
            ApplyFlipRotation();
        }

        private void ApplyFlipRotation()
        {
            if (visualModel != null)
            {
                visualModel.transform.localRotation = Quaternion.Euler(0, isFlipped ? 180 : 0, 0);
            }
        }

        public void Move(Vector2Int newPosition, Vector3 newWorldPosition)
        {
            position = newPosition;
            if (visualModel != null)
            {
                visualModel.transform.position = newWorldPosition;
                
                SortableVisual sortable = visualModel.GetComponent<SortableVisual>();
                if (sortable != null)
                {
                    sortable.UpdateSortingOrder();
                }
            }
        }

        public virtual bool IsPlacementValid(AreaBase targetArea, Vector2Int targetSubGridPos)
        {
            return true;
        }

        public virtual void UpdateVisualState(AreaBase currentArea)
        {
            // Override in derived classes to swap visuals
        }

        public virtual void Upgrade()
        {
            level++;
            costToUpgrade = Mathf.RoundToInt(costToUpgrade * 1.5f);
            
            if (visualModel != null)
            {
                var visual = visualModel.GetComponent<AccessoryVisual>();
                if (visual != null)
                {
                    visual.SetVariation(level - 1);
                }
            }
        }
    }
}
