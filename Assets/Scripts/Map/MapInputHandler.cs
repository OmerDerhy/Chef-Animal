using UnityEngine;

namespace MergesChef.Map
{
    [RequireComponent(typeof(Map))]
    public class MapInputHandler : MonoBehaviour
    {
        private Map mapManager;
        private Camera mainCamera;
        
        private AreaVisual currentlyDraggingArea;
        private AccessoryVisual currentlyDraggingAccessory;
        private AccessoryVisual selectedAccessory;
        private AreaVisual selectedArea;
        private Vector3 originalVisualPosition;

        // Build Mode: an area/accessory touched but not yet selected. Kept aside until
        // MouseUp confirms the gesture was a tap (not a pan swipe) before selecting it.
        private AreaVisual pendingArea;
        private AccessoryVisual pendingAccessory;

        private void Start()
        {
            mapManager = GetComponent<Map>();
            mainCamera = Camera.main;
            
            // Start zoomed out to see more of the game
            if (mainCamera != null)
            {
                mainCamera.orthographicSize = 3f;
            }
        }

        // Camera Control Variables
        public float zoomSpeed = 2f;
        public float minZoom = 2f;
        public float maxZoom = 20f;
        private bool isPanning;
        private Vector3 lastPanPosition;
        private Vector3 touchDownMousePosition;

        private bool isForcedDragging;
        
        public void ForceStartDragging(AreaVisual area)
        {
            ClearSelection();
            currentlyDraggingArea = area;
            originalVisualPosition = area.transform.position;
            isForcedDragging = true;
        }

        public void ForceStartDragging(AccessoryVisual accessory)
        {
            ClearSelection();
            currentlyDraggingAccessory = accessory;
            originalVisualPosition = accessory.transform.position;
            isForcedDragging = true;
        }

