using UnityEngine;

/// <summary>
/// Collision detection and management system for the wheelchair
/// Processes CharacterController collisions and manages directional blocking and wall sliding
/// </summary>
public class CollisionSystem : MonoBehaviour
{
    [Header("=== Collision State (Debug) ===")]
    [SerializeField] private bool inCollision = false;
    [SerializeField] private string collidedObject = "";
    [SerializeField] private bool frontBlocked = false;
    [SerializeField] private bool backBlocked = false;
    [SerializeField] private bool wallSliding = false;

    // External components
    private CharacterController controller;
    private Transform wheelchairTransform;
    private CollisionFlashEffect flashEffect;

    // Collision variables
    private Vector3 collisionNormal = Vector3.zero;
    private Vector3 collisionPoint = Vector3.zero;
    private float collisionTime = 0f;
    private float lastCollisionTime = 0f;

    // Directional blocking system
    private float blockingTime = 0f;
    private const float blockingDuration = 0.2f;

    // Wall sliding system
    private Vector3 slideDirection = Vector3.zero;

    /// <summary>
    /// Initialize collision system with necessary references
    /// </summary>
    public void Initialize(CharacterController characterController, Transform transform)
    {
        this.controller = characterController;
        this.wheelchairTransform = transform;

        // Get or create flash component
        flashEffect = GetComponent<CollisionFlashEffect>();
        if (flashEffect == null)
        {
            flashEffect = gameObject.AddComponent<CollisionFlashEffect>();
        }
    }

    /// <summary>
    /// Update collision system
    /// </summary>
    public void Update()
    {
        UpdateBlockingTimer();
        UpdateCollisionReset();
    }

    /// <summary>
    /// Updates blocking timer and clears blocks when expired
    /// </summary>
    private void UpdateBlockingTimer()
    {
        if (blockingTime > 0)
        {
            blockingTime -= Time.deltaTime;
            if (blockingTime <= 0)
            {
                frontBlocked = false;
                backBlocked = false;
            }
        }
    }

    /// <summary>
    /// Automatically resets collision state after timeout
    /// </summary>
    private void UpdateCollisionReset()
    {
        if (inCollision && Time.time - collisionTime > 0.5f)
        {
            inCollision = false;
            collidedObject = "";
            wallSliding = false;
        }
    }

    /// <summary>
    /// Process CharacterController collisions
    /// </summary>
    public void ProcessCollision(ControllerColliderHit hit, float currentSpeed, ref float currentSpeedRef)
    {
        // Ignore floor
        if (IsFloorObject(hit.gameObject.name))
            return;

        // Avoid multiple detections
        if (Time.time - lastCollisionTime < 0.1f) 
            return;

        // Determine collision direction
        Vector3 dirToObstacle = (hit.point - wheelchairTransform.position);
        dirToObstacle.y = 0;
        dirToObstacle.Normalize();

        float angle = Vector3.Angle(wheelchairTransform.forward, dirToObstacle);

        // Process collision based on angle
        if (angle < 60f)
        {
            ProcessFrontCollision(currentSpeedRef);
        }
        else if (angle > 120f)
        {
            ProcessBackCollision(currentSpeedRef);
        }
        else
        {
            ProcessSideCollision(hit, dirToObstacle);
        }

        // Update collision state
        inCollision = true;
        collidedObject = hit.gameObject.name;
        collisionPoint = hit.point;
        collisionTime = Time.time;
        lastCollisionTime = Time.time;
        blockingTime = blockingDuration;
    }

    /// <summary>
    /// Processes front collision
    /// </summary>
    private void ProcessFrontCollision(float currentSpeedRef)
    {
        frontBlocked = true;
        currentSpeedRef = 0;
        
        if (flashEffect != null)
            flashEffect.FrontFlash();
    }

    /// <summary>
    /// Processes back collision
    /// </summary>
    private void ProcessBackCollision(float currentSpeedRef)
    {
        backBlocked = true;
        currentSpeedRef = 0;
        
        if (flashEffect != null)
            flashEffect.BackFlash();
    }

    /// <summary>
    /// Processes side collision and calculates slide direction
    /// </summary>
    private void ProcessSideCollision(ControllerColliderHit hit, Vector3 dirToObstacle)
    {
        collisionNormal = hit.normal;
        
        // Calculate tangential slide direction using vector projection
        Vector3 projection = Vector3.Project(wheelchairTransform.forward, collisionNormal);
        slideDirection = (wheelchairTransform.forward - projection).normalized;
        wallSliding = true;

        // Determine if left or right side
        float side = Vector3.Dot(wheelchairTransform.right, dirToObstacle);
        
        if (flashEffect != null)
        {
            if (side > 0)
                flashEffect.RightSideFlash();
            else
                flashEffect.LeftSideFlash();
        }
    }

    /// <summary>
    /// Checks if object name indicates it's a floor/ground
    /// </summary>
    private bool IsFloorObject(string objectName)
    {
        string name = objectName.ToLower();
        return name.Contains("plane") || name.Contains("ground") || name.Contains("floor");
    }

    /// <summary>
    /// Clear wall sliding state
    /// </summary>
    public void ClearSlide()
    {
        wallSliding = false;
        slideDirection = Vector3.zero;
    }

    // ===== PUBLIC PROPERTIES =====

    public bool IsFrontBlocked => frontBlocked && (Time.time - lastCollisionTime < blockingDuration);
    public bool IsBackBlocked => backBlocked && (Time.time - lastCollisionTime < blockingDuration);
    public bool IsWallSliding => wallSliding;
    public Vector3 SlideDirection => slideDirection;
    public bool IsInCollision => inCollision;
    public string CollidedObject => collidedObject;
}