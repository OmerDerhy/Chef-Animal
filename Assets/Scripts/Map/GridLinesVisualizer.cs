using UnityEngine;
using System.Collections.Generic;

namespace MergesChef.Map
{
    public class GridLinesVisualizer : MonoBehaviour
    {
        public Material lineMaterial;
        public Color lineColor = new Color(1f, 1f, 0.8f, 0.8f);
        public Color invalidLineColor = new Color(1f, 0f, 0f, 0.8f);
        public float lineWidth = 0.02f;
        
        [Header("Drag Preview")]
        public Color validDropColor = new Color(0.3f, 1f, 0.4f, 0.95f);
        public Color invalidDropColor = new Color(1f, 0.25f, 0.25f, 0.95f);
        public float highlightWidthMultiplier = 3f;

        private List<GameObject> activeLines = new List<GameObject>();

        // Kept outside activeLines so ShowGrid/HideGrid can't destroy it mid-drag.
        private GameObject highlightObj;
        private LineRenderer highlightLine;

        /// <summary>
        /// Draws a thick green/red diamond over the cell the player is currently dragging onto, so the
        /// drop verdict is visible before they let go rather than only after.
        /// </summary>
        public void ShowCellHighlight(Vector2Int cell, Grid layoutGrid, bool isValid)
        {
            if (layoutGrid == null) return;

            if (highlightObj == null)
            {
                if (lineMaterial == null) lineMaterial = new Material(Shader.Find("Sprites/Default"));

                highlightObj = new GameObject("DropHighlight");
                highlightObj.transform.SetParent(this.transform);

                highlightLine = highlightObj.AddComponent<LineRenderer>();
                highlightLine.material = lineMaterial;
                highlightLine.useWorldSpace = true;
                highlightLine.loop = true;
                highlightLine.positionCount = 4;
                // Above the floor and the regular grid lines, below the dragged object itself.
                highlightLine.sortingOrder = 19000;
            }

            highlightObj.SetActive(true);

            Color c = isValid ? validDropColor : invalidDropColor;
            highlightLine.startColor = c;
            highlightLine.endColor = c;
            highlightLine.startWidth = lineWidth * highlightWidthMultiplier;
            highlightLine.endWidth = lineWidth * highlightWidthMultiplier;

            GetDiamondCorners(layoutGrid, cell, out Vector3 bottom, out Vector3 right, out Vector3 top, out Vector3 left);
            highlightLine.SetPosition(0, bottom);
            highlightLine.SetPosition(1, right);
            highlightLine.SetPosition(2, top);
            highlightLine.SetPosition(3, left);
        }

        public void HideCellHighlight()
        {
            if (highlightObj != null) highlightObj.SetActive(false);
        }

        private void GetDiamondCorners(Grid layoutGrid, Vector2Int cell,
                                       out Vector3 bottom, out Vector3 right, out Vector3 top, out Vector3 left)
        {
            Vector3 rightVec = layoutGrid.GetCellCenterWorld(new Vector3Int(1, 0, 0)) - layoutGrid.GetCellCenterWorld(Vector3Int.zero);
            Vector3 upVec = layoutGrid.GetCellCenterWorld(new Vector3Int(0, 1, 0)) - layoutGrid.GetCellCenterWorld(Vector3Int.zero);
            Vector3 center = layoutGrid.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));

            bottom = center - (rightVec / 2f) - (upVec / 2f);
            right = center + (rightVec / 2f) - (upVec / 2f);
            top = center + (rightVec / 2f) + (upVec / 2f);
            left = center - (rightVec / 2f) + (upVec / 2f);
        }

        public void ShowGrid(Dictionary<Vector2Int, AreaBase> grid, Grid layoutGrid, bool isValid = true)
        {
            ClearLines(); // Clear old lines, but keep any in-flight drag highlight

            if (layoutGrid == null) return;
            
            // Create a default material if none provided
            if (lineMaterial == null)
            {
                lineMaterial = new Material(Shader.Find("Sprites/Default"));
            }

            Vector3 rightVec = layoutGrid.GetCellCenterWorld(new Vector3Int(1, 0, 0)) - layoutGrid.GetCellCenterWorld(Vector3Int.zero);
            Vector3 upVec = layoutGrid.GetCellCenterWorld(new Vector3Int(0, 1, 0)) - layoutGrid.GetCellCenterWorld(Vector3Int.zero);

            foreach (var pos in grid.Keys)
            {
                Vector3 center = layoutGrid.GetCellCenterWorld(new Vector3Int(pos.x, pos.y, 0));

                Vector3 bottom = center - (rightVec / 2f) - (upVec / 2f);
                Vector3 right = center + (rightVec / 2f) - (upVec / 2f);
                Vector3 top = center + (rightVec / 2f) + (upVec / 2f);
                Vector3 left = center - (rightVec / 2f) + (upVec / 2f);

                CreateDiamondOutline(bottom, right, top, left, pos, isValid);
            }
        }

        private void CreateDiamondOutline(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Vector2Int gridPos, bool isValid)
        {
            GameObject lineObj = new GameObject($"GridLine_{gridPos.x}_{gridPos.y}");
            lineObj.transform.SetParent(this.transform);
            
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.material = lineMaterial;
            
            Color displayColor = isValid ? lineColor : invalidLineColor;
            lr.startColor = displayColor;
            lr.endColor = displayColor;
            
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.useWorldSpace = true;
            lr.loop = true;
            lr.positionCount = 4;
            
            lr.SetPosition(0, p1);
            lr.SetPosition(1, p2);
            lr.SetPosition(2, p3);
            lr.SetPosition(3, p4);
            
            // Draw on top of floors, below accessories
            lr.sortingOrder = 18000;
            
            activeLines.Add(lineObj);
        }

        public void HideGrid()
        {
            ClearLines();
            HideCellHighlight();
        }

        private void ClearLines()
        {
            foreach (var line in activeLines)
            {
                if (line != null) Destroy(line);
            }
            activeLines.Clear();
        }
    }
}