        private void Update()
        {
            if (mapManager == null) return;

            // If the user is touching or clicking on a UI element (like the dimmer panel or scroll view), 
            // ignore the input for the map completely! (unless we are forced dragging, we still want to drag behind UI)
            if (UnityEngine.EventSystems.EventSystem.current != null && !isForcedDragging)
            {
                // Check for mobile touch
                if (Input.touchCount > 0 && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                    return;
                
                // Check for mouse click
                if (Input.touchCount == 0 && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                    return;
            }

            HandleZoom();

            // If zooming with two fingers, don't allow panning or dragging
            if (Input.touchCount >= 2 && !isForcedDragging)
            {
                isPanning = false;
                return;
            }

            if (isForcedDragging)
            {
                DragObject();
                
                // If they click anywhere, try to drop it
                if (Input.GetMouseButtonDown(0))
                {
                    if (currentlyDraggingArea != null || currentlyDraggingAccessory != null)
                    {
                        DropObject();
                    }
                    isForcedDragging = false;
                }
                return; // Skip normal input
            }

            // Mouse Down / Touch Start
            if (Input.GetMouseButtonDown(0))
            {
                touchDownMousePosition = Input.mousePosition;
                bool hitSomething = false;
                
                // Handle tapping depending on the mode
                if (mapManager.GetCurrentMode() == GameMode.BuildMode)
                {
                    // Never drags immediately. It just remembers a pending candidate (whether
                    // or not it's already selected) - we decide what the gesture actually was
                    // at MouseUp/while dragging: a tap on an unselected item selects it, a tap
                    // on an already-selected item deselects it (toggle off), and only a real
                    // drag on an already-selected item picks it up to move it.
                    hitSomething = TryPickUpObject();
                }
                else if (mapManager.GetCurrentMode() == GameMode.PlayMode)
                {
                    // In Play Mode, we wait until MouseUp to determine if it's a tap or a drag.
                    hitSomething = false;
                }

                // If we didn't pick up or select anything, start panning the camera instead
                if (!hitSomething && currentlyDraggingArea == null && currentlyDraggingAccessory == null)
                {
                    ClearSelection();
                    isPanning = true;
                    lastPanPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                }
            }

            // Mouse Drag / Touch Move
            if (Input.GetMouseButton(0))
            {
                if (currentlyDraggingArea != null || currentlyDraggingAccessory != null)
                {
                    DragObject();
                }
                else if (pendingAccessory != null && pendingAccessory == selectedAccessory)
                {
                    // Touched the already-selected accessory. Only commit to actually
                    // dragging it once the finger has moved past the tap threshold.
                    if (Vector3.Distance(touchDownMousePosition, Input.mousePosition) >= 20f)
                    {
                        currentlyDraggingAccessory = pendingAccessory;
                        originalVisualPosition = currentlyDraggingAccessory.transform.position;
                        pendingAccessory = null;
                        DragObject();
                    }
                }
                else if (pendingArea != null && pendingArea == selectedArea)
                {
                    // Same deferred behavior for an already-selected area.
                    if (Vector3.Distance(touchDownMousePosition, Input.mousePosition) >= 20f)
                    {
                        currentlyDraggingArea = pendingArea;
                        originalVisualPosition = currentlyDraggingArea.transform.position;
                        currentlyDraggingArea.SetWallState(WallState.Regular);
                        pendingArea = null;
                        DragObject();
                    }
                }
                else if (isPanning)
                {
                    PanCamera();
                }
            }

            // Mouse Up / Touch End
            if (Input.GetMouseButtonUp(0))
            {
                if (currentlyDraggingArea != null || currentlyDraggingAccessory != null)
                {
                    DropObject();
                }
                else if ((pendingAccessory != null && pendingAccessory == selectedAccessory) ||
                         (pendingArea != null && pendingArea == selectedArea))
                {
                    // Tapped the already-selected item without dragging it - toggle its
                    // selection off, so the shiny highlight disappears.
                    ClearSelection();
                }
                else if (isPanning)
                {
                    // If the mouse didn't move much, it was a tap rather than a pan swipe!
                    if (Vector3.Distance(touchDownMousePosition, Input.mousePosition) < 20f)
                    {
                        if (mapManager.GetCurrentMode() == GameMode.PlayMode)
                        {
                            HandlePlayModeTap();
                        }
                        else if (mapManager.GetCurrentMode() == GameMode.BuildMode)
                        {
                            HandleBuildModeTap();
                        }
                    }
                }
                isPanning = false;
                pendingArea = null;
                pendingAccessory = null;
            }

            ClampCameraPosition();
        }

        private void ClampCameraPosition()
        {
            Vector3 camPos = mainCamera.transform.position;

            // Get the camera's current viewing box size
            float vertExtent = mainCamera.orthographicSize;
            float horzExtent = vertExtent * mainCamera.aspect;

            // Hardcoded bounds requested by user
            float mapMinX = -4.5f;
            float mapMaxX = 4.5f;
            float mapMinY = -5f;
            float mapMaxY = 5f;

            float minX = mapMinX + horzExtent;
            float maxX = mapMaxX - horzExtent;
            float minY = mapMinY + vertExtent;
            float maxY = mapMaxY - vertExtent;

            // If the map is smaller than the screen, lock the camera to the center of the map
            if (minX > maxX)
            {
                float centerX = (mapMinX + mapMaxX) / 2f;
                minX = centerX;
                maxX = centerX;
            }
            if (minY > maxY)
            {
                float centerY = (mapMinY + mapMaxY) / 2f;
                minY = centerY;
                maxY = centerY;
            }

            camPos.x = Mathf.Clamp(camPos.x, minX, maxX);
            camPos.y = Mathf.Clamp(camPos.y, minY, maxY);

            mainCamera.transform.position = camPos;
        }

        private void HandleZoom()
        {
            // Mouse Scroll Wheel
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                mainCamera.orthographicSize -= scroll * zoomSpeed * 2f;
                mainCamera.orthographicSize = Mathf.Clamp(mainCamera.orthographicSize, minZoom, maxZoom);
            }

            // Mobile Pinch to Zoom
            if (Input.touchCount == 2)
            {
                Touch touch0 = Input.GetTouch(0);
                Touch touch1 = Input.GetTouch(1);

                Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
                Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;

                float prevMagnitude = (touch0PrevPos - touch1PrevPos).magnitude;
                float currentMagnitude = (touch0.position - touch1.position).magnitude;

                float difference = currentMagnitude - prevMagnitude;

                mainCamera.orthographicSize -= difference * 0.01f * zoomSpeed;
                mainCamera.orthographicSize = Mathf.Clamp(mainCamera.orthographicSize, minZoom, maxZoom);
            }
        }

        private void PanCamera()
        {
            Vector3 currentWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 difference = lastPanPosition - currentWorldPos;
            
            mainCamera.transform.position += difference;
            
            // Re-calculate last pan position after moving camera
            lastPanPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        }

        private void ClearSelection()
        {
            if (selectedAccessory != null)
            {
                selectedAccessory.SetSelected(false);
                selectedAccessory = null;
            }
            if (selectedArea != null)
            {
                selectedArea.SetSelected(false);
                selectedArea = null;
            }
            UI.SelectionUIManager.Instance?.HidePanel();
        }

        public void ClearDraggingState(bool isCanceling)
        {
            if (isCanceling)
            {
                if (currentlyDraggingArea != null && isForcedDragging)
                {
                    Destroy(currentlyDraggingArea.gameObject);
                }
                if (currentlyDraggingAccessory != null && isForcedDragging)
                {
                    Destroy(currentlyDraggingAccessory.gameObject);
                }
            }
            else
            {
                if (currentlyDraggingArea != null || currentlyDraggingAccessory != null)
                {
                    DropObject();
                }
            }
            
            currentlyDraggingArea = null;
            currentlyDraggingAccessory = null;
            isForcedDragging = false;
        }

        private bool HandlePlayModeTap()
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(ray);
            
            AccessoryVisual hitAccessory = null;
            AreaVisual hitArea = null;

            foreach (var hit in hits)
            {
                if (hitAccessory == null)
                {
                    AccessoryVisual acc = hit.collider.GetComponentInParent<AccessoryVisual>();
                    if (acc != null) hitAccessory = acc;
                }
                
                if (hitArea == null)
                {
                    AreaVisual area = hit.collider.GetComponentInParent<AreaVisual>();
                    if (area != null) hitArea = area;
                }
            }

            // Deselect previous
            ClearSelection();

            if (hitAccessory != null)
            {
                if (hitAccessory is KitchenAccessoryVisual kv && kv.IsCooking)
                {
                    // If cooking, don't show the UI, just do nothing
                    return true;
                }
                
                selectedAccessory = hitAccessory;
                selectedAccessory.SetSelected(true);
                
                if (UI.SelectionUIManager.Instance != null)
                {
                    UI.SelectionUIManager.Instance.ShowPanel(selectedAccessory, null);
                }
                return true;
            }

            if (hitArea != null)
            {
                selectedArea = hitArea;
                selectedArea.SetSelected(true);
                
                if (UI.SelectionUIManager.Instance != null)
                {
                    UI.SelectionUIManager.Instance.ShowPanel(null, selectedArea);
                }
                return true;
            }

            // Hit nothing, hide UI
            if (UI.SelectionUIManager.Instance != null)
            {
                UI.SelectionUIManager.Instance.HidePanel();
            }

            return false;
        }

