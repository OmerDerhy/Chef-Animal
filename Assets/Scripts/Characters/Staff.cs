using UnityEngine;

namespace MergesChef.Characters
{
    public enum StaffState
    {
        Idle,
        WalkingToKitchen,
        Cooking,
        WalkingToTable,
        Serving,
        WalkingToKitchenForPickup,
        DeliveringFood,
        WanderingTest
    }

    public class Staff : Animal
    {
        public StaffState currentState = StaffState.Idle;
        
        [Header("Staff Settings")]
        public float cookingDuration = 3f;

        [Tooltip("Where the dish should be held (X, Y). X will flip automatically when they turn around.")]
        private Vector2 dishHoldOffset = new Vector2(2f, -0.5f); // Default guess for the 'hand' position

        [Header("Staff Marker Icon")]
        [Tooltip("Shown above this staff member's head at all times, twinkling gently, to mark them as staff.")]
        public Sprite starSprite;
        [Tooltip("World-space size of the star icon.")]
        public Vector3 statusIconScale = new Vector3(0.20f, 0.20f, 1f);
        [Tooltip("How far above the staff's own position (world units) the icon sits.")]
        public float statusIconHeightOffset = 0.35f;

        private FloatingStatusIcon staffStar;

        private float cookingTimer = 0f;
        private Customer targetCustomer;
        private Vector3 kitchenPosition;
        private MergesChef.Map.KitchenAccessoryVisual targetKitchen;

        // Held dish sprite — created automatically ßat runtime
        private GameObject dishHolderObj;
        private SpriteRenderer heldDishRenderer;
        
        // Test variables
        private float wanderWaitTimer = 0f;
        private float wanderWaitDuration = 2f;

        protected override void Start()
        {
            base.Start();

            // Create the held-dish sprite renderer as a child automatically
            dishHolderObj = new GameObject("HeldDish");
            dishHolderObj.transform.SetParent(transform);
            dishHolderObj.transform.localScale = new Vector3(0.35f, 0.35f, 1f); // Make the dish sprite much smaller
            heldDishRenderer = dishHolderObj.AddComponent<SpriteRenderer>();
            heldDishRenderer.sortingLayerName = spriteRenderer != null ? spriteRenderer.sortingLayerName : "Default";
            heldDishRenderer.sortingOrder = (spriteRenderer != null ? spriteRenderer.sortingOrder : 0) + 1;
            heldDishRenderer.enabled = false;

            ShowStaffStar();

            currentState = StaffState.WanderingTest;
            PickRandomWanderTarget();
        }

        private Vector3 StatusIconWorldPosition()
        {
            return transform.position + Vector3.up * statusIconHeightOffset;
        }

        private void ShowStaffStar()
        {
            if (starSprite == null) return;
            staffStar = FloatingStatusIcon.Create(starSprite, StatusIconWorldPosition(), statusIconScale);
            staffStar.PlayTwinkle();
        }

        private void PickRandomWanderTarget()
        {
            if (mapManager == null)
            {
                Vector3 randomOffset = new Vector3(Random.Range(-3f, 3f), Random.Range(-3f, 3f), 0);
                FindAndMoveTo(transform.position + randomOffset);
                return;
            }

            var validPositions = new System.Collections.Generic.List<Vector2Int>(mapManager.GetAllAreaPositions());
            if (validPositions.Count > 0)
            {
                Vector2Int randomTarget = validPositions[Random.Range(0, validPositions.Count)];
                Vector3 targetWorld = mapManager.GridToWorldPosition(randomTarget);
                FindAndMoveTo(targetWorld);
                return;
            }

            // If we couldn't find a path, just stay idle until the next update
            isMoving = false;
            SetAnimState(AnimState.Idle);
        }

        protected override void UpdateLogic()
        {
            // The star marker follows the staff member regardless of what they're doing.
            if (staffStar != null) staffStar.SetPosition(StatusIconWorldPosition());

            // Update the dish position so it stays in the hand and flips when they turn left/right!
            if (dishHolderObj != null && spriteRenderer != null)
            {
                // In Animal.cs, flipX = true means moving right. 
                // Adjust the sign below if it looks backwards on your specific sprites!
                float xOffset = spriteRenderer.flipX ? dishHoldOffset.x : -dishHoldOffset.x;
                dishHolderObj.transform.localPosition = new Vector3(xOffset, dishHoldOffset.y, 0);
            }

            switch (currentState)
            {
                case StaffState.WanderingTest:
                    if (!isMoving)
                    {
                        wanderWaitTimer += Time.deltaTime;
                        if (wanderWaitTimer >= wanderWaitDuration)
                        {
                            wanderWaitTimer = 0f;
                            PickRandomWanderTarget();
                        }
                    }
                    break;
                    
                case StaffState.Idle:
                    break;
                    
                case StaffState.WalkingToKitchen:
                    break;
                    
                case StaffState.Cooking:
                    cookingTimer += Time.deltaTime;
                    if (cookingTimer >= cookingDuration)
                    {
                        FinishCooking();
                    }
                    break;

                case StaffState.WalkingToKitchenForPickup:
                    // Handled by base movement
                    break;

                case StaffState.DeliveringFood:
                    // Keep walking target synced with the customer's chair (in case it moved)
                    if (targetCustomer != null && targetCustomer.targetChairVisual != null)
                    {
                        if (!isMoving && Vector3.Distance(transform.position, targetCustomer.targetChairVisual.transform.position) > 0.1f)
                        {
                            FindAndMoveTo(targetCustomer.targetChairVisual.transform.position);
                        }
                    }
                    break;

                case StaffState.WalkingToTable:
                    break;
            }
        }

