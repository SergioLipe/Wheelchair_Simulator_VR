using UnityEngine;

/// <summary>
/// Controls dummy positioning and pose in the wheelchair
/// Allows adjusting body tilt, arm positions, wrists and fingers through sliders
/// </summary>
public class DummyPositioner : MonoBehaviour
{
    [Header("=== Body Adjustments ===")]
    [Range(-45, 45)]
    [Tooltip("Back tilt forward/backward in degrees")]
    public float backTilt = -20f;
    
    [Range(0, 120)]
    [Tooltip("Knee bend angle in degrees")]
    public float kneeBend = 90f;
    
    [Range(-30, 30)]
    [Tooltip("Lateral leg spread in degrees")]
    public float legSpread = 10f;
    
    [Header("=== ▼ LEFT SIDE ▼ ===")]
    [Space(5)]
    [Header("Left Arm")]
    [Range(-180, 180)]
    [Tooltip("Open/close arm - negative values close the arm")]
    public float leftArmSpread = 0f;
    
    [Range(-90, 90)]
    [Tooltip("Raise/lower arm vertically")]
    public float leftArmRaise = 30f;
    
    [Range(-90, 90)]
    [Tooltip("Stretch arm forward/backward")]
    public float leftArmStretch = 0f;
    
    [Range(0, 160)]
    [Tooltip("Bend elbow in degrees")]
    public float leftElbowBend = 45f;

    [Header("Left Wrist")]
    [Range(-90, 90)]
    [Tooltip("Wrist flexion/extension")]
    public float leftWristBend = 0f;
    
    [Range(-90, 90)]
    [Tooltip("Palm rotation - pronation/supination")]
    public float leftWristRotate = 0f;
    
    [Range(-180, 180)]
    [Tooltip("Lateral tilt - radial/ulnar deviation")]
    public float leftWristTilt = -45f;

    [Header("Left Fingers")]
    [Range(0, 100)]
    [Tooltip("Close fingers - 0 = open hand, 100 = closed fist")]
    public float leftFingersCurl = 0f;
    
    [Range(0, 100)]
    [Tooltip("Spread fingers in fan")]
    public float leftFingersSpread = 0f;
    
    [Space(5)]
    [Tooltip("Disable thumb animation keeping original pose")]
    public bool leftThumbDisable = true;
    
    [Header("Left Thumb Manual Adjustment")]
    [Range(-2f, 2f)]
    [Tooltip("X axis rotation multiplier")]
    public float leftThumbMultX = 0f;
    
    [Range(-2f, 2f)]
    [Tooltip("Y axis rotation multiplier")]
    public float leftThumbMultY = 0f;
    
    [Range(-2f, 2f)]
    [Tooltip("Z axis rotation multiplier")]
    public float leftThumbMultZ = 0f;

    [Header("=== ▼ RIGHT SIDE ▼ ===")]
    [Space(5)]
    [Header("Right Arm")]
    [Range(-180, 180)]
    [Tooltip("Open/close arm - positive values close the arm")]
    public float rightArmSpread = 0f;
    
    [Range(-90, 90)]
    [Tooltip("Raise/lower arm vertically")]
    public float rightArmRaise = 30f;
    
    [Range(-90, 90)]
    [Tooltip("Stretch arm forward/backward")]
    public float rightArmStretch = 0f;
    
    [Range(0, 160)]
    [Tooltip("Bend elbow in degrees")]
    public float rightElbowBend = 45f;

    [Header("Right Wrist")]
    [Range(-90, 90)]
    [Tooltip("Wrist flexion/extension")]
    public float rightWristBend = 0f;
    
    [Range(-90, 90)]
    [Tooltip("Palm rotation - pronation/supination")]
    public float rightWristRotate = 0f;
    
    [Range(-180, 180)]
    [Tooltip("Lateral tilt - radial/ulnar deviation")]
    public float rightWristTilt = -45f;

    [Header("Right Fingers")]
    [Range(0, 100)]
    [Tooltip("Close fingers - 0 = open hand, 100 = closed fist")]
    public float rightFingersCurl = 0f;
    
    [Range(0, 100)]
    [Tooltip("Spread fingers in fan")]
    public float rightFingersSpread = 0f;
    
    [Space(5)]
    [Tooltip("Disable thumb animation keeping original pose")]
    public bool rightThumbDisable = true;
    
    [Header("Right Thumb Manual Adjustment")]
    [Range(-2f, 2f)]
    [Tooltip("X axis rotation multiplier")]
    public float rightThumbMultX = 0f;
    
