using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Modern visual collision feedback system
/// Uses UI Canvas with gradients, smooth animations and directional effects
/// </summary>
public class CollisionFlashEffect : MonoBehaviour
{
    [Header("=== General Configuration ===")]
    [Tooltip("Enable feedback system")]
    public bool feedbackActive = true;

    [Tooltip("Canvas reference (will be created automatically if null)")]
    public Canvas canvas;

    [Header("=== Visual Configuration ===")]
    [Tooltip("Effect duration (seconds)")]
    [Range(0.1f, 2f)]
    public float effectDuration = 0.5f;

    [Tooltip("Maximum effect intensity (0-1)")]
    [Range(0f, 1f)]
    public float maxIntensity = 0.7f;

    [Tooltip("Use radial gradient (more modern)")]
    public bool useRadialGradient = true;

    [Header("=== Colors ===")]
    [Tooltip("Color for front/rear collisions")]
    public Color impactColor = new Color(1f, 0.2f, 0.2f, 1f);

    [Tooltip("Color for side slides")]
    public Color slideColor = new Color(1f, 0.8f, 0f, 1f);

    [Header("=== Animation ===")]
    [Tooltip("Effect animation curve")]
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Tooltip("Number of pulses")]
    [Range(1, 3)]
    public int pulseCount = 1;

    [Header("=== Camera Shake (Optional) ===")]
    [Tooltip("Enable camera shake")]
    public bool cameraShakeActive = true;

    [Tooltip("Shake intensity")]
    [Range(0f, 1f)]
    public float shakeIntensity = 0.15f;

    [Tooltip("Shake duration")]
    [Range(0.05f, 0.5f)]
    public float shakeDuration = 0.2f;

    [Header("=== Extra Effects ===")]
    [Tooltip("Show directional arrows")]
    public bool showArrows = true;

    [Tooltip("Arrow size")]
    [Range(50f, 200f)]
    public float arrowSize = 100f;

    public enum CollisionType
    {
        None,
        Front,
        Back,
        LeftSide,
        RightSide
    }

    // UI components
    private GameObject effectPanel;
    private Image effectImage;
    private GameObject[] arrows = new GameObject[4];
    private Image[] arrowImages = new Image[4];

    // Animation state - one coroutine per arrow for independent control
    private Coroutine[] arrowCoroutines = new Coroutine[4];
    private Coroutine mainEffectCoroutine;
    private Transform cameraTransform;
    private Vector3 originalCameraPosition;

    void Start()
    {
        SetupUI();
        InitializeCamera();
    }

    void OnDestroy()
    {
        StopAllEffects();
    }

    /// <summary>
    /// Initializes camera reference for shake effect
    /// </summary>
    private void InitializeCamera()
    {
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
            originalCameraPosition = cameraTransform.localPosition;
        }
    }

    /// <summary>
    /// Sets up Canvas and UI elements
    /// </summary>
    void SetupUI()
    {
        CreateCanvasIfNeeded();
        CreateEffectPanel();
        
        if (showArrows)
        {
            CreateArrows();
        }
    }

    /// <summary>
    /// Creates Canvas if it doesn't exist
    /// </summary>
    private void CreateCanvasIfNeeded()
    {
        if (canvas != null) return;

        GameObject canvasObj = new GameObject("CollisionFeedbackCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();
    }

    /// <summary>
    /// Creates main effect panel
    /// </summary>
    private void CreateEffectPanel()
    {
        effectPanel = new GameObject("EffectPanel");
        effectPanel.transform.SetParent(canvas.transform, false);

        RectTransform rectEffect = effectPanel.AddComponent<RectTransform>();
        rectEffect.anchorMin = Vector2.zero;
        rectEffect.anchorMax = Vector2.one;
        rectEffect.sizeDelta = Vector2.zero;

        effectImage = effectPanel.AddComponent<Image>();
        effectImage.color = new Color(1, 1, 1, 0);
        effectImage.raycastTarget = false;
    }

    /// <summary>
    /// Creates directional arrows
    /// </summary>
    void CreateArrows()
    {
        string[] names = { "FrontArrow", "BackArrow", "LeftArrow", "RightArrow" };
        Vector2[] positions = {
            new Vector2(0.5f, 0.85f),
            new Vector2(0.5f, 0.15f),
            new Vector2(0.15f, 0.5f),
            new Vector2(0.85f, 0.5f)
        };
        float[] rotations = { 0f, 180f, 90f, -90f };

        Sprite arrowSprite = CreateArrowSprite();

        for (int i = 0; i < 4; i++)
        {
            arrows[i] = new GameObject(names[i]);
            arrows[i].transform.SetParent(canvas.transform, false);

            RectTransform rect = arrows[i].AddComponent<RectTransform>();
            rect.anchorMin = positions[i];
            rect.anchorMax = positions[i];
            rect.sizeDelta = new Vector2(arrowSize, arrowSize);
            rect.localRotation = Quaternion.Euler(0, 0, rotations[i]);

            arrowImages[i] = arrows[i].AddComponent<Image>();
            arrowImages[i].color = new Color(1, 1, 1, 0);
            arrowImages[i].raycastTarget = false;
            arrowImages[i].sprite = arrowSprite;
        }
    }

    /// <summary>
    /// Creates arrow sprite procedurally
    /// </summary>
    Sprite CreateArrowSprite()
    {
        int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float normY = (float)y / size;
                float centerX = size / 2f;
                float width = (1f - normY) * size / 2f;

                if (x >= centerX - width && x <= centerX + width)
                {
                    pixels[y * size + x] = Color.white;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    // ===== PUBLIC METHODS =====

    public void FrontFlash() => ActivateFeedback(CollisionType.Front);
    public void BackFlash() => ActivateFeedback(CollisionType.Back);
    public void LeftSideFlash() => ActivateFeedback(CollisionType.LeftSide);
    public void RightSideFlash() => ActivateFeedback(CollisionType.RightSide);

    /// <summary>
    /// Activates visual feedback
    /// </summary>
    void ActivateFeedback(CollisionType type)
    {
        if (!feedbackActive || type == CollisionType.None) return;

        // Stop and clean previous main effect
        if (mainEffectCoroutine != null)
        {
            StopCoroutine(mainEffectCoroutine);
            mainEffectCoroutine = null;
        }

        // Start main effect
        mainEffectCoroutine = StartCoroutine(AnimateMainEffect(type));

        // Handle arrow animation independently
        int arrowIndex = GetArrowIndex(type);
        if (arrowIndex >= 0 && showArrows)
        {
            // Stop previous arrow animation for this specific arrow
            if (arrowCoroutines[arrowIndex] != null)
            {
                StopCoroutine(arrowCoroutines[arrowIndex]);
                arrowCoroutines[arrowIndex] = null;
            }

            // Start new arrow animation
            Color color = GetColorForType(type);
            arrowCoroutines[arrowIndex] = StartCoroutine(AnimateArrow(arrowIndex, color));
        }

        // Camera shake
        if (cameraShakeActive && cameraTransform != null)
        {
            StartCoroutine(CameraShake());
        }
    }

    /// <summary>
    /// Animates the main screen effect
    /// </summary>
    IEnumerator AnimateMainEffect(CollisionType type)
    {
        Color color = GetColorForType(type);
        Texture2D texture = CreateGradientTexture(type);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), 
            new Vector2(0.5f, 0.5f));
        effectImage.sprite = sprite;

        float durationPerPulse = effectDuration / pulseCount;
        float elapsedTime = 0f;

        for (int pulse = 0; pulse < pulseCount; pulse++)
        {
            float pulseStartTime = elapsedTime;

            while (elapsedTime - pulseStartTime < durationPerPulse)
            {
                elapsedTime += Time.deltaTime;
                float progress = (elapsedTime - pulseStartTime) / durationPerPulse;
                float intensity = animationCurve.Evaluate(progress) * maxIntensity;

                Color currentColor = color;
                currentColor.a = intensity;
                effectImage.color = currentColor;

                yield return null;
            }
        }

        // Clean up main effect
        effectImage.color = new Color(color.r, color.g, color.b, 0);
        Destroy(texture);
        Destroy(sprite);
        mainEffectCoroutine = null;
    }

    /// <summary>
    /// Animates a specific arrow independently
    /// </summary>
    IEnumerator AnimateArrow(int arrowIndex, Color color)
    {
        if (arrowIndex < 0 || arrowIndex >= 4 || arrowImages[arrowIndex] == null)
            yield break;

        float durationPerPulse = effectDuration / pulseCount;
        float elapsedTime = 0f;

        for (int pulse = 0; pulse < pulseCount; pulse++)
        {
            float pulseStartTime = elapsedTime;

            while (elapsedTime - pulseStartTime < durationPerPulse)
            {
                elapsedTime += Time.deltaTime;
                float progress = (elapsedTime - pulseStartTime) / durationPerPulse;
                float intensity = animationCurve.Evaluate(progress) * maxIntensity;

                // Update arrow visual
                Color arrowColor = color;
                arrowColor.a = intensity * 1.5f;
                arrowImages[arrowIndex].color = arrowColor;

                float scale = 1f + Mathf.Sin(progress * Mathf.PI) * 0.3f;
                arrows[arrowIndex].transform.localScale = Vector3.one * scale;

                yield return null;
            }
        }

        // Clean up this specific arrow
        arrowImages[arrowIndex].color = new Color(color.r, color.g, color.b, 0);
        arrows[arrowIndex].transform.localScale = Vector3.one;
        arrowCoroutines[arrowIndex] = null;
    }

    /// <summary>
    /// Returns color based on collision type
    /// </summary>
    private Color GetColorForType(CollisionType type)
    {
        return (type == CollisionType.Front || type == CollisionType.Back) ? impactColor : slideColor;
    }

    /// <summary>
    /// Returns arrow index for collision type
    /// </summary>
    private int GetArrowIndex(CollisionType type)
    {
        if (!showArrows) return -1;

        switch (type)
        {
            case CollisionType.Front: return 0;
            case CollisionType.Back: return 1;
            case CollisionType.LeftSide: return 2;
            case CollisionType.RightSide: return 3;
            default: return -1;
        }
    }

    /// <summary>
    /// Creates directional gradient texture
    /// </summary>
    Texture2D CreateGradientTexture(CollisionType type)
    {
        int size = 512;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float alpha = CalculateGradientAlpha(type, x, y, size);
                texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(alpha)));
            }
        }

        texture.Apply();
        return texture;
    }

    /// <summary>
    /// Calculates gradient alpha for pixel
    /// </summary>
    private float CalculateGradientAlpha(CollisionType type, int x, int y, int size)
    {
        if (useRadialGradient)
        {
            return CalculateRadialGradient(type, x, y, size);
        }
        else
        {
            return CalculateLinearGradient(type, x, y, size);
        }
    }

    /// <summary>
    /// Calculates radial gradient alpha
    /// </summary>
    private float CalculateRadialGradient(CollisionType type, int x, int y, int size)
    {
        float distX = Mathf.Abs(x - size / 2f) / (size / 2f);
        float distY = Mathf.Abs(y - size / 2f) / (size / 2f);
        
        switch (type)
        {
            case CollisionType.Front:
                return 1f - (distX * 0.7f + (1f - (float)y / size) * 0.3f);
            case CollisionType.Back:
                return 1f - (distX * 0.7f + ((float)y / size) * 0.3f);
            case CollisionType.LeftSide:
                return 1f - (distY * 0.7f + (1f - (float)x / size) * 0.3f);
            case CollisionType.RightSide:
                return 1f - (distY * 0.7f + ((float)x / size) * 0.3f);
            default:
                return 0f;
        }
    }

    /// <summary>
    /// Calculates linear gradient alpha
    /// </summary>
    private float CalculateLinearGradient(CollisionType type, int x, int y, int size)
    {
        switch (type)
        {
            case CollisionType.Front:
                return 1f - (float)y / size;
            case CollisionType.Back:
                return (float)y / size;
            case CollisionType.LeftSide:
                return 1f - (float)x / size;
            case CollisionType.RightSide:
                return (float)x / size;
            default:
                return 0f;
        }
    }

    /// <summary>
    /// Camera shake effect
    /// </summary>
    IEnumerator CameraShake()
    {
        float elapsedTime = 0f;

        while (elapsedTime < shakeDuration)
        {
            float intensity = Mathf.Lerp(shakeIntensity, 0f, elapsedTime / shakeDuration);
            
            Vector3 offset = new Vector3(
                Random.Range(-1f, 1f) * intensity,
                Random.Range(-1f, 1f) * intensity,
                0
            );

            cameraTransform.localPosition = originalCameraPosition + offset;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        cameraTransform.localPosition = originalCameraPosition;
    }

    /// <summary>
    /// Stop all effects
    /// </summary>
    public void StopAllEffects()
    {
        // Stop main effect
        if (mainEffectCoroutine != null)
        {
            StopCoroutine(mainEffectCoroutine);
            mainEffectCoroutine = null;
        }

        // Stop all arrow coroutines
        for (int i = 0; i < arrowCoroutines.Length; i++)
        {
            if (arrowCoroutines[i] != null)
            {
                StopCoroutine(arrowCoroutines[i]);
                arrowCoroutines[i] = null;
            }
        }

        // Reset main effect visual
        if (effectImage != null)
            effectImage.color = new Color(1, 1, 1, 0);

        // Reset all arrow visuals
        for (int i = 0; i < arrowImages.Length; i++)
        {
            if (arrowImages[i] != null)
            {
                arrowImages[i].color = new Color(1, 1, 1, 0);
                if (arrows[i] != null)
                    arrows[i].transform.localScale = Vector3.one;
            }
        }

        // Reset camera
        if (cameraTransform != null)
            cameraTransform.localPosition = originalCameraPosition;
    }
}