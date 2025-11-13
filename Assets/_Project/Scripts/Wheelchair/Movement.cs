using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Main electric wheelchair movement controller
/// Responsible for: input, speed, acceleration, rotation and physics
/// </summary>
public class Movement : MonoBehaviour
{
    [Header("=== Speed Settings ===")]
    [Tooltip("Maximum speed in normal mode (km/h)")]
    public float maxSpeedNormal = 6f;

    [Tooltip("Maximum speed in slow/interior mode (km/h)")]
    public float maxSpeedSlow = 3f;

    [Tooltip("Reverse speed (km/h)")]
    public float reverseSpeed = 2f;

    [Header("=== Acceleration Settings ===")]
    [Tooltip("Time to reach maximum speed (seconds)")]
    public float accelerationTime = 2f;

    [Tooltip("Time to stop completely (seconds)")]
    public float brakingTime = 1.5f;

    [Header("=== Rotation Settings ===")]
    [Tooltip("Rotation speed (degrees per second)")]
    public float rotationSpeed = 45f;

    [Tooltip("Can rotate without moving forward/backward? (Only works with front steering)")]
    public bool rotationInPlace = false;

    [Header("=== Driving Modes ===")]
    [Tooltip("Current speed mode")]
    public SpeedMode currentMode = SpeedMode.Normal;

    [Header("=== Effect Sounds (One-Shot) ===")]
    [Tooltip("Audio launcher for short effects (clicks, collisions)")]
    public AudioSource effectsAudio;

    [Tooltip("Sound to play when changing speed mode (keys 1, 2)")]
    public AudioClip modeChangeSound;

    [Tooltip("Sound to play when changing steering type (key T)")]
    public AudioClip steeringChangeSound;

    [Tooltip("Sound to play when hitting hard")]
    public AudioClip hardCollisionSound;

    [Tooltip("Sound to play when starting to slide on wall")]
    public AudioClip slideStartSound;

    [Tooltip("Minimum speed (in m/s) for collision sound to play (optional)")]
    public float minCollisionSpeed = 0.8f;

    [Header("=== Physics and Limits ===")]
    [Tooltip("Maximum slope it can climb (degrees)")]
    public float maxSlope = 10f;

    [Tooltip("Applied gravity")]
    public float gravity = -9.81f;

    [Header("=== Current State (Debug) ===")]
    [SerializeField] private float currentSpeed = 0f;
    [SerializeField] private float targetSpeed = 0f;
    [SerializeField] private bool emergencyBrake = false;
    [SerializeField] private string currentSteeringType = "Frontal";
    [SerializeField] private float rotationEfficiency = 100f;

    // Components
    private CharacterController controller;
    private Vector3 movementVelocity;
    private WheelController wheelController;
    private CollisionSystem collisionSystem;

    // Smoothed input system
    private float smoothedVerticalInput = 0f;
    private float smoothedHorizontalInput = 0f;

    // Rear steering - feedback
    private bool tryingToTurnStationary = false;
    private float tryingToTurnTime = 0f;

    // Public variable for sound script to know if player is accelerating
    [HideInInspector]
    public bool playerIsAccelerating = false;

    // Cache for sounds (to not repeat)
    private bool slidingCache = false;
    private string steeringTypeCache = "Frontal";
    private bool inCollisionCache = false;

    public enum SpeedMode
    {
        Slow,
        Normal,
        Off
    }

    void Start()
    {
        SetupCharacterController();
        SetupComponents();
        ConvertSpeeds();
        InitializeCache();
    }