    [Range(-2f, 2f)]
    [Tooltip("Y axis rotation multiplier")]
    public float rightThumbMultY = 0f;
    
    [Range(-2f, 2f)]
    [Tooltip("Z axis rotation multiplier")]
    public float rightThumbMultZ = 0f;

    [Header("=== Quick Control ===")]
    [Tooltip("Synchronize both arms values automatically")]
    public bool syncArms = false;
    
    [Tooltip("Synchronize both wrists values automatically")]
    public bool syncWrists = false;
    
    // References to main body bones
    private Transform spine;
    private Transform spine1;
    private Transform spine2;
    private Transform leftUpLeg;
    private Transform leftLeg;
    private Transform rightUpLeg;
    private Transform rightLeg;
    private Transform leftArm;
    private Transform leftForeArm;
    private Transform rightArm;
    private Transform rightForeArm;
    private Transform leftHand;
    private Transform rightHand;
    
    // Arrays with the 4 bones of each left hand finger
    private Transform[] leftThumb = new Transform[4];
    private Transform[] leftIndex = new Transform[4];
    private Transform[] leftMiddle = new Transform[4];
    private Transform[] leftRing = new Transform[4];
    private Transform[] leftPinky = new Transform[4];
    
    // Arrays with the 4 bones of each right hand finger
    private Transform[] rightThumb = new Transform[4];
    private Transform[] rightIndex = new Transform[4];
    private Transform[] rightMiddle = new Transform[4];
    private Transform[] rightRing = new Transform[4];
    private Transform[] rightPinky = new Transform[4];
    
    void Start()
    {
        FindBones();
        ApplyPose();
    }
    
    /// <summary>
    /// Finds and stores references to all Mixamo rig bones
    /// </summary>
    private void FindBones()
    {
        Transform[] all = GetComponentsInChildren<Transform>();
        
        foreach (Transform t in all)
        {
            string name = t.name;
            
            // Spine
            if (name == "mixamorig:Spine") spine = t;
            else if (name == "mixamorig:Spine1") spine1 = t;
            else if (name == "mixamorig:Spine2") spine2 = t;
            
            // Legs
            else if (name == "mixamorig:LeftUpLeg") leftUpLeg = t;
            else if (name == "mixamorig:LeftLeg") leftLeg = t;
            else if (name == "mixamorig:RightUpLeg") rightUpLeg = t;
            else if (name == "mixamorig:RightLeg") rightLeg = t;
            
            // Arms and forearms
            else if (name == "mixamorig:LeftArm") leftArm = t;
            else if (name == "mixamorig:LeftForeArm") leftForeArm = t;
            else if (name == "mixamorig:RightArm") rightArm = t;
            else if (name == "mixamorig:RightForeArm") rightForeArm = t;

            // Wrists
            else if (name == "mixamorig:LeftHand") leftHand = t;
            else if (name == "mixamorig:RightHand") rightHand = t;
            
            // Left hand fingers - Thumb
            else if (name == "mixamorig:LeftHandThumb1") leftThumb[0] = t;
            else if (name == "mixamorig:LeftHandThumb2") leftThumb[1] = t;
            else if (name == "mixamorig:LeftHandThumb3") leftThumb[2] = t;
            else if (name == "mixamorig:LeftHandThumb4") leftThumb[3] = t;
            
            // Left hand fingers - Index
            else if (name == "mixamorig:LeftHandIndex1") leftIndex[0] = t;
            else if (name == "mixamorig:LeftHandIndex2") leftIndex[1] = t;
            else if (name == "mixamorig:LeftHandIndex3") leftIndex[2] = t;
            else if (name == "mixamorig:LeftHandIndex4") leftIndex[3] = t;
            
            // Left hand fingers - Middle
            else if (name == "mixamorig:LeftHandMiddle1") leftMiddle[0] = t;
            else if (name == "mixamorig:LeftHandMiddle2") leftMiddle[1] = t;
            else if (name == "mixamorig:LeftHandMiddle3") leftMiddle[2] = t;
            else if (name == "mixamorig:LeftHandMiddle4") leftMiddle[3] = t;
            
            // Left hand fingers - Ring
            else if (name == "mixamorig:LeftHandRing1") leftRing[0] = t;
            else if (name == "mixamorig:LeftHandRing2") leftRing[1] = t;
            else if (name == "mixamorig:LeftHandRing3") leftRing[2] = t;
            else if (name == "mixamorig:LeftHandRing4") leftRing[3] = t;
            
            // Left hand fingers - Pinky
            else if (name == "mixamorig:LeftHandPinky1") leftPinky[0] = t;
            else if (name == "mixamorig:LeftHandPinky2") leftPinky[1] = t;
            else if (name == "mixamorig:LeftHandPinky3") leftPinky[2] = t;
            else if (name == "mixamorig:LeftHandPinky4") leftPinky[3] = t;
            
            // Right hand fingers - Thumb
            else if (name == "mixamorig:RightHandThumb1") rightThumb[0] = t;
            else if (name == "mixamorig:RightHandThumb2") rightThumb[1] = t;
            else if (name == "mixamorig:RightHandThumb3") rightThumb[2] = t;
            else if (name == "mixamorig:RightHandThumb4") rightThumb[3] = t;
            
            // Right hand fingers - Index
            else if (name == "mixamorig:RightHandIndex1") rightIndex[0] = t;
            else if (name == "mixamorig:RightHandIndex2") rightIndex[1] = t;
            else if (name == "mixamorig:RightHandIndex3") rightIndex[2] = t;
            else if (name == "mixamorig:RightHandIndex4") rightIndex[3] = t;
            
            // Right hand fingers - Middle
            else if (name == "mixamorig:RightHandMiddle1") rightMiddle[0] = t;
            else if (name == "mixamorig:RightHandMiddle2") rightMiddle[1] = t;
            else if (name == "mixamorig:RightHandMiddle3") rightMiddle[2] = t;
            else if (name == "mixamorig:RightHandMiddle4") rightMiddle[3] = t;
            
            // Right hand fingers - Ring
            else if (name == "mixamorig:RightHandRing1") rightRing[0] = t;
            else if (name == "mixamorig:RightHandRing2") rightRing[1] = t;
            else if (name == "mixamorig:RightHandRing3") rightRing[2] = t;
            else if (name == "mixamorig:RightHandRing4") rightRing[3] = t;
            
            // Right hand fingers - Pinky
            else if (name == "mixamorig:RightHandPinky1") rightPinky[0] = t;
            else if (name == "mixamorig:RightHandPinky2") rightPinky[1] = t;
            else if (name == "mixamorig:RightHandPinky3") rightPinky[2] = t;
            else if (name == "mixamorig:RightHandPinky4") rightPinky[3] = t;
        }
    }
    
