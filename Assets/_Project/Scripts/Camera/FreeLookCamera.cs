using UnityEngine;

/// <summary>
/// Controls first-person camera allowing free look
/// with realistic head rotation limits
/// </summary>
public class FreeLookCamera : MonoBehaviour
{
    [Header("=== Look Settings ===")]
    [Tooltip("Mouse sensitivity")]
    public float mouseSensitivity = 2f;
    
    [Tooltip("Upward look limit in degrees")]
    public float verticalLimitUp = 80f;
    
    [Tooltip("Downward look limit when facing forward in degrees")]
    public float verticalLimitDownFront = 80f;
    
    [Tooltip("Downward look limit when facing backward in degrees")]
    public float verticalLimitDownBack = 20f;
    
    [Tooltip("Horizontal rotation limit left/right in degrees")]
    public float horizontalLimit = 90f;  
    
    [Header("=== Smoothing ===")]
    [Tooltip("Enable camera movement smoothing")]
    public bool smoothMovement = true;
    
    [Tooltip("Smoothing interpolation speed")]
    public float smoothSpeed = 10f;
    
    [Header("=== Debug Info ===")]
    [SerializeField] private float rotationX = 0f;  // Vertical rotation (up/down)
    [SerializeField] private float rotationY = 0f;  // Horizontal rotation (left/right)
    
    // Target rotation for smooth interpolation
    private Quaternion targetRotation;
    
    void Start()
    {
        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Initialize target rotation with current rotation
        targetRotation = transform.localRotation;
    }
    
    void Update()
    {
        ProcessMouseInput();
        ApplyRotationLimits();
        UpdateCameraRotation();
    }
    
    /// <summary>
    /// Processes mouse input and accumulates rotations
    /// </summary>
    private void ProcessMouseInput()
    {
        // Get mouse movement on X and Y axes
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // Only process if there's significant movement
        if (Mathf.Abs(mouseX) > 0.01f || Mathf.Abs(mouseY) > 0.01f)
        {
            // Accumulate horizontal rotation (turn head left/right)
            rotationY += mouseX;
            rotationY = Mathf.Clamp(rotationY, -horizontalLimit, horizontalLimit);
            
            // Accumulate vertical rotation (look up/down)
            // Subtract because mouse Y axis is inverted
            rotationX -= mouseY;
        }
    }
    
    /// <summary>
    /// Applies dynamic vertical rotation limits based on horizontal rotation
    /// When looking backward the lower limit is reduced to avoid seeing the neck
    /// </summary>
    private void ApplyRotationLimits()
    {
        // Calculate how much we're turned backward (0 = forward, 1 = completely backward)
        float backwardFactor = Mathf.Abs(rotationY) / horizontalLimit;
        
        // Interpolate lower limit between front and back based on horizontal rotation
        float currentLowerLimit = Mathf.Lerp(
            verticalLimitDownFront, 
            verticalLimitDownBack, 
            backwardFactor
        );
        
        // Apply vertical limits
        rotationX = Mathf.Clamp(rotationX, -verticalLimitUp, currentLowerLimit);
    }
    
    /// <summary>
    /// Updates camera rotation with or without smoothing
    /// </summary>
    private void UpdateCameraRotation()
    {
        // Create target rotation quaternion
        targetRotation = Quaternion.Euler(rotationX, rotationY, 0f);
        
        if (smoothMovement)
        {
            // Apply smooth rotation using spherical interpolation
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                targetRotation,
                smoothSpeed * Time.deltaTime
            );
        }
        else
        {
            // Apply instant rotation
            transform.localRotation = targetRotation;
        }
    }
}