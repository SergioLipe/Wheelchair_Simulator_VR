using UnityEngine;
using System.Collections.Generic;

public class WheelchairViewFix : MonoBehaviour
{
    [Header("=== Configuração ===")]
    [Tooltip("Câmara principal")]
    public Camera cameraPrincipal;
    
    [Tooltip("Lista de renderers do corpo (arrasta aqui todos os que quiseres)")]
    public List<SkinnedMeshRenderer> renderersDoCorpo = new List<SkinnedMeshRenderer>();
    
    [Header("=== Bones para Esconder ===")]
    [Tooltip("Arrasta aqui os bones/ossos que queres esconder")]
    public List<Transform> bonesParaEsconder = new List<Transform>();
    
    [Tooltip("Tamanho quando escondido (0.001 = quase invisível)")]
    [Range(0.001f, 1f)]
    public float tamanhoEscondido = 0.001f;
    
    // Guardar tamanhos originais
    private Dictionary<Transform, Vector3> tamanhosOriginais = new Dictionary<Transform, Vector3>();
    
    void Start()
    {
        ConfigurarCamara();
        AplicarDuplaFaceATodos();
        EsconderBones();
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
    
    void EsconderBones()
    {
        foreach (var bone in bonesParaEsconder)
        {
            if (bone != null)
            {
                // Guardar tamanho original
                tamanhosOriginais[bone] = bone.localScale;
                
                // Esconder reduzindo o tamanho
                bone.localScale = Vector3.one * tamanhoEscondido;
                
                Debug.Log($"Bone escondido: {bone.name}");
            }
        }
    }
    
    
}