    /// <summary>
    /// Applies all configured rotations to body bones
    /// </summary>
    private void ApplyPose()
    {
        ApplyBackPose();
        ApplyLegsPose();
        ApplyArmsPose();
        ApplyWristsPose();
        ApplyFingersPose();
    }
    
    /// <summary>
    /// Applies back tilt distributed across spine
    /// </summary>
    private void ApplyBackPose()
    {
        if (spine != null) 
            spine.localRotation = Quaternion.Euler(backTilt, 0, 0);
        
        if (spine1 != null) 
            spine1.localRotation = Quaternion.Euler(backTilt * 0.5f, 0, 0);
        
        if (spine2 != null) 
            spine2.localRotation = Quaternion.Euler(backTilt * 0.3f, 0, 0);
    }
    
    /// <summary>
    /// Applies rotation to legs including lateral spread and knee bend
    /// </summary>
    private void ApplyLegsPose()
    {
        // Left leg
        if (leftUpLeg != null)
            leftUpLeg.localRotation = Quaternion.Euler(-90, 180 - legSpread, 0);
        
        if (leftLeg != null)
            leftLeg.localRotation = Quaternion.Euler(-kneeBend, 0, 0);
        
        // Right leg
        if (rightUpLeg != null)
            rightUpLeg.localRotation = Quaternion.Euler(-90, 180 + legSpread, 0);
        
        if (rightLeg != null)
            rightLeg.localRotation = Quaternion.Euler(-kneeBend, 0, 0);
    }
    
    /// <summary>
    /// Applies rotations to arms and elbows
    /// </summary>
    private void ApplyArmsPose()
    {
        // Left arm - combines raise, spread and stretch
        if (leftArm != null)
        {
            leftArm.localRotation = Quaternion.Euler(
                leftArmRaise,
                -leftArmSpread,
                leftArmStretch
            );
        }
        
        if (leftForeArm != null)
        {
            leftForeArm.localRotation = Quaternion.Euler(leftElbowBend, 0, 0);
        }
        
        // Right arm - combines raise, spread and stretch
        if (rightArm != null)
        {
            rightArm.localRotation = Quaternion.Euler(
                rightArmRaise,
                rightArmSpread,
                -rightArmStretch
            );
        }
        
        if (rightForeArm != null)
        {
            rightForeArm.localRotation = Quaternion.Euler(rightElbowBend, 0, 0);
        }
    }
    
