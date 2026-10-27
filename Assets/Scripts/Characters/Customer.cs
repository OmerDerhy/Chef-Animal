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
        [Tooltip("Sitting scale as a fraction of this customer's own walking scale (not an absolute size) - so every species shrinks by the same proportion when they sit, regardless of how big their own art is.")]
        public float sittingScaleFactor = 0.8f;

        [Header("Eating & Reward Icons")]
        [Tooltip("Shown above the customer's head, breathing gently, while they eat.")]
        public Sprite eatingHeartSprite;
        [Tooltip("Popped up above the customer's head once they finish eating, as a reward cue.")]
        public Sprite rewardCoinSprite;
        [Tooltip("World-space size of the eating/reward icons.")]
        public Vector3 statusIconScale = new Vector3(0.20f, 0.20f, 1f);
        [Tooltip("How far above the customer's own position (world units) the icon sits.")]
        public float statusIconHeightOffset = 0.26f;

        private FloatingStatusIcon eatingHeart;

        private float eatingTimer = 0f;
        private Vector3 exitPosition;
        private Chair targetChair;
        public AccessoryVisual targetChairVisual { get; private set; } // Exposed so Staff knows where to deliver
        
        // originalScale is inherited from SortableVisual, which caches it in Awake.

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
                            HideEatingHeart();
                            FindAndMoveTo(targetChair.SeatPosition);
                            break;
                        }
                    }

                    if (eatingHeart != null) eatingHeart.SetPosition(StatusIconWorldPosition());

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

                // Pathfinding only walks to the nearest WALKABLE SUBGRID CELL CENTER, not the chair's
                // exact SeatPoint (which can sit anywhere within that cell) - snap the last, fine-grained
                // distance directly so the customer always ends up exactly on SeatPoint, not just close.
                if (targetChair != null)
                {
                    transform.position = targetChair.SeatPosition;
                }

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
                
                // Shrink to sitting scale, relative to THIS customer's own walking scale - a hardcoded
                // absolute size here shrank taller species (Penguin, Bear) far more than shorter ones.
                transform.localScale = originalScale * sittingScaleFactor;

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
                ShowEatingHeart();
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
            HideEatingHeart();
            ShowRewardCoin();

            // Restore original scale and rotation so the customer walks normally
            transform.localScale = originalScale;
            if (spriteRenderer != null)
            {
                spriteRenderer.transform.localEulerAngles = Vector3.zero;
            }

            currentState = CustomerState.PayingAndLeaving;
            FindAndMoveTo(exitPosition);
        }

        private Vector3 StatusIconWorldPosition()
        {
            return transform.position + Vector3.up * statusIconHeightOffset;
        }

        private void ShowEatingHeart()
        {
            if (eatingHeartSprite == null) return;

            HideEatingHeart(); // defensive: never leave a stray heart if this is somehow called twice
            eatingHeart = FloatingStatusIcon.Create(eatingHeartSprite, StatusIconWorldPosition(), statusIconScale);
            eatingHeart.PlayBreathing();
        }

        private void HideEatingHeart()
        {
            if (eatingHeart == null) return;
            eatingHeart.StopAndDestroy();
            eatingHeart = null;
        }

        private void ShowRewardCoin()
        {
            if (rewardCoinSprite == null) return;

            FloatingStatusIcon coin = FloatingStatusIcon.Create(rewardCoinSprite, StatusIconWorldPosition(), statusIconScale);
            coin.PlayRewardPopAndDestroy();
        }

        public override void SetStatusIconsVisible(bool visible)
        {
            if (eatingHeart != null) eatingHeart.SetVisible(visible);
        }

        private void OnDestroy()
        {
            HideEatingHeart();

            if (targetChair != null)
            {
                targetChair.IsOccupied = false;
                targetChair = null;
            }
        }
    }
}