        private bool TryPickUpObject()
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(ray);
            
            AccessoryVisual hitAccessory = null;
            AreaVisual hitArea = null;

            // Sort through all hits to find what we clicked on, prioritizing accessories over areas
            foreach (var hit in hits)
            {
                if (hitAccessory == null)
                {
                    AccessoryVisual acc = hit.collider.GetComponentInParent<AccessoryVisual>();
                    if (acc != null) hitAccessory = acc;
                }
                
                if (hitArea == null)
                {
                    AreaVisual area = hit.collider.GetComponentInParent<AreaVisual>();
                    if (area != null) hitArea = area;
                }
            }

            // Handle Accessory Click
            if (hitAccessory != null)
            {
                pendingAccessory = hitAccessory;

                if (selectedAccessory == hitAccessory)
                {
                    // Already selected - don't drag or deselect yet. A plain tap will
                    // toggle it off; only an actual drag (checked in Update) picks it up
                    // to move it. Returning true here just stops the camera from panning.
                    return true;
                }

                // Not yet selected - don't select it yet either. A swipe that merely
                // starts on top of it should still pan the camera; only commit the
                // selection (in HandleBuildModeTap) if MouseUp shows this was a tap.
                return false;
            }

            // Handle Area Click (if no accessory was hit)
            if (hitArea != null)
            {
                pendingArea = hitArea;

                if (selectedArea == hitArea)
                {
                    // Already selected - same deferred tap-to-deselect/drag-to-move
                    // behavior as accessories above.
                    return true;
                }

                // Not yet selected - same deferred behavior as accessories above, so
                // swiping across an unselected area pans instead of dragging it.
                return false;
            }

            return false;
        }

