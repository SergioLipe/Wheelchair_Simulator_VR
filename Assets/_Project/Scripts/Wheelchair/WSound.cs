using UnityEngine;
using System.Collections;

/// <summary>
/// Alternative sound system with fade out effects
/// Uses separate AudioSources for startup and movement loop
/// </summary>
[RequireComponent(typeof(Movement))]
[RequireComponent(typeof(AudioSource))]
public class WSound : MonoBehaviour
{
    [Header("Audio Source References")]
    [Tooltip("AudioSource with STARTUP sound (plays once)")]
    public AudioSource startupAudioSource;

    [Tooltip("AudioSource with MOVEMENT sound (loops)")]
    public AudioSource movementAudioSource;

    [Header("Fade Configuration")]
    [Tooltip("Time (in seconds) for movement sound to fade out")]
    public float fadeOutTime = 0.2f;

    // Component references
    private Movement movementController;

    // State
    private bool startupSoundPlayed = false;
    private bool wasAcceleratingCache = false;
    
    // Fade control
    private Coroutine fadeOutCoroutine;
    private float originalMovementVolume;

    void Start()
    {
        InitializeComponents();
    }

    void Update()
    {
        CheckAccelerationState();
        UpdateStartupToLoopTransition();
    }

    /// <summary>
    /// Initializes component references and validates audio sources
    /// </summary>
    private void InitializeComponents()
    {
        movementController = GetComponent<Movement>();

        if (startupAudioSource == null || movementAudioSource == null)
        {
            return;
        }
        
        originalMovementVolume = movementAudioSource.volume;
    }

    /// <summary>
    /// Checks acceleration state and triggers appropriate sounds
    /// </summary>
    private void CheckAccelerationState()
    {
        bool acceleratingNow = movementController.playerIsAccelerating;

        // Player started accelerating
        if (acceleratingNow && !wasAcceleratingCache)
        {
            PlayStartupSounds();
        }
        // Player stopped accelerating
        else if (!acceleratingNow && wasAcceleratingCache)
        {
            StopSounds();
        }

        wasAcceleratingCache = acceleratingNow;
    }

    /// <summary>
    /// Handles transition from startup sound to movement loop
    /// </summary>
    private void UpdateStartupToLoopTransition()
    {
        if (startupSoundPlayed && !startupAudioSource.isPlaying)
        {
            bool acceleratingNow = movementController.playerIsAccelerating;
            
            if (acceleratingNow && !movementAudioSource.isPlaying)
            {
                movementAudioSource.volume = originalMovementVolume;
                movementAudioSource.Play();
            }
            startupSoundPlayed = false;
        }
    }

    /// <summary>
    /// Plays startup sounds when acceleration begins
    /// </summary>
    private void PlayStartupSounds()
    {
        // Cancel fade out if active
        if (fadeOutCoroutine != null)
        {
            StopCoroutine(fadeOutCoroutine);
            fadeOutCoroutine = null;
        }

        // Stop loop sound immediately
        movementAudioSource.Stop();

        // Restore movement sound volume
        movementAudioSource.volume = originalMovementVolume;

        // Play startup sound
        startupAudioSource.Stop();
        startupAudioSource.Play();
        startupSoundPlayed = true;
    }

    /// <summary>
    /// Stops sounds with fade out effect
    /// </summary>
    private void StopSounds()
    {
        // Stop startup immediately
        startupAudioSource.Stop();
        startupSoundPlayed = false;

        // Fade out movement sound if playing
        if (movementAudioSource.isPlaying)
        {
            fadeOutCoroutine = StartCoroutine(FadeOut(movementAudioSource, fadeOutTime));
        }
    }

    /// <summary>
    /// Coroutine that lowers AudioSource volume to zero then stops it
    /// </summary>
    private IEnumerator FadeOut(AudioSource audioSource, float fadeTime)
    {
        float startVolume = audioSource.volume;
        float elapsedTime = 0f;

        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsedTime / fadeTime);
            yield return null;
        }

        // Ensure volume is zero and stop sound
        audioSource.volume = 0f;
        audioSource.Stop();
        
        // Restore original volume for next time
        audioSource.volume = originalMovementVolume;
        fadeOutCoroutine = null;
    }
}