        protected override void OnReachedDestination()
        {
            if (currentState == StaffState.WalkingToKitchen)
            {
                currentState = StaffState.Cooking;
                cookingTimer = 0f;
                Debug.Log($"{gameObject.name} arrived at kitchen and is cooking.");
            }
            else if (currentState == StaffState.WalkingToKitchenForPickup)
            {
                // Grab the dish from the kitchen tool
                if (targetKitchen != null)
                {
                    MergesChef.Map.Dish dish = targetKitchen.TakeDish();
                    if (dish != null && heldDishRenderer != null)
                    {
                        heldDishRenderer.sprite = dish.icon;
                        heldDishRenderer.enabled = true;
                        Debug.Log($"{gameObject.name} picked up {dish.dishName}. Heading to customer.");
                    }
                }

                // Walk to the customer's chair
                if (targetCustomer != null)
                {
                    currentState = StaffState.DeliveringFood;
                    Vector3 dest = targetCustomer.targetChairVisual != null
                        ? targetCustomer.targetChairVisual.transform.position
                        : targetCustomer.transform.position;
                    FindAndMoveTo(dest);
                }
                else
                {
                    ReturnToWander();
                }
            }
            else if (currentState == StaffState.DeliveringFood)
            {
                // Hide the dish sprite and serve the food
                if (heldDishRenderer != null) heldDishRenderer.enabled = false;
                ServeFood();
            }
            else if (currentState == StaffState.WalkingToTable)
            {
                currentState = StaffState.Serving;
                ServeFood();
            }
        }

        /// <summary>Called by RestaurantManager when a dish is ready and this staff should deliver it.</summary>
        public void PickUpDish(MergesChef.Map.KitchenAccessoryVisual kitchen, Customer customer)
        {
            targetKitchen = kitchen;
            targetCustomer = customer;
            currentState = StaffState.WalkingToKitchenForPickup;
            isMoving = false; // Stop wandering immediately
            currentPath?.Clear();
            FindAndMoveTo(kitchen.transform.position);
            Debug.Log($"{gameObject.name} heading to kitchen to pick up dish.");
        }

        // Call this when you want the staff to start a new order
        public void TakeOrder(Customer customer, Vector3 kitchenPos, Vector3 customerPos)
        {
            targetCustomer = customer;
            kitchenPosition = kitchenPos;
            
            Debug.Log($"{gameObject.name} took an order and is walking to kitchen.");
            currentState = StaffState.WalkingToKitchen;
            FindAndMoveTo(kitchenPosition);
        }

        private void FinishCooking()
        {
            Debug.Log($"{gameObject.name} finished cooking. Walking to customer.");
            currentState = StaffState.WalkingToTable;
            
            if (targetCustomer != null)
                FindAndMoveTo(targetCustomer.transform.position);
        }

        private void ServeFood()
        {
            if (targetCustomer != null)
            {
                targetCustomer.ReceiveFood();
                targetCustomer = null;
            }
            targetKitchen = null;
            
            Debug.Log($"{gameObject.name} served the food. Returning to wander.");
            ReturnToWander();
        }

        private void ReturnToWander()
        {
            currentState = StaffState.WanderingTest;
            wanderWaitTimer = 0f;
            PickRandomWanderTarget();
        }

        public override void SetStatusIconsVisible(bool visible)
        {
            if (staffStar != null) staffStar.SetVisible(visible);
        }

        /// <summary>
        /// Staff render BEHIND kitchen/restaurant accessories (AccessoryVisual's band, 20000) while
        /// still staying above the floor/walls (AreaVisual's band, ~16000) - a flat band of its own,
        /// below Animal's default (25000, which Customer keeps using so customers still draw in front
        /// of tables/chairs as before). Real world-Y only matters within a band, so this is purely
        /// about which band Staff competes in, not where they stand.
        /// </summary>
        public override void UpdateSortingOrder()
        {
            ApplySortingOrder(0, 0, 18000);
        }

        private void OnDestroy()
        {
            if (staffStar != null)
            {
                staffStar.StopAndDestroy();
                staffStar = null;
            }
        }
    }
}
