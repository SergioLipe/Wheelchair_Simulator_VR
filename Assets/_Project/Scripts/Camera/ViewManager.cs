using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Fixes first-person wheelchair view by making materials visible from both sides
/// and hiding specific parts
/// </summary>
public class WheelchairViewFix : MonoBehaviour
{
    [Header("=== Configuration ===")]
    [Tooltip("Reference to the main camera")]
    public Camera mainCamera;
    
    [Tooltip("List of body renderers that will be visible from both sides")]
    public List<SkinnedMeshRenderer> bodyRenderers = new List<SkinnedMeshRenderer>();
    
    [Header("=== Bones to Hide ===")]
    [Tooltip("List of transforms (bones) that will be hidden by reducing scale")]
    public List<Transform> bonesToHide = new List<Transform>();
    
    [Tooltip("Scale applied to hidden bones (smaller = more invisible)")]
    [Range(0.001f, 1f)]
    public float hiddenScale = 0.001f;
    
    void Start()
    {
        SetupCamera();
        ApplyDoubleSidedRendering();
        HideBones();
    }
    
    /// <summary>
    /// Configures camera to render very close objects
    /// </summary>
    private void SetupCamera()
    {
        if (mainCamera == null)
        {
            mainCamera = GetComponentInChildren<Camera>();
        }
        
        if (mainCamera != null)
        {
            mainCamera.nearClipPlane = 0.01f;
        }
    }
    
    /// <summary>
    /// Applies double-sided rendering to all body renderers
    /// Allows materials to be visible from inside in first person
    /// </summary>
    private void ApplyDoubleSidedRendering()
    {
        foreach (var renderer in bodyRenderers)
        {
            if (renderer != null)
            {
                Material mat = new Material(renderer.material);
                mat.SetInt("_Cull", 0);
                renderer.material = mat;
            }
        }
    }
    
    /// <summary>
    /// Hides bones from list by drastically reducing their scale
    /// </summary>
    private void HideBones()
    {
        foreach (var bone in bonesToHide)
        {
            if (bone != null)
            {
                bone.localScale = Vector3.one * hiddenScale;
            }
        }
    }
}