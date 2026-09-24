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
        
        private List<GameObject> activeLines = new List<GameObject>();

        public void ShowGrid(Dictionary<Vector2Int, AreaBase> grid, Grid layoutGrid, bool isValid = true)
        {
            HideGrid(); // Clear old lines

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
            foreach (var line in activeLines)
            {
                if (line != null) Destroy(line);
            }
            activeLines.Clear();
        }
    }
}