    /// <summary>
    /// Applies rotations to wrists including flexion, tilt and rotation
    /// </summary>
    private void ApplyWristsPose()
    {
        // Left wrist
        if (leftHand != null)
        {
            leftHand.localRotation = Quaternion.Euler(
                leftWristBend,
                leftWristTilt,
                leftWristRotate
            );
        }
        
        // Right wrist - some axes inverted for symmetry
        if (rightHand != null)
        {
            rightHand.localRotation = Quaternion.Euler(
                rightWristBend,
                -rightWristTilt,
                -rightWristRotate
            );
        }
    }
    
    /// <summary>
    /// Applies rotations to fingers of both hands
    /// </summary>
    private void ApplyFingersPose()
    {
        // Left hand
        ApplyFingers(
            leftThumb, leftIndex, leftMiddle, leftRing, leftPinky,
            leftFingersCurl, leftFingersSpread,
            leftThumbMultX, leftThumbMultY, leftThumbMultZ,
            leftThumbDisable, true
        );
        
        // Right hand
        ApplyFingers(
            rightThumb, rightIndex, rightMiddle, rightRing, rightPinky,
            rightFingersCurl, rightFingersSpread,
            rightThumbMultX, rightThumbMultY, rightThumbMultZ,
            rightThumbDisable, false
        );
    }
    
    /// <summary>
    /// Applies animation to fingers of one hand
    /// </summary>
    private void ApplyFingers(
        Transform[] thumb, Transform[] index, Transform[] middle,
        Transform[] ring, Transform[] pinky,
        float curl, float spread,
        float thumbMultX, float thumbMultY, float thumbMultZ,
        bool disableThumb, bool isLeft)
    {
        // If both values are zero, reset to original pose
        if (curl == 0f && spread == 0f)
        {
            ResetFingersToOriginalPose(thumb, index, middle, ring, pinky);
            return;
        }
        
        // Convert values from 0-100 to rotation angles
        float curlAmount = curl * 1.3f;      // Maximum ~130 degrees
        float spreadAmount = spread * 0.4f;  // Maximum ~40 degrees
        
        // Spread direction (left vs right)
        float dir = isLeft ? 1f : -1f;
        
        // Apply animation to thumb
        AnimateThumb(thumb, curlAmount, spreadAmount, dir, thumbMultX, thumbMultY, thumbMultZ, disableThumb);
        
        // Apply animation to other fingers
        AnimateIndex(index, curlAmount, spreadAmount, dir);
        AnimateMiddle(middle, curlAmount, spreadAmount, dir);
        AnimateRing(ring, curlAmount, spreadAmount, dir);
        AnimatePinky(pinky, curlAmount, spreadAmount, dir);
    }
    
    /// <summary>
    /// Resets all fingers to original pose (Quaternion.identity)
    /// </summary>
    private void ResetFingersToOriginalPose(
        Transform[] thumb, Transform[] index, Transform[] middle,
        Transform[] ring, Transform[] pinky)
    {
        for (int i = 0; i < 4; i++)
        {
            if (thumb[i] != null) thumb[i].localRotation = Quaternion.identity;
            if (index[i] != null) index[i].localRotation = Quaternion.identity;
            if (middle[i] != null) middle[i].localRotation = Quaternion.identity;
            if (ring[i] != null) ring[i].localRotation = Quaternion.identity;
            if (pinky[i] != null) pinky[i].localRotation = Quaternion.identity;
        }
    }
    