        private void HandleBuildModeTap()
        {
            if (pendingAccessory != null)
            {
                selectedAccessory = pendingAccessory;
                selectedAccessory.SetSelected(true);
                UI.SelectionUIManager.Instance?.ShowBuildModeFlip(selectedAccessory);
            }
            else if (pendingArea != null)
            {
                selectedArea = pendingArea;
                selectedArea.SetSelected(true);
                UI.SelectionUIManager.Instance?.ShowBuildModeAreaPanel(selectedArea);
            }

            pendingAccessory = null;
            pendingArea = null;
        }

        private void DragObject()
        {
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            
            if (currentlyDraggingArea != null)
            {
                // Snap to Grid
                Vector2Int newGridPos = mapManager.WorldToGridPosition(worldPos);
                Vector3 snappedPos = mapManager.GridToWorldPosition(newGridPos);
                currentlyDraggingArea.transform.position = new Vector3(snappedPos.x, snappedPos.y, originalVisualPosition.z);
                
                // Drag all accessories with it
                AreaBase areaData = currentlyDraggingArea.LinkedArea;
                for (int x = 0; x < 3; x++)
                {
                    for (int y = 0; y < 3; y++)
                    {
                        AccessoryBase acc = areaData.GetAccessoryAt(new Vector2Int(x, y));
                        if (acc != null && acc.VisualModel != null)
                        {
                            Vector3 accPos = mapManager.GetAccessoryWorldPosition(newGridPos, new Vector2Int(x, y));
                            acc.VisualModel.transform.position = new Vector3(accPos.x, accPos.y, originalVisualPosition.z - 1);
                        }
                    }
                }
            }
            else if (currentlyDraggingAccessory != null)
            {
                Vector2Int newGridPos = mapManager.WorldToGridPosition(worldPos);
                Vector2Int subGridPos = mapManager.GetSubGridPosFromWorld(worldPos, newGridPos);
                
                MergesChef.Map.AreaBase area = mapManager.GetAreaAt(newGridPos);
                if (area != null)
                {
                    Vector3 snappedPos = mapManager.GetAccessoryWorldPosition(newGridPos, subGridPos);
                    currentlyDraggingAccessory.transform.position = new Vector3(snappedPos.x, snappedPos.y, originalVisualPosition.z);
                }
                else
                {
                    currentlyDraggingAccessory.transform.position = new Vector3(worldPos.x, worldPos.y, originalVisualPosition.z);
                }
            }
        }

        private void DropObject()
        {
            if (currentlyDraggingArea != null)
            {
                Vector3 dropPosition = currentlyDraggingArea.transform.position;
                Vector2Int newGridPos = mapManager.WorldToGridPosition(dropPosition);

                bool success = mapManager.TryMoveArea(currentlyDraggingArea.LinkedArea, newGridPos);

                if (!success)
                {
                    currentlyDraggingArea.transform.position = originalVisualPosition;
                    
                    AreaBase areaData = currentlyDraggingArea.LinkedArea;
                    for (int x = 0; x < 3; x++)
                    {
                        for (int y = 0; y < 3; y++)
                        {
                            AccessoryBase acc = areaData.GetAccessoryAt(new Vector2Int(x, y));
                            if (acc != null && acc.VisualModel != null)
                            {
                                acc.VisualModel.transform.position = mapManager.GetAccessoryWorldPosition(areaData.Position, new Vector2Int(x, y));
                            }
                        }
                    }
                    mapManager.UpdateMapVisuals();
                }
                
                currentlyDraggingArea = null;
            }
            else if (currentlyDraggingAccessory != null)
            {
                Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                worldPos.z = 0;
                Vector2Int newGridPos = mapManager.WorldToGridPosition(worldPos);
                Vector2Int subGridPos = mapManager.GetSubGridPosFromWorld(worldPos, newGridPos);

                bool success = mapManager.TryMoveAccessory(currentlyDraggingAccessory.LinkedAccessory, newGridPos, subGridPos);

                if (!success)
                {
                    currentlyDraggingAccessory.transform.position = originalVisualPosition;
                }
                
                // Restore sorting order
                SortableVisual sortable = currentlyDraggingAccessory.GetComponent<SortableVisual>();
                if (sortable != null)
                {
                    sortable.UpdateSortingOrder();
                }
                else
                {
                    var sg = currentlyDraggingAccessory.GetComponent<UnityEngine.Rendering.SortingGroup>();
                    if (sg != null) sg.sortingOrder = 20000;
                }
                
                currentlyDraggingAccessory = null;
                ClearSelection();
            }
        }
    }
}
