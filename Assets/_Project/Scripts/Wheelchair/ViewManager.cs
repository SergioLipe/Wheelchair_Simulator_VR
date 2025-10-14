using UnityEngine;
using System.Collections.Generic;

public class ViewManager : MonoBehaviour
{
    [Header("=== Configuração ===")]
    [Tooltip("Câmara principal")]
    public Camera cameraPrincipal;
    
    [Tooltip("Lista de renderers do corpo (arrasta aqui todos os que quiseres)")]
    public List<SkinnedMeshRenderer> renderersDoCorpo = new List<SkinnedMeshRenderer>();
    
    void Start()
    {
        ConfigurarCamara();
        AplicarDuplaFaceATodos();
    }
    
    void ConfigurarCamara()
    {
        // Se não foi definida, procura a câmara filha
        if (cameraPrincipal == null)
        {
            cameraPrincipal = GetComponentInChildren<Camera>();
        }
        
        // Ajustar câmara para ver objetos muito próximos
        if (cameraPrincipal != null)
        {
            cameraPrincipal.nearClipPlane = 0.01f;
        }
    }
    
    void AplicarDuplaFaceATodos()
    {
        // Fazer com que os renderers sejam visíveis dos dois lados
        foreach (var renderer in renderersDoCorpo)
        {
            if (renderer != null)
            {
                Material mat = new Material(renderer.material);
                mat.SetInt("_Cull", 0); // 0 = ver dos dois lados
                renderer.material = mat;
            }
        }
        
        Debug.Log($"Vista corrigida para {renderersDoCorpo.Count} partes do corpo!");
    }
    
    // Método opcional para ligar/desligar o corpo
    public void AlternarCorpo()
    {
        foreach (var renderer in renderersDoCorpo)
        {
            if (renderer != null)
            {
                renderer.enabled = !renderer.enabled;
            }
        }
    }
}