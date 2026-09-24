using UnityEngine;
using MergesChef.Map;

namespace MergesChef.Characters
{
    public enum CustomerState
    {
        Entering,
        LookingForSeat,
        WalkingToSeat,
        WaitingForFood,
        Eating,
        PayingAndLeaving,
        WanderingTest // Added for testing
    }

    public class Customer : Animal
    {
        public CustomerState currentState = CustomerState.Entering;
        
        [Header("Customer Settings")]
        public float eatingDuration = 5f;
        public int coinReward = 15;
        public bool isFoodOnTheWay = false;

        private float eatingTimer = 0f;
        private Vector3 exitPosition;
        private Chair targetChair;
        public AccessoryVisual targetChairVisual { get; private set; } // Exposed so Staff knows where to deliver
        
        private Vector3 originalScale;

        // Test variables
        private float wanderWaitTimer = 0f;
        private float wanderWaitDuration = 2f;

        protected override void Start()
        {
            base.Start();
            originalScale = transform.localScale; // remember walking scale
            exitPosition = transform.position;
            
            currentState = CustomerState.LookingForSeat;
        }

        private void PickRandomWanderTarget()
        {
            // Pick a random point nearby
            Vector3 randomOffset = new Vector3(Random.Range(-3f, 3f), Random.Range(-3f, 3f), 0);
            FindAndMoveTo(transform.position + randomOffset);
        }

        protected override void UpdateLogic()
        {
            switch (currentState)
            {
                case CustomerState.LookingForSeat:
                    AccessoryVisual[] accessories = FindObjectsOfType<AccessoryVisual>();
                    foreach (var acc in accessories)
                    {
                        if (acc.LinkedAccessory is Chair chair && !chair.IsOccupied)
                        {
                            targetChair = chair;
                            targetChair.IsOccupied = true;
                            targetChairVisual = acc;
                            currentState = CustomerState.WalkingToSeat;
                            FindAndMoveTo(chair.SeatPosition); // walk to the SeatPoint child on the prefab
                            break;
                        }
                    }
                    
                    // If no chair found, fall back to wandering
                    if (currentState == CustomerState.LookingForSeat)
                    {
                        currentState = CustomerState.WanderingTest;
                        PickRandomWanderTarget();
                    }
                    break;
                    
                case CustomerState.WanderingTest:
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



                case CustomerState.WalkingToSeat:
                    // Keep target synced to SeatPoint so we don't miss if the chair moves.
                    if (targetChair != null && targetChair.VisualModel != null)
                    {
                        if (!isMoving && Vector3.Distance(transform.position, targetChair.SeatPosition) > 0.1f)
                        {
                            FindAndMoveTo(targetChair.SeatPosition);
                        }
                    }
                    break;

                case CustomerState.WaitingForFood:
                    // If the chair moves while waiting, walk to the new seat position.
                    if (targetChair != null && targetChair.VisualModel != null)
                    {
                        if (!isMoving && Vector3.Distance(transform.position, targetChair.SeatPosition) > 0.1f)
                        {
                            currentState = CustomerState.WalkingToSeat;
                            FindAndMoveTo(targetChair.SeatPosition);
                        }
                    }
                    break;

                case CustomerState.Eating:
                    // If the chair moves while eating, follow it.
                    if (targetChair != null && targetChair.VisualModel != null)
                    {
                        if (!isMoving && Vector3.Distance(transform.position, targetChair.SeatPosition) > 0.1f)
                        {
                            currentState = CustomerState.WalkingToSeat;
                            FindAndMoveTo(targetChair.SeatPosition);
                            break;
                        }
                    }

                    eatingTimer += Time.deltaTime;
                    if (eatingTimer >= eatingDuration)
                    {
                        FinishEating();
                    }
                    break;

                case CustomerState.PayingAndLeaving:
                    // Walk off screen, handled by base movement.
                    break;
            }
        }

        protected override void OnReachedDestination()
        {
            if (currentState == CustomerState.WalkingToSeat)
            {
                currentState = CustomerState.WaitingForFood;
                
                // NOTE: Sprites in the Inspector are assigned swapped:
                //   sitBackSprites = actual FRONT sprites
                //   sitFrontSprites = actual BACK sprites
                bool isFacingFront = targetChair != null && targetChair.IsFacingFront;
                if (isFacingFront)
                {
                    SetAnimState(AnimState.SitBack);  // holds actual FRONT sprites
                }
                else
                {
                    SetAnimState(AnimState.SitFront); // holds actual BACK sprites
                }

                // Match the customer's Y rotation to the chair's visual rotation
                // so the customer always faces the same direction as the chair.
                // Also reset flipX since the walk direction must not affect the sitting pose.
                if (targetChair != null && targetChair.VisualModel != null && spriteRenderer != null)
                {
                    spriteRenderer.flipX = false; // clear walk-direction flip
                    float chairYRot = targetChair.VisualModel.transform.eulerAngles.y;
                    spriteRenderer.transform.localEulerAngles = new Vector3(0f, chairYRot, 0f);
                }
                
                // Shrink to sitting scale
                transform.localScale = new Vector3(0.04f, 0.04f, 0.04f);

                Debug.Log($"{gameObject.name} sat down and is waiting for food.");
            }
            else if (currentState == CustomerState.PayingAndLeaving)
            {
                Debug.Log($"{gameObject.name} left the restaurant.");
                if (CustomerManager.Instance != null)
                {
                    CustomerManager.Instance.OnCustomerLeft();
                }
                Destroy(gameObject);
            }
        }

        // Called by Staff when they deliver the food
        public void ReceiveFood()
        {
            if (currentState == CustomerState.WaitingForFood)
            {
                Debug.Log($"{gameObject.name} received food and is now eating!");
                currentState = CustomerState.Eating;
                eatingTimer = 0f;
            }
        }

        private void FinishEating()
        {
            if (targetChair != null)
            {
                targetChair.IsOccupied = false;
                targetChair = null;
            }

            Debug.Log($"{gameObject.name} finished eating. Dropping {coinReward} coins.");
            // Instantiate coins here...

            // Restore original scale and rotation so the customer walks normally
            transform.localScale = originalScale;
            if (spriteRenderer != null)
            {
                spriteRenderer.transform.localEulerAngles = Vector3.zero;
            }

            currentState = CustomerState.PayingAndLeaving;
            FindAndMoveTo(exitPosition);
        }
        private void OnDestroy()
        {
            if (targetChair != null)
            {
                targetChair.IsOccupied = false;
                targetChair = null;
            }
        }
    }
}
