using UnityEngine;
using MergesChef.Map;

namespace MergesChef.Characters
{
    public enum AnimState
    {
        Idle,
        Walk,
        SitFront,
        SitBack
    }

    public abstract class Animal : SortableVisual
    {
        [Header("Animal Settings")]
        public float moveSpeed = 2f;
        public SpriteRenderer spriteRenderer;
        
        [Header("Animation Settings")]
        public Sprite[] idleSprites;
        public Sprite[] walkSprites;
        public Sprite[] sitBackSprites;
        public Sprite[] sitFrontSprites;
        public float animationSpeed = 0.15f;

        protected Vector3 targetPosition;
        protected bool isMoving = false;
        protected AnimState currentAnimState = AnimState.Idle;
        protected System.Collections.Generic.Queue<Vector3> currentPath = new System.Collections.Generic.Queue<Vector3>();
        
        protected MergesChef.Map.Map mapManager;

        private float animationTimer;
        private int currentFrame;

        protected virtual void Start()
        {
            mapManager = FindObjectOfType<MergesChef.Map.Map>();
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            targetPosition = transform.position;
        }

        public void SetAnimState(AnimState newState)
        {
            if (currentAnimState != newState)
            {
                currentAnimState = newState;
                currentFrame = 0;
                animationTimer = 0f;
            }
        }

        protected virtual void Update()
        {
            HandleMovement();
            HandleAnimation();
            UpdateLogic();
        }

        private void HandleAnimation()
        {
            if (spriteRenderer == null) return;

            Sprite[] currentAnim = idleSprites;

            switch (currentAnimState)
            {
                case AnimState.Walk: currentAnim = walkSprites; break;
                case AnimState.SitFront: currentAnim = sitFrontSprites; break;
                case AnimState.SitBack: currentAnim = sitBackSprites; break;
            }
            
            if (currentAnim == null || currentAnim.Length == 0) return;

            animationTimer += Time.deltaTime;
            if (animationTimer >= animationSpeed)
            {
                animationTimer = 0f;
                currentFrame = (currentFrame + 1) % currentAnim.Length;
                spriteRenderer.sprite = currentAnim[currentFrame];
            }
        }

        private void HandleMovement()
        {
            if (!isMoving) return;

            // Move towards target
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            // Check if we reached the target
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                
                if (currentPath != null && currentPath.Count > 0)
                {
                    // Continue to next point in the path
                    targetPosition = currentPath.Dequeue();
                }
                else
                {
                    isMoving = false;
                    
                    if (currentAnimState == AnimState.Walk)
                        SetAnimState(AnimState.Idle);
                        
                    OnReachedDestination();
                }
            }

            // Flip sprite based on movement direction (assuming left-facing sprite by default)
            // Adjust this if your sprites face right by default!
            if (targetPosition.x > transform.position.x + 0.01f)
            {
                spriteRenderer.flipX = true;
            }
            else if (targetPosition.x < transform.position.x - 0.01f)
            {
                spriteRenderer.flipX = false;
            }

            // Continuously update sorting order so it layers perfectly as it walks
            UpdateSortingOrder();
        }

        public void FindAndMoveTo(Vector3 targetWorldPosition)
        {
            if (mapManager != null)
            {
                var path = mapManager.FindPathWorld(transform.position, targetWorldPosition);
                
                // If path is valid AND actually reaches the target (or the target's subgrid), use it
                if (path != null && path.Count > 1)
                {
                    Vector3 pathEnd = path[path.Count - 1];
                    // 0.5f is roughly the size of a subgrid, so if it's within that, we successfully routed to it
                    if (Vector3.Distance(pathEnd, targetWorldPosition) < 0.6f)
                    {
                        MoveAlongPath(path);
                        return;
                    }
                }
            }
            
            // Fallback if pathfinding fails (e.g. completely boxed in) or mapManager is missing
            MoveTo(targetWorldPosition);
        }

        public void MoveTo(Vector3 position)
        {
            if (currentPath != null) currentPath.Clear();
            targetPosition = position;
            isMoving = true;
            SetAnimState(AnimState.Walk);
        }
        
        public void MoveAlongPath(System.Collections.Generic.List<Vector3> path)
        {
            if (path == null || path.Count == 0) return;
            
            if (currentPath != null) currentPath.Clear();
            foreach (var point in path)
            {
                currentPath.Enqueue(point);
            }
            
            targetPosition = currentPath.Dequeue();
            isMoving = true;
            SetAnimState(AnimState.Walk);
        }

        public override void UpdateSortingOrder()
        {
            // Characters are highly dynamic, so they use a base offset of 25000 to 
            // ensure they always render above flat floor tiles and properly interlace with accessories
            ApplySortingOrder(0, 0, 25000); 
        }

        // Abstract method for unique subclass logic (State Machine)
        protected abstract void UpdateLogic();

        // Called automatically when MoveTo finishes
        protected virtual void OnReachedDestination() { }
    }
}