    /// <summary>
    /// Configures CharacterController with optimized values for realistic contact
    /// </summary>
    private void SetupCharacterController()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
        }

        // Optimized values for realistic obstacle contact
        controller.height = 1.4f;
        controller.radius = 0.2f;
        controller.center = new Vector3(0, 0.7f, 0);
        controller.skinWidth = 0.0001f;
        controller.minMoveDistance = 0.0f;
        controller.stepOffset = 0.1f;

        // Slightly elevate at start to avoid floor collision
        transform.position += Vector3.up * 0.1f;
    }

    /// <summary>
    /// Initializes references to necessary components
    /// </summary>
    private void SetupComponents()
    {
        wheelController = GetComponent<WheelController>();

        collisionSystem = GetComponent<CollisionSystem>();
        if (collisionSystem == null)
        {
            collisionSystem = gameObject.AddComponent<CollisionSystem>();
        }
        collisionSystem.Initialize(controller, transform);
    }

    /// <summary>
    /// Converts speeds from km/h to m/s (internally used format)
    /// </summary>
    private void ConvertSpeeds()
    {
        maxSpeedNormal = maxSpeedNormal / 3.6f;
        maxSpeedSlow = maxSpeedSlow / 3.6f;
        reverseSpeed = reverseSpeed / 3.6f;
    }

    /// <summary>
    /// Initializes cache values for state change detection
    /// </summary>
    private void InitializeCache()
    {
        if (wheelController != null)
        {
            steeringTypeCache = wheelController.GetSteeringType().ToString();
        }
    }

    void Update()
    {
        UpdateSteeringState();
        collisionSystem.Update();
        ProcessSoundEffects();
        UpdateTimers();

        ManageModes();

        if (currentMode != SpeedMode.Off)
        {
            ProcessRealisticInput();
            ApplyRealisticMovement();
        }
        else
        {
            EmergencyStop();
            ApplyVerticalMovement();
        }

        ApplyGravity();
    }

    /// <summary>
    /// Updates current steering type for reference
    /// </summary>
    private void UpdateSteeringState()
    {
        if (wheelController != null)
        {
            currentSteeringType = wheelController.GetSteeringType().ToString();
        }
    }

    /// <summary>
    /// Processes and plays sound effects based on state changes
    /// </summary>
    private void ProcessSoundEffects()
    {
        // Steering change sound
        if (currentSteeringType != steeringTypeCache)
        {
            PlaySound(steeringChangeSound);
            steeringTypeCache = currentSteeringType;
        }

        // Slide start sound
        bool slidingNow = collisionSystem.IsWallSliding;
        if (slidingNow && !slidingCache)
        {
            PlaySound(slideStartSound);
        }
        slidingCache = slidingNow;

        // Collision sound (sliding has priority)
        bool inCollisionNow = (collisionSystem.IsInCollision || 
                               collisionSystem.IsFrontBlocked || 
                               collisionSystem.IsBackBlocked);

        if (slidingNow)
        {
            inCollisionNow = false;
        }

        if (inCollisionNow && !inCollisionCache)
        {
            PlaySound(hardCollisionSound);
        }

        inCollisionCache = inCollisionNow;
    }

    /// <summary>
    /// Updates warning and feedback timers
    /// </summary>
    private void UpdateTimers()
    {
        if (tryingToTurnTime > 0)
        {
            tryingToTurnTime -= Time.deltaTime;
        }
    }

    /// <summary>
    /// Manages speed mode switching through keyboard input
    /// </summary>
    void ManageModes()
    {
        // Key 1: Slow Mode
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            currentMode = SpeedMode.Slow;
            PlaySound(modeChangeSound);
        }
        // Key 2: Normal Mode
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            currentMode = SpeedMode.Normal;
            PlaySound(modeChangeSound);
        }
        // Space: Emergency brake
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            currentMode = SpeedMode.Off;
            emergencyBrake = true;
        }
        // Release space: Return to normal mode
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            currentMode = SpeedMode.Normal;
            emergencyBrake = false;
        }
    }

    /// <summary>
    /// Processes player input applying smoothing and realistic physics
    /// </summary>
    void ProcessRealisticInput()
    {
        // Get player input
        float verticalInput = Input.GetAxis("Vertical");
        float horizontalInput = Input.GetAxis("Horizontal");

        // Update acceleration flag for sound system
        playerIsAccelerating = (Mathf.Abs(verticalInput) > 0.1f);

        // Apply input smoothing
        float smoothing = 3f;
        smoothedVerticalInput = Mathf.Lerp(smoothedVerticalInput, verticalInput, smoothing * Time.deltaTime);
        smoothedHorizontalInput = Mathf.Lerp(smoothedHorizontalInput, horizontalInput, smoothing * Time.deltaTime);

        // Determine maximum speed based on mode
        float maxSpeed = currentMode == SpeedMode.Slow ?
                         maxSpeedSlow : maxSpeedNormal;

        // Apply realistic collision blocking system
        ApplyCollisionBlocking(ref verticalInput, ref maxSpeed);

        // Calculate acceleration and deceleration
        ApplyAccelerationDeceleration(maxSpeed);

        // Process rotation
        ProcessRotation(smoothedHorizontalInput);
    }

    /// <summary>
    /// Applies movement blocking based on detected collisions
    /// </summary>
    private void ApplyCollisionBlocking(ref float verticalInput, ref float maxSpeed)
    {
        // Front blocking - prevents forward movement
        if (collisionSystem.IsFrontBlocked && smoothedVerticalInput > 0)
        {
            smoothedVerticalInput = 0;
            targetSpeed = 0;

            // Apply small pushback if trying to force
            if (verticalInput > 0.5f)
            {
                currentSpeed = Mathf.Max(currentSpeed - 0.5f * Time.deltaTime, -0.05f);
            }
        }
        // Back blocking - prevents reverse
        else if (collisionSystem.IsBackBlocked && smoothedVerticalInput < 0)
        {
            smoothedVerticalInput = 0;
            targetSpeed = 0;
        }
        // Normal movement when not blocked
        else
        {
            // Use reverse speed when input is negative
            if (smoothedVerticalInput < 0)
            {
                maxSpeed = reverseSpeed;
            }

            targetSpeed = smoothedVerticalInput * maxSpeed;
        }
    }

    /// <summary>
    /// Applies gradual acceleration or deceleration based on current state
    /// </summary>
    private void ApplyAccelerationDeceleration(float maxSpeed)
    {
        bool notBlocked = !collisionSystem.IsFrontBlocked && !collisionSystem.IsBackBlocked;
        bool accelerating = Mathf.Abs(targetSpeed) > Mathf.Abs(currentSpeed);

        if (notBlocked && accelerating)
        {
            // Gradual acceleration
            float acceleration = maxSpeed / accelerationTime;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
        }
        else
        {
            // Gradual deceleration
            float deceleration = maxSpeed / brakingTime;

            if (collisionSystem.IsFrontBlocked || collisionSystem.IsBackBlocked)
            {
                // Stop immediately if blocked
                currentSpeed = 0;
            }
            else if (collisionSystem.IsInCollision)
            {
                // Faster deceleration in collision
                deceleration *= 2f;
                currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, deceleration * Time.deltaTime);
            }
            else
            {
                // Normal deceleration
                currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, deceleration * Time.deltaTime);
            }
        }
    }

    /// <summary>
    /// Processes wheelchair rotation based on steering type and speed
    /// </summary>
    void ProcessRotation(float horizontalInput)
    {
        float rotationMultiplier = 1f;
        bool isRearSteering = false;
        rotationEfficiency = 100f;

        // Check steering type
        if (wheelController != null)
        {
            isRearSteering = wheelController.GetSteeringType() == WheelController.SteeringType.RearSteering;

            if (isRearSteering)
            {
                rotationMultiplier = 1.3f; // Rear steering turns more easily
            }
        }

        bool isStationary = Mathf.Abs(currentSpeed) < 0.1f;

        if (isRearSteering)
        {
            ProcessRearRotation(isStationary, horizontalInput, ref rotationMultiplier);
        }
        else
        {
            ProcessFrontRotation(isStationary, ref rotationMultiplier);
        }

        // Apply calculated rotation
        float rotation = horizontalInput * rotationSpeed * rotationMultiplier * Time.deltaTime;
        transform.Rotate(0, rotation, 0);
    }

    /// <summary>
    /// Processes rotation logic specific to rear steering
    /// </summary>
    private void ProcessRearRotation(bool isStationary, float horizontalInput, ref float multiplier)
    {
        if (isStationary)
        {
            // Rear steering doesn't allow stationary rotation
            rotationEfficiency = 0f;

            if (Mathf.Abs(horizontalInput) > 0.1f)
            {
                tryingToTurnStationary = true;
                tryingToTurnTime = 1f;
            }

            multiplier = 0f;
        }
        else
        {
            tryingToTurnStationary = false;
            float normalizedSpeed = Mathf.Abs(currentSpeed) / maxSpeedNormal;
            float baseEfficiency = Mathf.Lerp(0.2f, 1f, normalizedSpeed);
            multiplier *= baseEfficiency;

            if (currentSpeed < 0)
            {
                // In reverse rotation is inverted and less efficient
                multiplier *= -0.8f;
                rotationEfficiency = baseEfficiency * 80f;
            }
            else
            {
                rotationEfficiency = baseEfficiency * 100f;
            }
        }
    }

    /// <summary>
    /// Processes rotation logic specific to front steering
    /// </summary>
    private void ProcessFrontRotation(bool isStationary, ref float multiplier)
    {
        tryingToTurnStationary = false;

        if (isStationary && !rotationInPlace)
        {
            // No rotation when stationary (unless rotationInPlace is active)
            rotationEfficiency = 0f;
            multiplier = 0f;
        }
        else if (isStationary && rotationInPlace)
        {
            // Facilitated rotation when stationary
            multiplier *= 1.5f;
            rotationEfficiency = 100f;
        }
        else
        {
            // Rotation slightly increases with speed
            float normalizedSpeed = Mathf.Abs(currentSpeed) / maxSpeedNormal;
            multiplier *= (1f + normalizedSpeed * 0.2f);
            rotationEfficiency = 100f;
        }
    }

    /// <summary>
    /// Applies calculated movement to CharacterController including sliding
    /// </summary>
    void ApplyRealisticMovement()
    {
        Vector3 movementDirection = Vector3.zero;

        // Use slide direction if sliding on wall
        if (collisionSystem.IsWallSliding && collisionSystem.SlideDirection != Vector3.zero)
        {
            movementDirection = collisionSystem.SlideDirection * Mathf.Abs(currentSpeed) * 0.5f;
        }
        else
        {
            movementDirection = transform.forward * currentSpeed;
        }

        // Preserve vertical movement (gravity)
        movementDirection.y = movementVelocity.y;

        controller.Move(movementDirection * Time.deltaTime);
    }

    /// <summary>
    /// Applies only vertical movement (used when in off mode)
    /// </summary>
    void ApplyVerticalMovement()
    {
        Vector3 verticalMovement = new Vector3(0, movementVelocity.y, 0);
        controller.Move(verticalMovement * Time.deltaTime);
    }

    /// <summary>
    /// Applies gravity to vertical movement
    /// </summary>
    void ApplyGravity()
    {
        if (controller.isGrounded)
        {
            // Small downward force to keep on ground
            movementVelocity.y = -0.5f;
        }
        else
        {
            // Accumulate fall velocity
            movementVelocity.y += gravity * Time.deltaTime;

            // Limit maximum fall speed
            movementVelocity.y = Mathf.Max(movementVelocity.y, -20f);
        }
    }

    /// <summary>
    /// Completely stops the wheelchair (emergency brake)
    /// </summary>
    void EmergencyStop()
    {
        currentSpeed = 0;
        targetSpeed = 0;

        collisionSystem.ClearSlide();

        if (wheelController != null)
        {
            wheelController.StopWheels();
        }
    }

    /// <summary>
    /// Unity callback when CharacterController collides with objects
    /// Delegates processing to collision system
    /// </summary>
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        collisionSystem.ProcessCollision(hit, currentSpeed, ref currentSpeed);
    }

    // ===== PUBLIC METHODS =====

    /// <summary>
    /// Returns normalized current speed (0-1) based on normal maximum speed
    /// </summary>
    public float GetNormalizedSpeed()
    {
        return currentSpeed / maxSpeedNormal;
    }

    /// <summary>
    /// Checks if wheelchair is moving
    /// </summary>
    public bool IsMoving()
    {
        return Mathf.Abs(currentSpeed) > 0.1f;
    }

    /// <summary>
    /// Reduces current speed by a multiplier
    /// </summary>
    public void ReduceSpeed(float multiplier)
    {
        currentSpeed *= multiplier;
    }

    /// <summary>
    /// Plays an effect sound through the effects AudioSource
    /// </summary>
    public void PlaySound(AudioClip clip)
    {
        if (effectsAudio != null && clip != null)
        {
            effectsAudio.PlayOneShot(clip);
        }
    }

    // ===== GRAPHICAL INTERFACE =====

    void OnGUI()
    {
        if (!Application.isEditor) return;

        // Movement info
        GUI.color = new Color(0, 0, 0, 0.8f);
        GUI.Box(new Rect(10, 100, 250, 110), "");

        GUI.color = Color.white;
        GUI.Label(new Rect(15, 105, 240, 20), "=== CADEIRA DE RODAS ===");
        GUI.Label(new Rect(15, 125, 240, 20), $"Modo: {currentMode}");
        GUI.Label(new Rect(15, 145, 240, 20), $"Velocidade: {(currentSpeed * 3.6f):F1} / {(currentMode == SpeedMode.Slow ? 3 : 6)} km/h");
        string steeringSimple = currentSteeringType.Contains("Rear") ? "Traseira" : "Frontal";
        GUI.Label(new Rect(15, 165, 240, 20), $"Direção: {steeringSimple}");

        // State (uses collision system)
        string state = "Normal";
        if (collisionSystem.IsWallSliding) state = "Deslizar";
        else if (collisionSystem.IsInCollision || collisionSystem.IsFrontBlocked || collisionSystem.IsBackBlocked)
            state = "Colisão";

        // Yellow for sliding, red for collision, green for normal
        if (collisionSystem.IsWallSliding) GUI.color = Color.yellow;
        else if (collisionSystem.IsInCollision || collisionSystem.IsFrontBlocked || collisionSystem.IsBackBlocked)
            GUI.color = Color.red;
        else GUI.color = Color.green;

        GUI.Label(new Rect(15, 185, 240, 20), $"Estado: {state}");
        GUI.color = Color.white;

        // Emergency brake
        if (emergencyBrake)
        {
            GUI.color = new Color(1, 0, 0, 0.9f);
            GUI.Box(new Rect(10, 220, 250, 35), "");
            GUI.color = Color.red;
            GUI.Label(new Rect(15, 228, 240, 20), "TRAVÃO DE EMERGÊNCIA ATIVO!");
            GUI.color = Color.white;
        }

        // Controls
        int controlsYPos = emergencyBrake ? 265 : 220;

        GUI.color = new Color(0, 0.5f, 0, 0.8f);
        GUI.Box(new Rect(10, controlsYPos, 250, 95), "");
        GUI.color = Color.white;
        GUI.Label(new Rect(15, controlsYPos + 5, 240, 20), "=== CONTROLOS ===");
        GUI.Label(new Rect(15, controlsYPos + 25, 240, 20), "WASD/Setas - Mover");
        GUI.Label(new Rect(15, controlsYPos + 42, 240, 20), "1/2 - Modo Lento/Normal");
        GUI.Label(new Rect(15, controlsYPos + 59, 240, 20), "T - Alternar direção");
        GUI.Label(new Rect(15, controlsYPos + 76, 240, 20), "ESPAÇO - Travão");
    }
}