    /// <summary>
    /// Animates thumb with manual control of rotation axes
    /// </summary>
    private void AnimateThumb(Transform[] thumb, float curl, float spread, float dir,
        float multX, float multY, float multZ, bool disable)
    {
        if (disable)
        {
            // Keep thumb in original pose
            for (int i = 0; i < 4; i++)
            {
                if (thumb[i] != null) 
                    thumb[i].localRotation = Quaternion.identity;
            }
            return;
        }
        
        // Bone 0 - Thumb base (combines curl and spread)
        if (thumb[0] != null)
        {
            thumb[0].localRotation = Quaternion.Euler(
                curl * 0.5f * multX,
                curl * 0.5f * multY,
                curl * 0.5f * multZ + dir * spread * 1.5f
            );
        }
        
        // Bones 1-3 - Thumb joints (curl only)
        if (thumb[1] != null)
        {
            thumb[1].localRotation = Quaternion.Euler(
                curl * 0.7f * multX,
                curl * 0.7f * multY,
                curl * 0.7f * multZ
            );
        }
        
        if (thumb[2] != null)
        {
            thumb[2].localRotation = Quaternion.Euler(
                curl * 0.9f * multX,
                curl * 0.9f * multY,
                curl * 0.9f * multZ
            );
        }
        
        if (thumb[3] != null)
        {
            thumb[3].localRotation = Quaternion.Euler(
                curl * 1.0f * multX,
                curl * 1.0f * multY,
                curl * 1.0f * multZ
            );
        }
    }
    
    /// <summary>
    /// Animates index finger
    /// </summary>
    private void AnimateIndex(Transform[] index, float curl, float spread, float dir)
    {
        if (index[0] != null)
            index[0].localRotation = Quaternion.Euler(curl * 0.8f, 0, dir * (-spread * 0.5f));
        if (index[1] != null)
            index[1].localRotation = Quaternion.Euler(curl * 0.9f, 0, 0);
        if (index[2] != null)
            index[2].localRotation = Quaternion.Euler(curl * 1.0f, 0, 0);
        if (index[3] != null)
            index[3].localRotation = Quaternion.Euler(curl * 1.1f, 0, 0);
    }
    
    /// <summary>
    /// Animates middle finger
    /// </summary>
    private void AnimateMiddle(Transform[] middle, float curl, float spread, float dir)
    {
        if (middle[0] != null)
            middle[0].localRotation = Quaternion.Euler(curl * 0.8f, 0, dir * (-spread * 0.2f));
        if (middle[1] != null)
            middle[1].localRotation = Quaternion.Euler(curl * 0.9f, 0, 0);
        if (middle[2] != null)
            middle[2].localRotation = Quaternion.Euler(curl * 1.0f, 0, 0);
        if (middle[3] != null)
            middle[3].localRotation = Quaternion.Euler(curl * 1.1f, 0, 0);
    }
    
    /// <summary>
    /// Animates ring finger
    /// </summary>
    private void AnimateRing(Transform[] ring, float curl, float spread, float dir)
    {
        if (ring[0] != null)
            ring[0].localRotation = Quaternion.Euler(curl * 0.8f, 0, dir * spread * 0.2f);
        if (ring[1] != null)
            ring[1].localRotation = Quaternion.Euler(curl * 0.9f, 0, 0);
        if (ring[2] != null)
            ring[2].localRotation = Quaternion.Euler(curl * 1.0f, 0, 0);
        if (ring[3] != null)
            ring[3].localRotation = Quaternion.Euler(curl * 1.1f, 0, 0);
    }
    
    /// <summary>
    /// Animates pinky finger
    /// </summary>
    private void AnimatePinky(Transform[] pinky, float curl, float spread, float dir)
    {
        if (pinky[0] != null)
            pinky[0].localRotation = Quaternion.Euler(curl * 0.8f, 0, dir * spread * 0.5f);
        if (pinky[1] != null)
            pinky[1].localRotation = Quaternion.Euler(curl * 0.9f, 0, 0);
        if (pinky[2] != null)
            pinky[2].localRotation = Quaternion.Euler(curl * 1.0f, 0, 0);
        if (pinky[3] != null)
            pinky[3].localRotation = Quaternion.Euler(curl * 1.1f, 0, 0);
    }
    
    /// <summary>
    /// Called by Unity when values are changed in Inspector
    /// Allows real-time visualization of changes
    /// </summary>
    void OnValidate()
    {
        // Automatic synchronization if enabled
        if (syncArms)
        {
            rightArmSpread = leftArmSpread;
            rightArmRaise = leftArmRaise;
            rightArmStretch = leftArmStretch;
            rightElbowBend = leftElbowBend;
        }
        
        if (syncWrists)
        {
            rightWristBend = leftWristBend;
            rightWristRotate = leftWristRotate;
            rightWristTilt = leftWristTilt;
            rightFingersCurl = leftFingersCurl;
            rightFingersSpread = leftFingersSpread;
        }
        
        // Reapply pose with new values
        if (spine == null || leftArm == null) 
            FindBones();
        
        ApplyPose();
    }
}