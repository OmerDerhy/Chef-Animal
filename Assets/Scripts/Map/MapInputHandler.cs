using UnityEngine;
using DG.Tweening;
using MergesChef.Feel;

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

        [Header("Gesture Tuning")]
        [Tooltip("Screen pixels the finger may travel before a tap is treated as a drag/swipe instead.")]
        public float tapMoveThreshold = 20f;
        [Tooltip("Hold this long without moving to pick an object straight up, Clash of Clans style.")]
        public float longPressDuration = 0.25f;

        [Header("Camera Control")]
        public float zoomSpeed = 2f;
        public float minZoom = 2f;
        public float maxZoom = 20f;
        [Tooltip("How quickly flick momentum bleeds off. Higher stops sooner.")]
        public float panInertiaDamping = 6f;
        [Tooltip("How far past the map edge the camera may be dragged before springing back.")]
        public float edgeSlack = 1.2f;

        [Header("Camera Bounds")]
        public Vector2 mapMin = new Vector2(-4.5f, -5f);
        public Vector2 mapMax = new Vector2(4.5f, 5f);

        private bool isPanning;
        private Vector3 lastPanPosition;
        private Vector3 touchDownMousePosition;
        private float pointerDownTime;

        private bool isForcedDragging;

        // Drag session state
        private Vector3 dragGrabOffset;
        private Vector3 dragTargetPosition;
        private Vector3 dragVelocity;

        // Camera momentum
        private Vector3 panVelocity;

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

        public void ForceStartDragging(AreaVisual area)
        {
            ClearSelection();
            currentlyDraggingArea = area;
            originalVisualPosition = area.transform.position;
            // Walls are a function of neighbours, which mean nothing mid-air, so a held area always
            // shows its plain floor sprite. UpdateMapVisuals restores the real state on drop.
            area.SetWallState(WallState.Regular);
            BeginDragSession(area, Vector3.zero);
            isForcedDragging = true;
        }

        public void ForceStartDragging(AccessoryVisual accessory)
        {
            ClearSelection();
            currentlyDraggingAccessory = accessory;
            originalVisualPosition = accessory.transform.position;
            BeginDragSession(accessory, Vector3.zero);
            isForcedDragging = true;
        }

        private bool IsDragging => currentlyDraggingArea != null || currentlyDraggingAccessory != null;

        private Vector3 PointerWorld()
        {
            Vector3 world = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            world.z = 0f;
            return world;
        }

        private void Update()
        {
            if (mapManager == null || mainCamera == null) return;

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
                UpdateDragTarget();

                // If they click anywhere, try to drop it
                if (Input.GetMouseButtonDown(0))
                {
                    if (IsDragging)
                    {
                        DropObject();
                    }
                    isForcedDragging = false;
                }
                return; // Skip normal input
            }

            if (Input.GetMouseButtonDown(0)) HandlePointerDown();
            if (Input.GetMouseButton(0)) HandlePointerHeld();
            if (Input.GetMouseButtonUp(0)) HandlePointerUp();
        }

        /// <summary>
        /// Motion runs here rather than in Update so a dragged object keeps gliding and the camera keeps
        /// coasting even on frames where input was swallowed by the UI gate above.
        /// </summary>
        private void LateUpdate()
        {
            if (mapManager == null || mainCamera == null) return;

            ApplyDragMotion();
            ApplyPanInertia();
            ClampCameraPosition();
        }

        // ── Pointer phases ──────────────────────────────────────────────────

        private void HandlePointerDown()
        {
            touchDownMousePosition = Input.mousePosition;
            pointerDownTime = Time.unscaledTime;
            panVelocity = Vector3.zero; // a new touch kills any coasting

            bool hitSomething = false;

            if (mapManager.GetCurrentMode() == GameMode.BuildMode)
            {
                // Never drags immediately. It just remembers a pending candidate (whether
                // or not it's already selected) - we decide what the gesture actually was
                // at MouseUp/while dragging: a tap on an unselected item selects it, a tap
                // on an already-selected item deselects it (toggle off), a long press picks
                // it straight up, and a drag on an already-selected item moves it.
                hitSomething = TryPickUpObject();
            }
            else if (mapManager.GetCurrentMode() == GameMode.PlayMode)
            {
                // In Play Mode, we wait until MouseUp to determine if it's a tap or a drag.
                hitSomething = false;
            }

            // If we didn't pick up or select anything, start panning the camera instead
            if (!hitSomething && !IsDragging)
            {
                ClearSelection();
                isPanning = true;
                lastPanPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            }
        }

        private void HandlePointerHeld()
        {
            if (IsDragging)
            {
                UpdateDragTarget();
                return;
            }

            float travel = Vector3.Distance(touchDownMousePosition, Input.mousePosition);
            bool heldLongEnough = Time.unscaledTime - pointerDownTime >= longPressDuration;

            // Clash of Clans pickup: press and hold anything, selected or not, and it lifts.
            // A swipe disqualifies itself by exceeding the tap threshold before the timer elapses.
            if (heldLongEnough && travel < tapMoveThreshold)
            {
                if (pendingAccessory != null) { BeginDragAccessory(pendingAccessory); return; }
                if (pendingArea != null) { BeginDragArea(pendingArea); return; }
            }

            // Existing behaviour: dragging something that was already selected moves it.
            if (travel >= tapMoveThreshold)
            {
                if (pendingAccessory != null && pendingAccessory == selectedAccessory)
                {
                    BeginDragAccessory(pendingAccessory);
                    return;
                }
                if (pendingArea != null && pendingArea == selectedArea)
                {
                    BeginDragArea(pendingArea);
                    return;
                }
            }

            if (isPanning) PanCamera();
        }

        private void HandlePointerUp()
        {
            if (IsDragging)
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
                if (Vector3.Distance(touchDownMousePosition, Input.mousePosition) < tapMoveThreshold)
                {
                    panVelocity = Vector3.zero; // a tap must not fling the camera
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

        // ── Camera ──────────────────────────────────────────────────────────

        private void ClampCameraPosition()
        {
            Vector3 camPos = mainCamera.transform.position;

            float vertExtent = mainCamera.orthographicSize;
            float horzExtent = vertExtent * mainCamera.aspect;

            float minX = mapMin.x + horzExtent;
            float maxX = mapMax.x - horzExtent;
            float minY = mapMin.y + vertExtent;
            float maxY = mapMax.y - vertExtent;

            // If the map is smaller than the screen, lock the camera to the center of the map
            if (minX > maxX)
            {
                float centerX = (mapMin.x + mapMax.x) / 2f;
                minX = centerX;
                maxX = centerX;
            }
            if (minY > maxY)
            {
                float centerY = (mapMin.y + mapMax.y) / 2f;
                minY = centerY;
                maxY = centerY;
            }

            if (isPanning)
            {
                // While the finger is down the edge gives a little, then resists.
                camPos.x = RubberBand(camPos.x, minX, maxX, edgeSlack);
                camPos.y = RubberBand(camPos.y, minY, maxY, edgeSlack);
            }
            else
            {
                // Released: spring back inside the bounds.
                float settle = 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime);
                float targetX = Mathf.Clamp(camPos.x, minX, maxX);
                float targetY = Mathf.Clamp(camPos.y, minY, maxY);

                if (!Mathf.Approximately(camPos.x, targetX)) panVelocity.x = 0f;
                if (!Mathf.Approximately(camPos.y, targetY)) panVelocity.y = 0f;

                camPos.x = Mathf.Lerp(camPos.x, targetX, settle);
                camPos.y = Mathf.Lerp(camPos.y, targetY, settle);
            }

            mainCamera.transform.position = camPos;
        }

        /// <summary>Compresses travel past a bound so the edge feels elastic instead of hitting a wall.</summary>
        private static float RubberBand(float value, float min, float max, float slack)
        {
            if (value < min) return min - Compress(min - value, slack);
            if (value > max) return max + Compress(value - max, slack);
            return value;
        }

        private static float Compress(float overshoot, float slack)
        {
            if (slack <= 0f) return 0f;
            return slack * (1f - 1f / (overshoot / slack + 1f));
        }

        private void HandleZoom()
        {
            // Mouse Scroll Wheel
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                ZoomAround(Input.mousePosition, scroll * zoomSpeed * 2f);
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
                Vector2 pinchCenter = (touch0.position + touch1.position) * 0.5f;

                ZoomAround(pinchCenter, difference * 0.01f * zoomSpeed);
            }
        }

        /// <summary>Keeps the world point under <paramref name="screenPoint"/> pinned while zooming.</summary>
        private void ZoomAround(Vector3 screenPoint, float delta)
        {
            Vector3 before = mainCamera.ScreenToWorldPoint(screenPoint);

            mainCamera.orthographicSize = Mathf.Clamp(mainCamera.orthographicSize - delta, minZoom, maxZoom);

            Vector3 after = mainCamera.ScreenToWorldPoint(screenPoint);
            Vector3 correction = before - after;
            correction.z = 0f;
            mainCamera.transform.position += correction;
        }

        private void PanCamera()
        {
            Vector3 currentWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 difference = lastPanPosition - currentWorldPos;

            mainCamera.transform.position += difference;

            // Remember how fast the map was moving so a flick keeps coasting after release.
            if (Time.unscaledDeltaTime > 0.0001f)
            {
                Vector3 instant = difference / Time.unscaledDeltaTime;
                panVelocity = Vector3.Lerp(panVelocity, instant, 0.6f);
            }

            // Re-calculate last pan position after moving camera
            lastPanPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        }

        private void ApplyPanInertia()
        {
            if (isPanning || panVelocity.sqrMagnitude < 0.0001f) return;

            mainCamera.transform.position += panVelocity * Time.unscaledDeltaTime;
            panVelocity = Vector3.Lerp(panVelocity, Vector3.zero,
                                       1f - Mathf.Exp(-panInertiaDamping * Time.unscaledDeltaTime));
        }

        // ── Selection ───────────────────────────────────────────────────────

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
                if (IsDragging)
                {
                    DropObject();
                }
            }

            currentlyDraggingArea = null;
            currentlyDraggingAccessory = null;
            isForcedDragging = false;
            mapManager.HideDropHighlight();
        }

        private bool HandlePlayModeTap()
        {
            RaycastCandidates(out AccessoryVisual hitAccessory, out AreaVisual hitArea);

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

        private void RaycastCandidates(out AccessoryVisual hitAccessory, out AreaVisual hitArea)
        {
            hitAccessory = null;
            hitArea = null;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(ray);

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
        }

        private bool TryPickUpObject()
        {
            RaycastCandidates(out AccessoryVisual hitAccessory, out AreaVisual hitArea);

            if (hitAccessory != null)
            {
                pendingAccessory = hitAccessory;

                // Already selected - returning true stops the camera from panning underneath it.
                // Not yet selected - return false so a swipe that merely starts on top of it still
                // pans; the long-press timer in HandlePointerHeld can still promote it to a drag.
                return selectedAccessory == hitAccessory;
            }

            if (hitArea != null)
            {
                pendingArea = hitArea;
                return selectedArea == hitArea;
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

        // ── Dragging ────────────────────────────────────────────────────────

        private void BeginDragArea(AreaVisual area)
        {
            ClearSelection();
            currentlyDraggingArea = area;
            originalVisualPosition = area.transform.position;
            // Walls are a function of neighbours, which mean nothing mid-air, so a held area always
            // shows its plain floor sprite. UpdateMapVisuals restores the real state on drop.
            area.SetWallState(WallState.Regular);
            BeginDragSession(area, area.transform.position - PointerWorld());
        }

        private void BeginDragAccessory(AccessoryVisual accessory)
        {
            ClearSelection();
            currentlyDraggingAccessory = accessory;
            originalVisualPosition = accessory.transform.position;
            BeginDragSession(accessory, accessory.transform.position - PointerWorld());
        }

        private void BeginDragSession(SortableVisual visual, Vector3 grabOffset)
        {
            dragGrabOffset = grabOffset;
            dragTargetPosition = visual.transform.position;
            dragVelocity = Vector3.zero;

            visual.SetDragging(true);

            isPanning = false;
            panVelocity = Vector3.zero;
            pendingAccessory = null;
            pendingArea = null;
        }

        /// <summary>Works out where the held object wants to sit and previews whether it may land there.</summary>
        private void UpdateDragTarget()
        {
            Vector3 anchor = PointerWorld() + dragGrabOffset;

            if (currentlyDraggingArea != null)
            {
                Vector2Int newGridPos = mapManager.WorldToGridPosition(anchor);
                Vector3 snapped = mapManager.GridToWorldPosition(newGridPos);
                dragTargetPosition = new Vector3(snapped.x, snapped.y, originalVisualPosition.z);

                ShowPreview(newGridPos, mapManager.CanPlaceAreaAt(currentlyDraggingArea.LinkedArea, newGridPos));
            }
            else if (currentlyDraggingAccessory != null)
            {
                Vector2Int newGridPos = mapManager.WorldToGridPosition(anchor);
                Vector2Int subGridPos = mapManager.GetSubGridPosFromWorld(anchor, newGridPos);

                if (mapManager.GetAreaAt(newGridPos) != null)
                {
                    Vector3 snapped = mapManager.GetAccessoryWorldPosition(newGridPos, subGridPos);
                    dragTargetPosition = new Vector3(snapped.x, snapped.y, originalVisualPosition.z);
                }
                else
                {
                    // No floor under the finger: follow it freely so it never appears stuck.
                    dragTargetPosition = new Vector3(anchor.x, anchor.y, originalVisualPosition.z);
                }

                ShowPreview(newGridPos,
                            mapManager.CanPlaceAccessoryAt(currentlyDraggingAccessory.LinkedAccessory, newGridPos, subGridPos));
            }
        }

        private void ShowPreview(Vector2Int cell, bool isValid)
        {
            mapManager.ShowDropHighlight(cell, isValid);
        }

        /// <summary>
        /// Eases the held object toward its snapped target every frame. This glide, rather than a hard
        /// per-frame assignment, is what makes the grid snap read as smooth instead of teleporting.
        /// </summary>
        private void ApplyDragMotion()
        {
            if (!IsDragging) return;

            Transform t = currentlyDraggingArea != null
                ? currentlyDraggingArea.transform
                : currentlyDraggingAccessory.transform;

            t.position = Vector3.SmoothDamp(t.position, dragTargetPosition, ref dragVelocity,
                                            Juice.DragFollowSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);

            if (currentlyDraggingArea != null)
            {
                SyncAreaAccessoriesToVisual(currentlyDraggingArea);
                currentlyDraggingArea.UpdateSortingOrder();
            }
            else
            {
                currentlyDraggingAccessory.UpdateSortingOrder();
            }
        }

        /// <summary>Carries an area's accessories along with it while it is being dragged.</summary>
        private void SyncAreaAccessoriesToVisual(AreaVisual areaVisual)
        {
            AreaBase areaData = areaVisual.LinkedArea;
            if (areaData == null) return;

            Vector3 center = areaVisual.transform.position;

            for (int x = 0; x < Map.SubGridSize; x++)
            {
                for (int y = 0; y < Map.SubGridSize; y++)
                {
                    AccessoryBase acc = areaData.GetAccessoryAt(new Vector2Int(x, y));
                    if (acc == null || acc.VisualModel == null) continue;

                    Vector3 offset = SubGridOffset(new Vector2Int(x, y));
                    acc.VisualModel.transform.position =
                        new Vector3(center.x + offset.x, center.y + offset.y, center.z - 1f);
                }
            }
        }

        /// <summary>Offset of a sub-grid slot from its area's centre. Constant, so cell (0,0) is fine to sample.</summary>
        private Vector3 SubGridOffset(Vector2Int subGridPos)
        {
            return mapManager.GetAccessoryWorldPosition(Vector2Int.zero, subGridPos)
                 - mapManager.GridToWorldPosition(Vector2Int.zero);
        }

        private void DropObject()
        {
            mapManager.HideDropHighlight();

            if (currentlyDraggingArea != null)
            {
                AreaVisual area = currentlyDraggingArea;
                currentlyDraggingArea = null;

                // Drop where the preview said, so the verdict the player saw is the one they get.
                Vector2Int newGridPos = mapManager.WorldToGridPosition(dragTargetPosition);
                bool success = mapManager.TryMoveArea(area.LinkedArea, newGridPos);

                area.SetDragging(false);

                if (success)
                {
                    area.PlayDropSettle();
                }
                else
                {
                    area.PlayRejected();
                    // Both jobs go in one callback: DOTween's OnUpdate replaces rather than appends,
                    // so chaining a second one here would silently drop ReturnTo's own sorting update.
                    area.ReturnTo(originalVisualPosition)
                        .OnUpdate(() =>
                        {
                            area.UpdateSortingOrder();
                            SyncAreaAccessoriesToVisual(area);
                        })
                        .OnComplete(() =>
                        {
                            AreaBase areaData = area.LinkedArea;
                            for (int x = 0; x < Map.SubGridSize; x++)
                            {
                                for (int y = 0; y < Map.SubGridSize; y++)
                                {
                                    AccessoryBase acc = areaData.GetAccessoryAt(new Vector2Int(x, y));
                                    if (acc != null && acc.VisualModel != null)
                                    {
                                        acc.VisualModel.transform.position =
                                            mapManager.GetAccessoryWorldPosition(areaData.Position, new Vector2Int(x, y));
                                    }
                                }
                            }
                            mapManager.UpdateMapVisuals();
                        });
                }
            }
            else if (currentlyDraggingAccessory != null)
            {
                AccessoryVisual accessory = currentlyDraggingAccessory;
                currentlyDraggingAccessory = null;

                Vector2Int newGridPos = mapManager.WorldToGridPosition(dragTargetPosition);
                Vector2Int subGridPos = mapManager.GetSubGridPosFromWorld(dragTargetPosition, newGridPos);

                bool success = mapManager.TryMoveAccessory(accessory.LinkedAccessory, newGridPos, subGridPos);

                accessory.SetDragging(false);

                if (success)
                {
                    accessory.PlayDropSettle();
                }
                else
                {
                    accessory.PlayRejected();
                    accessory.ReturnTo(originalVisualPosition);
                }

                accessory.UpdateSortingOrder();
                ClearSelection();
            }
        }
    }
}
